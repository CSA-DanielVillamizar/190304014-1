using Itm.Store.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Itm.Store.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });


        // Registro de Arquitectura Nivel 5: Inyección de Dependencias

        // 1. Registramos nuestro "Peaje" de seguridad (AuthHandler) para que se ejecute en cada petición HTTP.
        builder.Services.AddTransient<AuthHandler>();

        // 2. Registramos el cliente HTTP apuntando unicamente al API Gateway, y le decimos que use el AuthHandler para agregar el token a cada petición.

        builder.Services.AddHttpClient("GatewayClient", client =>
        {

            // El truco del emulador 10.0.2.2 es la ip para llegar a nuestro PC desde el emulador de Android. Es como si fuera localhost pero para el emulador.
            // Para probar localmente a través del Ingress / Gateway:
            client.BaseAddress = new Uri("http://10.0.2.2"); // Usa la IP especial de Android para localhost


        })
.AddHttpMessageHandler<AuthHandler>(); // Le estamos conectando el peaje automático

        // 3. Registramos la vista principal de la aplicación (MainPage) para que se muestre al iniciar la app.
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
