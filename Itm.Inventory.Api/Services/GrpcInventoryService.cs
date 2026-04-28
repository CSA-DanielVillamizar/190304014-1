using Grpc.Core;
using Itm.Inventory.Api.Protos;

namespace Itm.Inventory.Api.Services;

public class GrpcInventoryService : InventoryService.InventoryServiceBase
{
    public override Task<StockResponse> CheckStock(StockRequest request, ServerCallContext context)
    {
        // En un caso real, aquí irías a la base de datos de SQL o Redis
        var stockActual = (request.ProductId == 1) ? 10 : 0;

        return Task.FromResult(new StockResponse
        {
            ProductId = request.ProductId,
            Stock = stockActual,
            IsAvailable = stockActual > 0
        });
    }
}
