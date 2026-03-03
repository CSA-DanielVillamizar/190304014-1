var builder = WebApplication.CreateBuilder(args); // La creación del builder

//1. Agregamos YARP a la caja de herramientas (DI)
// Le decimos que lea la configuración del archivo appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

//2. Activamos el middleware de YARP
app.MapReverseProxy();

app.Run();