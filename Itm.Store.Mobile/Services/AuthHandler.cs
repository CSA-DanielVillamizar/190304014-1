using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itm.Store.Mobile.Services;

// DelegatingHandler: Es un "Peaje" por donde pasan las peticiones HTTP antes de salir a internet, o antes de llegar a la aplicación. Es una forma de interceptar las peticiones y respuestas HTTP para agregar lógica personalizada, como autenticación, registro, manejo de errores, etc. En este caso, el AuthHandler se utilizará para agregar un token de autenticación a las peticiones HTTP antes de que sean enviadas al servidor.

public class AuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Buscamos el token en la bóveda de seguridad del celular (Secure Storage).
        var token = await SecureStorage.Default.GetAsync("jwt_token");

        // 2. Si el usuario esta autenticado (hay token), se lo pegamos a la petición como autorización.
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // 3. Continuamos con la petición hacia el Gateway.

        return await base.SendAsync(request, cancellationToken);

    }
}