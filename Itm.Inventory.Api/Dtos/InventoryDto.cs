namespace Itm.Inventory.Api.Dtos;

// ANÁLISIS DE CÓDIGO:
// 1. 'public record': Usamos record en vez de class.
//    ¿Por qué? Porque son INMUTABLES. Una vez creados, los datos no cambian.
//    Es más rápido y seguro para transferir datos.
// 2. Parámetros (int ProductId...): Define la estructura del JSON que enviaremos.
public record InventoryDto(int ProductId, int Stock, string Sku);