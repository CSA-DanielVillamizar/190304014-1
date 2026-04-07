namespace Itm.Inventory.Api.Core.Interfaces;

/// <summary>
/// Abstracción de acceso al usuario actual. Permite a la capa de negocio
/// obtener datos del usuario sin depender de HTTP o de detalles web.
/// </summary>
public interface ICurrentUserService
{
    string ObtenerEmailUsuario();
    string ObtenerRolUsuario();
}
