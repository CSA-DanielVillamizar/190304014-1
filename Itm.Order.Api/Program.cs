using System.Net.Http.Json;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registro de clientes HTTP hacia los otros microservicios
builder.Services
    .AddHttpClient("InventoryClient", client =>
    {
        // Puerto actual de Inventory.Api (ver launchSettings.json del proyecto Inventory)
        client.BaseAddress = new Uri("http://localhost:5273");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient("PriceClient", client =>
    {
        // TODO: Ajustar al puerto real de Price.Api cuando exista el proyecto
        client.BaseAddress = new Uri("http://localhost:5280");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint principal de creación de órdenes con lógica SAGA (acción + compensación)
app.MapPost("/api/orders", async (CreateOrderDto order, IHttpClientFactory factory) =>
{
    var invClient = factory.CreateClient("InventoryClient");

    // PASO 1: Intentar reservar Stock (acción directa sobre Inventario)
    var reduceResponse = await invClient.PostAsJsonAsync("/api/inventory/reduce", order);

    if (!reduceResponse.IsSuccessStatusCode)
    {
        return Results.BadRequest("No se pudo reservar el stock. Transacción abortada.");
    }

    // Si llegamos aquí, YA RESTAMOS EL STOCK. A partir de aquí necesitamos compensación si algo falla.
    try
    {
        // PASO 2: Procesar el Pago (simulación de fallo aleatorio)
        var random = new Random();
        var paymentSuccess = random.Next(0, 10) > 5; // Aprox. 50% de éxito

        if (!paymentSuccess)
        {
            throw new InvalidOperationException("Fondos insuficientes en la tarjeta.");
        }

        return Results.Ok(new { Message = "Orden creada y pagada exitosamente." });
    }
    catch (Exception ex)
    {
        // El pago falló, pero ya quitamos el stock: iniciamos la compensación tipo SAGA
        Console.WriteLine($"[ERROR] Falló el pago: {ex.Message}. Iniciando compensación...");

        var compensateResponse = await invClient.PostAsJsonAsync("/api/inventory/release", order);

        if (compensateResponse.IsSuccessStatusCode)
        {
            return Results.Problem("El pago falló. El stock fue devuelto correctamente. Intente de nuevo.");
        }

        // Peor escenario: falló el pago y también la compensación del stock
        Console.WriteLine("[CRITICAL] Falló la compensación. Datos inconsistentes, requiere intervención manual.");
        return Results.Problem("Error crítico del sistema. Contacte soporte.");
    }
});

app.Run();

// DTOs locales para orquestación
public record CreateOrderDto(int ProductId, int Quantity);

public record InventoryResponse(int ProductId, int Stock, string Sku);

public record PriceResponse(int ProductId, decimal Amount, string Currency);

// Simulación de DTO de Pago (para futuras extensiones de la SAGA)
public record PaymentDto(int OrderId, decimal Amount);

// -----------------------------------------------------------------------------
// EJEMPLO TEÓRICO (COMENTADO):
// Patrón SAGA coreografiado usando mensajería (RabbitMQ / Azure Service Bus / Kafka)
// -----------------------------------------------------------------------------
//
// Diferencia clave con la SAGA orquestada que sí implementamos arriba:
//
// - Orquestada (implementada):
//   `Itm.Order.Api` hace llamadas HTTP directas a `Inventory.Api` (y en un futuro a Payment).
//   Existe un "orquestador" que coordina el flujo y dispara las compensaciones.
//
// - Coreografiada (este ejemplo teórico):
//   No hay un servicio jefe. Cada microservicio reacciona a mensajes en una cola.
//   Order publica un evento "OrderCreated"; Inventory escucha ese evento, intenta
//   reservar stock y luego publica otro evento "StockReserved" o "StockRejected".
//   Payment escucha "StockReserved", intenta cobrar y publica "PaymentSucceeded"
//   o "PaymentFailed". Order escucha esos eventos y actualiza el estado de la orden.
//
// A continuación un EJEMPLO SIMPLIFICADO SOLO PARA ESTUDIO (NO SE EJECUTA):
//
// using Azure.Messaging.ServiceBus; // o el cliente de RabbitMQ/Kafka
// using System.Text.Json;
//
// app.MapPost("/api/orders/async", async (CreateOrderDto order, ServiceBusClient busClient) =>
// {
//     // 1. Generar identificador único de la orden
//     var orderId = Guid.NewGuid();
//
//     // 2. Construir el evento de dominio "OrderCreated"
//     var orderCreatedEvent = new
//     {
//         OrderId = orderId,
//         order.ProductId,
//         order.Quantity,
//         CreatedAt = DateTime.UtcNow
//     };
//
//     // 3. Publicar el evento en la cola/bus de mensajes
//     var sender = busClient.CreateSender("orders");
//     var body = JsonSerializer.Serialize(orderCreatedEvent);
//     var message = new ServiceBusMessage(body)
//     {
//         Subject = "OrderCreated"
//     };
//
//     await sender.SendMessageAsync(message);
//
//     // 4. Responder al cliente de forma asíncrona (procesamiento en background)
//     return Results.Accepted($"/api/orders/{orderId}", new
//     {
//         OrderId = orderId,
//         Status = "Pending",
//         Message = "La orden fue recibida y será procesada de manera asíncrona."
//     });
// });
//
// -----------------------------------------------------------------------------
// ¿Qué harían otros servicios en una SAGA coreografiada?
// -----------------------------------------------------------------------------
//
// Inventory.Api (pseudo-código):
//
// - Suscrito a la cola "orders" filtrando Subject = "OrderCreated".
// - Al recibir el evento:
//   1. Verifica el stock disponible.
//   2. Si hay stock suficiente, lo reserva y publica "StockReserved" en otra cola,
//      por ejemplo "order-events": { OrderId, ProductId, Quantity, Status = "Reserved" }.
//   3. Si no hay stock, publica "StockRejected": { OrderId, Reason = "OutOfStock" }.
//
// Payment.Api (pseudo-código):
//
// - Suscrito a "order-events" filtrando Subject = "StockReserved".
// - Al recibir el evento:
//   1. Intenta procesar el pago.
//   2. Si el pago tiene éxito, publica "PaymentSucceeded".
//   3. Si el pago falla, publica "PaymentFailed".
//   4. Inventory podría escuchar "PaymentFailed" para ejecutar la compensación
//      devolviendo el stock internamente.
//
// Order.Api escuchando eventos (pseudo-código):
//
// - Suscrito a "order-events":
//   - Si recibe "StockRejected": marca la orden como Cancelada por falta de stock.
//   - Si recibe "PaymentFailed": marca la orden como Fallida por pago.
//   - Si recibe "PaymentSucceeded": marca la orden como Completada.
//
// -----------------------------------------------------------------------------
// Puntos de discusión para los estudiantes:
//
// 1. Ventajas del enfoque coreografiado:
//    - Menor acoplamiento: Order no conoce directamente las URLs de Inventory/Payment.
//    - Alta escalabilidad: cada servicio escala leyendo de la cola.
//    - Flujo basado en eventos: fácil de extender (shipping, email, notificaciones, etc.).
//
// 2. Retos adicionales:
//    - Trazabilidad: el flujo pasa por varios servicios de forma asíncrona.
//    - Observabilidad crítica: se necesitan buenos logs, métricas y tracing distribuido.
//    - Diseño de eventos: hay que cuidar qué información viaja en cada mensaje.
//
// 3. Comparación con la SAGA orquestada (implementada en este archivo):
//    - Orquestada: más fácil de entender e implementar al inicio; acopla más los servicios.
//    - Coreografiada: más flexible y desacoplada, pero exige mejor infraestructura
//      de mensajería y observabilidad.


