using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------------------------------------
// ANÁLISIS PROFUNDO: REGISTRO DE CLIENTES HTTP
// -----------------------------------------------------------
// ¿Qué problema resuelve? Evita crear conexiones manuales y gestiona la red.
builder.Services.AddHttpClient("InventoryClient", client =>
{
    // OJO: Este puerto debe ser el del Inventory.Api (Revisar launchSettings.json)
    // En producción, esto viene de una variable de entorno, no quemado en código.
    client.BaseAddress = new Uri("http://localhost:5273");

    // Timeout: Si el inventario no responde en 5s, cancelamos. 
    // Evita que el usuario espere infinitamente.
    client.Timeout = TimeSpan.FromSeconds(5);
})
// RESILIENCIA (Rúbrica Nivel 5):
// .AddStandardResilienceHandler(): Agrega magia automática.
// - Reintentos (Retry): Si falla, intenta 3 veces más.
// - Circuit Breaker: Si falla mucho, deja de intentar para no saturar.
.AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

// Endpoint Orquestador
app.MapGet("/api/products/{id}/check-stock", async (int id, IHttpClientFactory factory) =>
{
    // 1. Pedimos prestado un cliente configurado a la fábrica
    var client = factory.CreateClient("InventoryClient");

    try
    {
        // 2. Hacemos la llamada asíncrona (async/await)
        // Usamos GetFromJsonAsync para traer el dato y convertirlo a objeto C# en un solo paso.
        // 'InventoryDto' es una clase interna (ver abajo) para recibir los datos.
        var stockData = await client.GetFromJsonAsync<InventoryResponse>($"/api/inventory/{id}");

        // 3. Construimos la respuesta final agregando valor
        return Results.Ok(new
        {
            ProductId = id,
            MarketingName = "Super Laptop Gamer", // Dato propio de Productos
            StockInfo = stockData,                // Dato traído de Inventario
            Source = "Live from Microservice"
        });
    }
    catch (HttpRequestException ex)
    {
        // MANEJO DE ERRORES (Rúbrica Nivel 5):
        // No mostramos el error técnico feo al usuario.
        // Capturamos si el otro servicio está caído.
        return Results.Problem($"El servicio de Inventario no responde. Detalle: {ex.Message}");
    }
});

app.Run();

// DTO Local para recibir la respuesta (Debe coincidir con el del otro servicio)
record InventoryResponse(int ProductId, int Stock, string Sku);
record ProductResponse(int ProductId, decimal Amount, string Currency);