using Itm.Inventory.Api.Dtos; // Importamos el DTO que acabamos de crear

var builder = WebApplication.CreateBuilder(args);

// --- 1. ZONA DE SERVICIOS (La Caja de Herramientas) ---
// Aquí le decimos a .NET qué capacidades tendrá nuestra API.
builder.Services.AddEndpointsApiExplorer(); // Permite que Swagger analice los endpoints
builder.Services.AddSwaggerGen();           // Genera la documentación visual

var app = builder.Build();

// --- 2. ZONA DE MIDDLEWARE (El Portero) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Activa el JSON de Swagger
    app.UseSwaggerUI(); // Activa la página web azul bonita
}

// --- 3. ZONA DE DATOS (Simulación de BD) ---
// Usamos una lista en memoria. En la vida real, aquí iría un 'DbContext' de Entity Framework.
var inventoryDb = new List<InventoryDto>
{
    new(1, 50, "LAPTOP-DELL"),
    new(2, 0,  "MOUSE-GAMER") // Stock 0 para probar lógica
};

// --- 4. ZONA DE ENDPOINTS (Las Rutas) ---
// MapGet: Define que responderemos a peticiones HTTP GET (Lectura).
// "/api/inventory/{id}": La URL. {id} es una variable.
app.MapGet("/api/inventory/{id}", (int id) =>
{
    // Lógica LINQ: Buscamos en la lista el primero que coincida con el ID.
    var item = inventoryDb.FirstOrDefault(p => p.ProductId == id);

    //  PATRÓN DE RESPUESTA HTTP:
    // Si existe (is not null) -> 200 OK con el dato.
    // Si no existe -> 404 NotFound.
    return item is not null ? Results.Ok(item) : Results.NotFound();
});

app.Run(); // Arranca el motor