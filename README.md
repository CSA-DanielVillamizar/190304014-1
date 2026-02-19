# 190304014-1 - ITM Store System

Repositorio de la clase de Arquitectura de Software impartida por **Daniel Villamizar**.

Este proyecto corresponde a la **CLASE 1: Fundamentos de Arquitectura Distribuida y Orquestación**, donde transformamos un escenario monolítico en una arquitectura basada en microservicios usando .NET 8 y Minimal APIs.

---

## 🎯 Objetivo de la Clase

Tomar el caso de una tienda ficticia (**ITM-Tech Store**) que colapsa en Black Friday debido a un **monolito acoplado**, y rediseñarlo en una arquitectura distribuida donde cada módulo pueda fallar sin tumbar a los demás.

- Entender la diferencia entre **Monolito** y **Microservicios**.
- Introducir el concepto de **acoplamiento** y cómo reducirlo.
- Diseñar contratos de comunicación usando **DTOs (Data Transfer Objects)**.
- Usar `HttpClientFactory` y políticas de **resiliencia** para comunicar microservicios.

---

## 🧱 Escenario de Negocio: "ITM-Tech Store"

A las 00:01 del Black Friday, la tienda lanza oferta de laptops al 50%. El sistema es monolítico:

- Un solo proyecto ASP.NET con todo junto: precios, pagos, bodega, usuarios, etc.
- Miles de usuarios entran a ver precios.
- El módulo de precios satura el servidor.
- Como todo vive en el mismo proceso, también se caen **pagos** y **logística**.
- Nadie puede comprar, ni despachar pedidos.

**Conclusión:** Si un módulo se cae, se lleva todo por delante. Necesitamos **microservicios**.

---

## 🧠 Conceptos Clave Trabajados en Clase

### Monolito vs Microservicios

- **Monolito:** Un solo bloque de código y despliegue. Un fallo puede tumbar todo.
- **Microservicios:** Servicios pequeños, autónomos, desplegados de forma independiente.

### Acoplamiento (Coupling)

- **Alto acoplamiento:** Un módulo conoce detalles internos de otro (por ejemplo, la app móvil conoce directamente las tablas Oracle).
- **Bajo acoplamiento:** Los módulos se hablan por **contratos** (DTOs, APIs) en lugar de tocarse internamente.

### DTO (Data Transfer Object)

- Objeto simple para transportar datos entre procesos.
- No contiene lógica de negocio.
- En este proyecto se usa `record` para obtener **inmutabilidad** y semántica de valor.

### HttpClientFactory y Resiliencia

- `HttpClientFactory` gestiona conexiones HTTP de forma eficiente.
- Evita problemas de sockets agotados por mal uso de `new HttpClient()`.
- Se agrega `Microsoft.Extensions.Http.Resilience` para:
  - Reintentos (Retry).
  - Circuit Breaker.
  - Manejo de fallos transitorios.

---

## 🏗️ Estructura de la Solución

Solución: `Itm.Store.System`

Proyectos principales:

- `Itm.Inventory.Api` – Microservicio dueño del **stock** de productos.
- `Itm.Product.Api` – Microservicio **orquestador**, que consulta a Inventario vía HTTP.

---

## 📦 Tecnologías y Requisitos

- **.NET SDK:** 8.0
- **IDE recomendado:** Visual Studio 2022+ (carga de trabajo "Desarrollo ASP.NET y Web").
- **Estilo de API:** Minimal APIs.
- **Paquetes NuGet usados:**
  - `Microsoft.AspNetCore.OpenApi`
  - `Microsoft.Extensions.Http.Resilience`

---

## 🔹 Itm.Inventory.Api (Servicio de Inventario)

Microservicio responsable de exponer el stock de productos.

### DTO principal

Archivo: `Itm.Inventory.Api/Dtos/InventoryDto.cs`

```csharp
namespace Itm.Inventory.Api.Dtos;

public record InventoryDto(int ProductId, int Stock, string Sku);
```

### Lógica principal (`Program.cs`)

- Configura Swagger.
- Define una "base de datos" en memoria (`List<InventoryDto>`).
- Expone el endpoint:

`GET /api/inventory/{id}`

Comportamiento:

- Si el producto existe → `200 OK` con el JSON del inventario.
- Si no existe → `404 Not Found`.

Ejemplo de respuesta:

```json
{
  "productId": 1,
  "stock": 50,
  "sku": "LAPTOP-DELL"
}
```

---

## 🔹 Itm.Product.Api (Orquestador de Productos)

Microservicio que **no tiene su propio inventario**. Su trabajo es orquestar información consultando a `Itm.Inventory.Api`.

### Configuración de HttpClient y Resiliencia

Archivo: `Itm.Product.Api/Program.cs`

```csharp
builder.Services.AddHttpClient("InventoryClient", client =>
{
    // Puerto del servicio Inventory.Api (revisar launchSettings.json)
    client.BaseAddress = new Uri("http://localhost:5273");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();
```

### Endpoint Orquestador

`GET /api/products/{id}/check-stock`

- Usa `IHttpClientFactory` para obtener el cliente configurado.
- Llama a `GET /api/inventory/{id}` del microservicio de inventario.
- Combina la info de inventario con datos propios de producto (ej. nombre de marketing).
- Maneja fallos de red con `try/catch` sobre `HttpRequestException`.

Ejemplo de respuesta esperada:

```json
{
  "productId": 1,
  "marketingName": "Super Laptop Gamer",
  "stockInfo": {
    "productId": 1,
    "stock": 50,
    "sku": "LAPTOP-DELL"
  },
  "source": "Live from Microservice"
}
```

---

## ▶️ Cómo Ejecutar la Solución Localmente

1. **Clonar el repositorio**

```bash
git clone https://github.com/CSA-DanielVillamizar/190304014-1.git
cd 190304014-1
```

2. **Abrir en Visual Studio**

- Abrir la solución `Itm.Store.System.sln`.

3. **Configurar proyectos de inicio múltiples**

- Clic derecho sobre la **solución** → `Propiedades`.
- Opción: `Proyectos de inicio múltiples`.
- Seleccionar `Itm.Inventory.Api` y `Itm.Product.Api` con acción `Iniciar`.

4. **Verificar puertos**

- Ejecutar la solución.
- Revisar en qué puerto corre `Itm.Inventory.Api` (por ejemplo, `http://localhost:5273`).
- Confirmar que el `BaseAddress` en `Itm.Product.Api` apunta a ese mismo puerto.

5. **Probar el flujo completo**

- Abrir Swagger de `Itm.Product.Api`.
- Probar el endpoint `GET /api/products/{id}/check-stock` con `id = 1`.

---

## ✅ Qué Aprendemos con Este Ejemplo

- Cómo separar responsabilidades en microservicios.
- Cómo definir **contratos** de intercambio de datos usando DTOs.
- Cómo usar `HttpClientFactory` + `Microsoft.Extensions.Http.Resilience` para construir servicios más robustos.
- Cómo manejar errores controladamente en llamadas entre servicios.

---

## 🚀 Próximos Pasos (para futuras clases)

- Agregar **seguridad** entre microservicios (API Keys, OAuth2, etc.).
- Introducir **observabilidad** (logging estructurado, métricas, tracing distribuido).
- Persistencia real con **bases de datos** por microservicio.
- Mensajería asíncrona (colas / eventos) para desacoplar aún más los módulos.

---

## 🧪 Retos para el Estudiante

1. **Agregar nuevo producto al inventario**  
   Extiende `Itm.Inventory.Api` para incluir un tercer producto en la lista en memoria y verifica que el orquestador lo consuma correctamente.

2. **DTO extendido**  
   Agrega un nuevo campo al `InventoryDto` (por ejemplo, `Location` o `Warehouse`) y actualiza tanto `Itm.Inventory.Api` como el `record InventoryResponse` en `Itm.Product.Api`. Prueba que el nuevo campo fluya de extremo a extremo.

3. **Manejo de producto sin stock**  
   Modifica el orquestador para que, cuando `stock` sea `0`, devuelva un mensaje claro en la respuesta (por ejemplo, `"status": "OutOfStock"`).

4. **Simulación de fallo controlado**  
   Cambia temporalmente el `BaseAddress` del `InventoryClient` a un puerto donde no haya servicio escuchando y observa cómo responde el endpoint orquestador. Mejora el mensaje de error para el usuario.

5. **Timeouts experimentales**  
   Reduce el `Timeout` del `HttpClient` a `1` segundo e introduce artificialmente un `Task.Delay` en `Itm.Inventory.Api` para simular lentitud. Analiza el comportamiento y discute qué valores de timeout serían razonables en producción.

6. **Separar configuración en appsettings**  
   Extrae la URL base de `InventoryClient` a `appsettings.json` y léela desde la configuración de .NET. Piensa cómo esto ayuda a mover la solución entre ambientes (dev, QA, prod).

7. **Diseño de endpoint adicional**  
   Diseña (aunque no lo implementes totalmente) un nuevo endpoint en `Itm.Product.Api` que combine información de precios, inventario y un futuro microservicio de recomendaciones. Escribe el contrato JSON esperado y justifica tus decisiones.

---

> Este repo está pensado como material educativo para estudiantes que se inician en Arquitectura de Software moderna con .NET. El foco no es solo "hacer que funcione", sino **entender por qué** se toman ciertas decisiones de diseño.
