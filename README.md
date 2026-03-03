# 190304014-1 - ITM Store System

Repositorio de la clase de Arquitectura de Software impartida por **Daniel Villamizar**.

Este proyecto corresponde a la **CLASE 1: Fundamentos de Arquitectura Distribuida y Orquestación**, donde transformamos un escenario monolítico en una arquitectura basada en microservicios usando .NET 8 y Minimal APIs.

---

## Objetivo de la Clase

Tomar el caso de una tienda ficticia (**ITM-Tech Store**) que colapsa en Black Friday debido a un **monolito acoplado**, y rediseñarlo en una arquitectura distribuida donde cada módulo pueda fallar sin tumbar a los demás.

- Entender la diferencia entre **Monolito** y **Microservicios**.
- Introducir el concepto de **acoplamiento** y cómo reducirlo.
- Diseñar contratos de comunicación usando **DTOs (Data Transfer Objects)**.
- Usar `HttpClientFactory` y políticas de **resiliencia** para comunicar microservicios.

---

## Escenario de Negocio: "ITM-Tech Store"

A las 00:01 del Black Friday, la tienda lanza oferta de laptops al 50%. El sistema es monolítico:

- Un solo proyecto ASP.NET con todo junto: precios, pagos, bodega, usuarios, etc.
- Miles de usuarios entran a ver precios.
- El módulo de precios satura el servidor.
- Como todo vive en el mismo proceso, también se caen **pagos** y **logística**.
- Nadie puede comprar, ni despachar pedidos.

**Conclusión:** Si un módulo se cae, se lleva todo por delante. Necesitamos **microservicios**.

---

## Conceptos Clave Trabajados en Clase

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

## Estructura de la Solución

Solución: `Itm.Store.System`

Proyectos principales:

- `Itm.Inventory.Api` – Microservicio dueño del **stock** de productos.
- `Itm.Product.Api` – Microservicio **orquestador**, que consulta a Inventario vía HTTP.

---

## Tecnologías y Requisitos

- **.NET SDK:** 8.0
- **IDE recomendado:** Visual Studio 2022+ (carga de trabajo "Desarrollo ASP.NET y Web").
- **Estilo de API:** Minimal APIs.
- **Paquetes NuGet usados:**
  - `Microsoft.AspNetCore.OpenApi`
  - `Microsoft.Extensions.Http.Resilience`

---

## Itm.Inventory.Api (Servicio de Inventario)

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

## Itm.Product.Api (Orquestador de Productos)

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

## Cómo Ejecutar la Solución Localmente

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

## Qué Aprendemos con Este Ejemplo

- Cómo separar responsabilidades en microservicios.
- Cómo definir **contratos** de intercambio de datos usando DTOs.
- Cómo usar `HttpClientFactory` + `Microsoft.Extensions.Http.Resilience` para construir servicios más robustos.
- Cómo manejar errores controladamente en llamadas entre servicios.

---

## Próximos Pasos (para futuras clases)

- Agregar **seguridad** entre microservicios (API Keys, OAuth2, etc.).
- Introducir **observabilidad** (logging estructurado, métricas, tracing distribuido).
- Persistencia real con **bases de datos** por microservicio.
- Mensajería asíncrona (colas / eventos) para desacoplar aún más los módulos.

---

## Retos para el Estudiante

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

## Reto Resuelto / Solución de Referencia: Itm.Order.Api

Como ejemplo completo de orquestación entre microservicios, el proyecto `Itm.Order.Api` implementa un flujo de creación de órdenes de compra.

- Tipo de proyecto: ASP.NET Core Web API (Minimal API).
- Endpoint principal: `POST /api/orders`.
- Entrada (body JSON):

```json
{
  "productId": 1,
  "quantity": 2
}
```

### Flujo de Orquestación

1. `Itm.Order.Api` recibe la orden con `ProductId` y `Quantity`.
2. Usa `IHttpClientFactory` para crear:
   - `InventoryClient` → consulta a `Itm.Inventory.Api` (`GET /api/inventory/{productId}`).
   - `PriceClient` → consulta a `Itm.Price.Api` (`GET /api/prices/{productId}`).
3. Ejecuta ambas llamadas en paralelo con `Task.WhenAll`.
4. Valida que:
   - El producto exista en inventario.
   - El precio exista en el servicio de precios.
   - El stock sea suficiente para la cantidad solicitada.
5. Calcula el total: `Total = UnitPrice * Quantity`.
6. Devuelve una "factura" de orden con un `OrderId` generado:

```json
{
  "orderId": "<guid>",
  "product": "LAPTOP-DELL",
  "quantity": 2,
  "unitPrice": 2500000,
  "totalToPay": 5000000,
  "currency": "COP",
  "status": "Created"
}
```

Este proyecto sirve como solución de referencia para el reto "Construyendo Itm.Order.Api" y muestra buenas prácticas de orquestación, validación de reglas de negocio y uso de `HttpClientFactory` con resiliencia (`Microsoft.Extensions.Http.Resilience`).

---

### Pruebas de Caos para la SAGA de Pedidos

Para validar el comportamiento de la SAGA (acción + compensación) en `Itm.Order.Api`:

1. **Preparación**
   - Iniciar `Itm.Inventory.Api`.
   - Iniciar `Itm.Order.Api`.

2. **Verificar estado inicial**
   - En Swagger de `Itm.Inventory.Api`, llamar a `GET /api/inventory/1`.
   - Anotar el stock inicial del producto 1 (por ejemplo, `50`).

3. **Ejecutar órdenes con fallo simulado**
   - Abrir Swagger de `Itm.Order.Api`.
   - Llamar a `POST /api/orders` con este cuerpo:

     ```json
     {
       "productId": 1,
       "quantity": 10
     }
     ```

   - Ejecutar varias veces. Aproximadamente la mitad de las veces el "pago" fallará (mensaje de fondos insuficientes) y la API responderá indicando que el stock fue devuelto.

4. **Verificar la compensación**
   - Cada vez que el pago falle y la respuesta indique que el stock fue devuelto:
     - Volver a Swagger de `Itm.Inventory.Api`.
     - Llamar de nuevo a `GET /api/inventory/1`.
     - El stock debe permanecer igual al valor inicial (por ejemplo, `50`).
   - En la consola de `Itm.Inventory.Api` aparecerán mensajes similares a:

     ```text
     [COMPENSACIÓN] Se devolvieron 10 unidades del producto LAPTOP-DELL. Nuevo Stock: 50
     ```

5. **Sin SAGA (qué ocurriría)**
   - Si no existiera la llamada de compensación a `/api/inventory/release`, el stock quedaría reducido (por ejemplo, `40`) aunque el pago haya fallado.
   - La SAGA garantiza que, ante un fallo en el flujo de negocio, el sistema vuelve a un estado consistente.

---

## Clase 5: API Gateway (YARP) y Seguridad Distribuida con JWT

En la Clase 5 agregamos dos piezas clave al ecosistema:

- `Itm.Gateway.Api` – API Gateway / Reverse Proxy usando **YARP**.
- Seguridad distribuida con **JWT** sobre `Itm.Inventory.Api`.

El objetivo es:

- Exponer **una sola URL pública** al cliente (Gateway) y ocultar los puertos internos.
- Proteger los microservicios con un **brazalete VIP** (JWT) para que solo clientes autorizados puedan consultar inventario.

---

### Itm.Gateway.Api – API Gateway con YARP

Proyecto muy liviano que actúa como "Recepcionista del Hotel": recibe todas las peticiones del cliente y las enruta hacia los microservicios reales.

#### Paquete utilizado

- `Yarp.ReverseProxy`

#### Program.cs

Archivo: `Itm.Gateway.Api/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args); // La creación del builder

// 1. Agregamos YARP a la caja de herramientas (DI)
// Le decimos que lea la configuración del archivo appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// 2. Activamos el middleware de YARP
app.MapReverseProxy();

app.Run();
```

#### Configuración de rutas y clusters (appsettings.json)

Archivo: `Itm.Gateway.Api/appsettings.json`

Ejemplo de configuración para exponer `Inventory.Api` como `/bodega` desde el Gateway:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "inventory-route": {
        "ClusterId": "inventory-cluster",
        "Match": {
          "Path": "/bodega/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/inventory/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "inventory-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5000" // Puerto donde corre Itm.Inventory.Api
          }
        }
      }
    }
  }
}
```

Con esto, una llamada del cliente a:

- `GET http://localhost:7000/bodega/1`

es traducida por YARP a:

- `GET http://localhost:5000/api/inventory/1`

El cliente nunca ve el puerto interno `5000`, solo conoce el Gateway.

---

### Seguridad con JWT en Itm.Inventory.Api

En `Itm.Inventory.Api` se agregó autenticación y autorización con **JWT Bearer**. El endpoint de lectura de inventario ahora requiere un token válido.

#### Paquete utilizado

- `Microsoft.AspNetCore.Authentication.JwtBearer`

#### Configuración JWT en appsettings.json

Archivo: `Itm.Inventory.Api/appsettings.json`

```json
{
  "JwtSettings": {
    "Issuer": "ItmIdentityServer",
    "Audience": "ItmStoreApis",
    "SecretKey": "ITM-Super-Secret-Key-For-JWT-Class-2026-Nivel5"
  }
}
```

> Nota: En un entorno real, la `SecretKey` vendría de variables de entorno o un secret manager, no se subiría a GitHub.

#### Program.cs con seguridad JWT

Archivo: `Itm.Inventory.Api/Program.cs`

Fragmento relevante:

```csharp
using System.Text;
using Itm.Inventory.Api.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bloque de seguridad JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

var inventoryDb = new List<InventoryDto>
{
    new(1, 50, "LAPTOP-DELL"),
    new(2, 0,  "MOUSE-GAMER")
};

app.MapGet("/api/inventory/{id}", (int id) =>
{
    var item = inventoryDb.FirstOrDefault(p => p.ProductId == id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
})
.RequireAuthorization();
```

Los endpoints `POST /api/inventory/reduce` y `POST /api/inventory/release` quedan públicos por ahora, pero también se pueden proteger añadiendo `.RequireAuthorization()` si el escenario lo requiere.

---

## Guía Paso a Paso para Probar Gateway + JWT

### 1. Requisitos previos

- .NET 8 SDK instalado.
- Visual Studio 2022+ (o VS Code) configurado.
- Postman / Insomnia (opcional pero recomendado).

### 2. Levantar Itm.Inventory.Api en puerto 5000

Opción A (Visual Studio):

- Establecer `Itm.Inventory.Api` como proyecto de inicio.
- Verificar en `launchSettings.json` que el puerto sea `5000` (o ajustarlo).
- Ejecutar el proyecto.

Opción B (CLI):

```powershell
cd C:\190304014-1\Itm.Store.System
dotnet run --project Itm.Inventory.Api\Itm.Inventory.Api.csproj --urls http://localhost:5000
```

### 3. Probar la seguridad JWT directamente en Inventory

1. Sin token:
   - Navega a `http://localhost:5000/api/inventory/1`.
   - Resultado esperado: **401 Unauthorized**.

2. Con token (para pruebas manuales):
   - Ve a https://jwt.io.
   - En el payload (JSON morado), coloca por lo menos:

     ```json
     {
       "iss": "ItmIdentityServer",
       "aud": "ItmStoreApis"
     }
     ```

   - En la sección de firma (Verify Signature), usa la `SecretKey` del `appsettings.json`:

     ```text
     ITM-Super-Secret-Key-For-JWT-Class-2026-Nivel5
     ```

   - Copia el token generado (cadena larga `eyJ...`).
   - En Postman/Insomnia, crea una petición `GET http://localhost:5000/api/inventory/1`.
   - En la pestaña **Authorization**, selecciona `Bearer Token` y pega el token.
   - Envía la petición.
   - Resultado esperado: **200 OK** con el JSON del inventario.

### 4. Levantar el Gateway en puerto 7000

Opción A (Visual Studio):

- Establecer `Itm.Gateway.Api` como proyecto de inicio.
- Verificar el puerto en `launchSettings.json` (por ejemplo, `7000`).
- Ejecutar el proyecto (asegúrate de que Inventory siga corriendo).

Opción B (CLI):

```powershell
cd C:\190304014-1\Itm.Store.System
dotnet run --project Itm.Gateway.Api\Itm.Gateway.Api.csproj --urls http://localhost:7000 --no-build
```

> Usamos `--no-build` para evitar re-compilar otros proyectos que ya están ejecutándose.

### 5. Probar el Gateway (sin seguridad en el propio Gateway)

En este momento, el Gateway solo enruta y no valida JWT. La validación ocurre en `Itm.Inventory.Api`.

1. Llamada sin token:
   - `GET http://localhost:7000/bodega/1`.
   - El Gateway traduce a `GET http://localhost:5000/api/inventory/1`.
   - Como Inventory exige JWT, la respuesta final será **401 Unauthorized**.

2. Llamada con token (desde cliente HTTP):
   - Usar el mismo token JWT generado en el paso 3.
   - En Postman/Insomnia, crear `GET http://localhost:7000/bodega/1`.
   - Enviar header: `Authorization: Bearer <tu_token>`.
   - Resultado esperado: **200 OK** con el inventario, enrutable a través del Gateway.

Con esto, el estudiante puede comprobar:

- Cómo el Gateway oculta los microservicios internos.
- Cómo funciona la seguridad distribuida con JWT en un ecosistema de microservicios.

---

## Licencia

Este proyecto se distribuye bajo la licencia MIT. Para más detalles, consulte el archivo `LICENSE` en la raíz del repositorio.

---

> Este repo está pensado como material educativo para estudiantes que se inician en Arquitectura de Software moderna con .NET. El foco no es solo "hacer que funcione", sino **entender por qué** se toman ciertas decisiones de diseño.
