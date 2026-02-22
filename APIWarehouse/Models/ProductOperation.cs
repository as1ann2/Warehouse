public class ProductOperation
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public string OperationType { get; set; } = "";

    public DateTime OperationDate { get; set; } = DateTime.Now;
}