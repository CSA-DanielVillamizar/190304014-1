namespace Itm.Store.Mobile;

public partial class MainPage : ContentPage
{
    private readonly IHttpClientFactory _httpClientFactory;

    // Inyectamos la fábrica, tal como lo hacemos en el Backend
    public MainPage(IHttpClientFactory httpClientFactory)
    {
        InitializeComponent();
        _httpClientFactory = httpClientFactory;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Simulamos que fuimos a un servidor de Identidad (IdentityServer / Auth0)
        // y nos devolvió este JWT. En un caso real, haríamos un POST /api/login
        // IMPORTANTE: Este token fue generado en jwt.io con:
        // iss = "ItmIdentityServer", aud = "ItmStoreApis" y firmado con la SecretKey
        // "ITM-Super-Secret-Key-For-JWT-Class-2026-Nivel5".
        // Ejemplo original de simulación (NO usar):
        // string simulatedToken = "eyJhbGciOiJIUzI1NiIsInR... (token simulado)";

        string simulatedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJJdG1JZGVudGl0eVNlcnZlciIsImF1ZCI6Ikl0bVN0b3JlQXBpcyIsImVtYWlsIjoiYWRtaW5AaXRtLmVkdS5jbyIsInJvbGUiOiJBZG1pbmlzdHJhZG9yIn0.PaSdxe8NkHzbkrTA40janIgKn4gnVp63yWh_cenvUDw";

        //  SEGURIDAD NIVEL 5: Lo guardamos en la bóveda criptográfica del celular
        await SecureStorage.Default.SetAsync("jwt_token", simulatedToken);

        ResultLabel.Text = " ¡Token JWT guardado seguro en el dispositivo!";
        ResultLabel.TextColor = Colors.Green;
    }

    private async void OnGetDataClicked(object sender, EventArgs e)
    {
        try
        {
            ResultLabel.Text = "Consultando Gateway...";
            ResultLabel.TextColor = Colors.Orange;

            // 1. Pedimos el cliente configurado (Él ya sabe que debe ir al Gateway y usar el AuthHandler)
            var client = _httpClientFactory.CreateClient("GatewayClient");

            // 2. Hacemos la petición HTTP al Gateway (Ruta que creamos en clases pasadas)
            var response = await client.GetAsync("/api/products/1/check-stock");

            if (response.IsSuccessStatusCode)
            {
                // Si el Gateway y los microservicios responden 200 OK
                var data = await response.Content.ReadAsStringAsync();
                ResultLabel.Text = $" ÉXITO:\n{data}";
                ResultLabel.TextColor = Colors.Green;
            }
            else
            {
                ResultLabel.Text = $" ERROR {response.StatusCode}:\n{await response.Content.ReadAsStringAsync()}";
                ResultLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            // Atrapamos errores de red (Ej: El Gateway está apagado)
            ResultLabel.Text = $" ERROR DE RED:\n{ex.Message}";
            ResultLabel.TextColor = Colors.Red;
        }
    }
}