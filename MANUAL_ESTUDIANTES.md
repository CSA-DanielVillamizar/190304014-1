# Guía Paso a Paso: Generación de QR, Configuración de Kubernetes y Pruebas de Carga

Esta guía documenta todo el proceso que hemos realizado en el proyecto de `Itm.Store.System`. Está diseñada para que los estudiantes puedan replicar cada uno de los pasos en sus propios entornos.

---

## 🚀 Paso 1: Generación de Códigos QR en Azure Functions

Implementamos la lógica para generar un código QR cada vez que se procesa una orden exitosamente mediante RabbitMQ.

1. **Instalar el paquete de generación QR:**
   Nos posicionamos en la carpeta de `Itm.Tickets.Functions` e instalamos la biblioteca `QRCoder`:
   ```bash
   dotnet add Itm.Tickets.Functions/Itm.Tickets.Functions.csproj package QRCoder
   ```

2. **Actualizar la Lógica en `GenerateQrFunction.cs`:**
   Modificamos la función para generar un QR en base64 con los datos `OrderId`, `CustomerEmail` y `TotalAmount`, importando `using QRCoder;`.

3. **Subir archivos de configuración (Opcional):**
   Si se requiere el archivo `local.settings.json`, se fuerza su subida a git:
   ```bash
   git add -f Itm.Tickets.Functions/local.settings.json
   ```

---

## 📈 Paso 2: Configuración de Métricas en Kubernetes y Autoescalado (HPA)

Para que Kubernetes pueda escalar automáticamente nuestros contenedores (Horizontal Pod Autoscaler o HPA), necesita saber cuánta CPU/Memoria están consumiendo.

1. **Instalar Metrics Server (Para Docker Desktop):**
   Descargamos y aplicamos el configurador, y agregamos la bandera `--kubelet-insecure-tls` que es necesaria para entornos locales:
   ```bash
   kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

   kubectl patch deployment metrics-server -n kube-system --type='json' -p='[{"op": "add", "path": "/spec/template/spec/containers/0/args/-", "value": "--kubelet-insecure-tls"}]'
   ```

2. **Asignar Resources Requests a los Deployments:**
   El HPA necesita un límite base de CPU para calcular porcentajes de uso. A nuestros deployments les agregamos:
   ```yaml
   resources:
     requests:
       cpu: 100m
   ```

---

## 🐳 Paso 3: Creación de Dockerfiles para microservicios

Creamos configuraciones `Dockerfile` para las APIs restantes de forma que también podamos ejecutarlas en clúster.
Ubicamos un `Dockerfile` en la raíz de cada proyecto (`Itm.Inventory.Api`, `Itm.Product.Api`, `Itm.Gateway.Api`) usando la imagen oficial de .NET 8.

**Para compilar y subir a Docker Hub (Deben loguearse con `docker login` antes):**
```bash
# Compilar
docker build -t <tu_usuario_docker>/itm-inventory-api:latest -f Itm.Inventory.Api/Dockerfile .
docker build -t <tu_usuario_docker>/itm-product-api:latest -f Itm.Product.Api/Dockerfile .
docker build -t <tu_usuario_docker>/itm-gateway-api:latest -f Itm.Gateway.Api/Dockerfile .

# Subir a Docker Hub
docker push <tu_usuario_docker>/itm-inventory-api:latest
docker push <tu_usuario_docker>/itm-product-api:latest
docker push <tu_usuario_docker>/itm-gateway-api:latest
```

---

## ⚙️ Paso 4: Creación de Manifiestos YAML para Kubernetes

Creamos los archivos `.yaml` para desplegar nuestros microservicios recién empaquetados.
Creamos:
* `inventory-deployment.yaml`
* `product-deployment.yaml`
* `gateway-deployment.yaml`

Cada uno contiene un **Deployment** (con Resource Requests configurado a `100m`) y un **Service** que expone el microservicio internamente en el puerto `80`.

**Aplicar los manifiestos:**
```bash
kubectl apply -f inventory-deployment.yaml
kubectl apply -f product-deployment.yaml
kubectl apply -f gateway-deployment.yaml
```

---

## 🎯 Paso 5: Preparación y Ejecución de Pruebas de Carga con k6 y Auto-Escalado (HPA)

En este paso unimos el ataque de tráfico con `k6` y activamos las reglas para que el sistema crezca automáticamente frente a este tráfico. Creamos un archivo `test-load.js` para simular tráfico pesado y probar la escalabilidad del sistema. 

1. **El Script `test-load.js`:**
   Ataca el Ingress en el endpoint de órdenes (`http://api.itm-tickets.com/orders`) escalando hasta 100 peticiones concurrentes y manteniendo esa carga.

2. **Instalar k6:**
   * En Windows con Chocolatey: `choco install k6`
   * En Windows con Winget: `winget install k6`

3. **Activar el Auto-Escalado (HPA):**
   *🗣️ Guion Docente:*
   *"Vamos a configurar el HPA. Le diremos a Kubernetes: 'Si ves que mis pods están trabajando a más del 50% de su capacidad, tráeme refuerzos'."*

   Ejecutamos en la consola para activar el escalado en el deployment `order-api-deployment`:
   ```bash
   kubectl autoscale deployment order-api-deployment --cpu-percent=50 --min=3 --max=10
   ```

4. **Verificación en Tiempo Real (La Sala de Guerra):**
   Para que la clase sea dinámica, pedir a los estudiantes abrir **3 terminales en paralelo**.

   * **Terminal 1 (El Observador de Métricas):**
     Aquí verán cómo sube el % de CPU y aumenta la columna REPLICAS.
     ```bash
     kubectl get hpa order-api-deployment -w
     ```

   * **Terminal 2 (Los Pods en Combate):**
     Verán pods pasando de 'Pending' a 'Running' en segundos.
     ```bash
     kubectl get pods -l app=order-api -w
     ```

   * **Terminal 3 (El Ataque):**
     ```bash
     k6 run test-load.js
     ```

   *🗣️ Guion Docente:*
   *"Miren la Terminal 1. Noten que hay un retraso de unos 15-30 segundos. Kubernetes no escala instantáneamente para evitar el 'efecto rebote'. Se llama Cooldown period. ¡Arquitectos, vean cómo nacen los nuevos pods para salvar el negocio!"*
