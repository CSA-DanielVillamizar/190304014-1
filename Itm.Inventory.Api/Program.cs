using System.Text;
using Itm.Inventory.Api.Dtos;
using Itm.Inventory.Api.Core.Interfaces;
using Itm.Inventory.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ZONA DE SERVICIOS (La Caja de Herramientas) ---
// Aquí le decimos a .NET qué capacidades tendrá nuestra API.
builder.Services.AddGrpc(); // Agregamos soporte para gRPC
builder.Services.AddEndpointsApiExplorer(); // Permite que Swagger analice los endpoints

// Registro de Swagger con seguridad JWT (botón Authorize)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Itm Inventory API", Version = "v1" });

    // Definimos el esquema de seguridad (candado visual)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en este formato: Bearer {su_token_aqui}"
    });

    // Requerimiento global para que Swagger use el token en las peticiones
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Bloque de seguridad JWT (JSON Web Tokens) - Opcional, pero recomendado para proteger la API

//1. Extraemos la configuración (No quemamos strings mágicos)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

//2. Registramos la autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true, // Valida que el token no haya expirado
            ValidateIssuerSigningKey = true, // Valida la firma del token
            IssuerSigningKey = new SymmetricSecurityKey(secretKey) // Clave secreta para validar la firma
        };
    });

//3. Agregamos autorización (incluimos una política de rol de Administrador)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
});

// 4. Servicios auxiliares para extraer información del usuario actual
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();



// --- 2. ZONA DE MIDDLEWARE (El Portero) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Activa el JSON de Swagger
    app.UseSwaggerUI(); // Activa la página web azul bonita
}

// Middleware de seguridad (JWT)

app.UseAuthentication(); // Verifica el token JWT en cada petición
app.UseAuthorization();    // Verifica los permisos del usuario

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
// GET /api/inventory/1 -> id=1

app.MapGet("/api/inventory/{id}", (int id, HttpContext httpContext, ILogger<Program> logger) =>
{
    // Extraemos el Correlation ID si viene de upstream (Gateway / Order.Api)
    var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? "SIN-ID";

    using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        // Lógica LINQ: Buscamos en la lista el primero que coincida con el ID.
        var item = inventoryDb.FirstOrDefault(p => p.ProductId == id);

        //  PATRÓN DE RESPUESTA HTTP:
        // Si existe (is not null) -> 200 OK con el dato.
        // Si no existe -> 404 NotFound.
        return item is not null ? Results.Ok(item) : Results.NotFound();
    }
})
.RequireAuthorization(); // Protegemos este endpoint, solo usuarios autenticados pueden acceder

// POST /api/inventory/reduce-stock -> Reduce el stock de un producto
// Solo usuarios con rol Administrador pueden reducir stock.
// Usamos [FromBody] para indicar que el dato viene en el cuerpo de la petición (JSON).
app.MapPost("/api/inventory/reduce", (ReduceStockDto request, ICurrentUserService currentUserService) =>
{
  // Auditoría básica usando la información del token JWT
    var email = currentUserService.ObtenerEmailUsuario();
    Console.WriteLine($"[AUDITORÍA] El usuario {email} intenta reducir stock del producto {request.ProductId}.");

    // 1. Buscamos el producto
    var item = inventoryDb.FirstOrDefault(p => p.ProductId == request.ProductId);

    // 2. Validamos que exista el producto (Reglas de Negocio)

    if (item is null)
    {
    return Results.NotFound(new { Error = "Producto no exister en bodega" });
        }
    if (item.Stock < request.Quantity)
    {
    // 400 Bad Request: No hay suficiente stock para reducir
    return Results.BadRequest(new { Error = "No hay suficiente stock para reducir", CurrentStock  = item.Stock });

}
// 3. Mutación de Estado (Restamos el stock)
// Nota: Como usamos 'record', que es inmutable, aquí hacemos un truco sucio
// modificando la lista directament para la clase.
// En la vida real (SQL), haríamos un UPDATE en la base de datos.
var index = inventoryDb.IndexOf(item);
    inventoryDb[index] = item with { Stock = item.Stock - request.Quantity }; // Crea una nueva instancia con el stock reducido

    // 4. Confirmación de la operación
return Results.Ok(new { Message = "Stock actualizado",NewStock = inventoryDb[index].Stock });
})
.RequireAuthorization("AdminOnly");

//DTO para devolver stock (El mismo de reducir  sirve, o creamos uno nuevo)
// Usamos el mismo DTO 'ReduceStockDto' (ProductId, Quantity) para la respuesta, pero podríamos crear uno específico si queremos más claridad.

app.MapPost("/api/inventory/release", (ReduceStockDto request) =>
{
    var item = inventoryDb.FirstOrDefault(p => p.ProductId == request.ProductId);
if (item is null) return Results.NotFound();
//Logica de Compensación (El Ctrl+Z del inventario)
// sumamos lo que habiamos reducido antes
var index = inventoryDb.IndexOf(item);
    inventoryDb[index] = item with { Stock = item.Stock + request.Quantity }; // Crea una nueva instancia con el stock aumentado
   Console.WriteLine($"[COMPENSACIÓN] Se devolvieron {request.Quantity} unidades al producto {item.Sku}. Nuevo stock: {inventoryDb[index].Stock}");
    return Results.Ok(new { Message = "Stock liberado por fallo de transacción", CurrentStock = inventoryDb[index].Stock });

});

app.MapGrpcService<GrpcInventoryService>();

app.Run();

