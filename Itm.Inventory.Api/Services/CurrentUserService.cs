using System.Security.Claims;
using Itm.Inventory.Api.Core.Interfaces;

namespace Itm.Inventory.Api.Services;

/// <summary>
/// Implementación de ICurrentUserService que obtiene la información del usuario
/// desde el HttpContext y el token JWT asociado a la petición.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string ObtenerEmailUsuario()
    {
        // Extraemos el correo directamente de los claims del token JWT
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }

    public string ObtenerRolUsuario()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
