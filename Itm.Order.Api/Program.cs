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

