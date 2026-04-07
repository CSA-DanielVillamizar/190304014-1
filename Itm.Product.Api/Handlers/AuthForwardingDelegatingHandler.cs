using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Itm.Product.Api.Handlers;

/// <summary>
/// DelegatingHandler que propaga el encabezado Authorization de la petición entrante
/// hacia las peticiones HTTP salientes del Product.Api (por ejemplo, hacia Inventory.Api).
/// Esto permite que el mismo JWT que validó el Gateway llegue hasta los microservicios internos.
/// </summary>
public class AuthForwardingDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthForwardingDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Tomamos el encabezado Authorization de la petición que llegó al Product.Api
        var authHeader = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"]
            .FirstOrDefault();

        // Si existe y aún no está presente en la petición saliente, lo agregamos
        if (!string.IsNullOrEmpty(authHeader) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
