using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Itm.Order.Api.Handlers;

// Interceptor que se ejecuta antes de que HttpClient envíe la petición saliente.
// Propaga el encabezado X-Correlation-ID desde la petición entrante al microservicio destino.
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Tomamos el X-Correlation-ID que llegó al Order.Api (por ejemplo, desde el Gateway)
        var correlationId = _httpContextAccessor.HttpContext?
            .Request.Headers["X-Correlation-ID"]
            .FirstOrDefault();

        // Lo propagamos a la petición saliente si existe y aún no está presente
        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains("X-Correlation-ID"))
        {
            request.Headers.Add("X-Correlation-ID", correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
