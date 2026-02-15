using System;
using System.Collections.Generic;

namespace APIWarehouse.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string Name { get; set; } = null!;

    public int Quantity { get; set; }
}
