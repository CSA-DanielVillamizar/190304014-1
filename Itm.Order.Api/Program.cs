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

// Endpoint principal de creación de órdenes
app.MapPost("/api/orders", async (CreateOrderDto order, IHttpClientFactory factory) =>
{
    var inventoryClient = factory.CreateClient("InventoryClient");
    var priceClient = factory.CreateClient("PriceClient");

    // Llamadas en paralelo a Inventory y Price
    var stockTask = inventoryClient.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{order.ProductId}");
    var priceTask = priceClient.GetFromJsonAsync<PriceResponse>($"/api/prices/{order.ProductId}");

    await Task.WhenAll(stockTask, priceTask);

    var stock = stockTask.Result;
    var price = priceTask.Result;

    if (stock is null)
    {
        return Results.NotFound(new { Error = "Producto no encontrado en inventario." });
    }

    if (price is null)
    {
        return Results.Problem("No se pudo obtener el precio del producto.");
    }

    if (stock.Stock < order.Quantity)
    {
        return Results.BadRequest(new { Error = "No hay suficiente mercancía.", CurrentStock = stock.Stock });
    }

    var total = price.Amount * order.Quantity;

    var response = new
    {
        OrderId = Guid.NewGuid(),
        Product = stock.Sku,
        Quantity = order.Quantity,
        UnitPrice = price.Amount,
        TotalToPay = total,
        Currency = price.Currency,
        Status = "Created"
    };

    return Results.Ok(response);
});

app.Run();

// DTOs locales para orquestación
public record CreateOrderDto(int ProductId, int Quantity);

public record InventoryResponse(int ProductId, int Stock, string Sku);

public record PriceResponse(int ProductId, decimal Amount, string Currency);

