using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itm.Shared.Events;

// Usamos 'record' porque los eventos son hechos históricos inmutables. NO PUEDEN CAMBIAR una vez creados.
// Lo que pasó, pasó. No podemos permitir que alguien edite el precio en pleno vuelo.

public record OrderCreatedEvent
(
    Guid OrderId,
    int ProductId,
    string CustomerEmail,
    decimal TotalAmount
    );


