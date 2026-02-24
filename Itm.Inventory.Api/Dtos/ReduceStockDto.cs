namespace Itm.Inventory.Api.Dtos;

    // por qué: Solo necesitamos sabaer QUÉ producto y CUÁNTO reducir, no más.
    //No nECESITAMOS EL SKU ni el nombre d ela operación, solo el ID y la cantidad a reducir.

    public record ReduceStockDto(int ProductId, int Quantity);

