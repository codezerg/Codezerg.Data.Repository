using LinqToDB.Mapping;

namespace Codezerg.Data.Repository.Example.Models;

/// <summary>
/// Example order entity demonstrating relationships
/// </summary>
[Table("Orders")]
public class Order
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    
    public int CustomerId { get; set; }
    
    public DateTime OrderDate { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public OrderStatus Status { get; set; }
    
    public string ShippingAddress { get; set; } = string.Empty;
    
    public List<OrderItem> Items { get; set; } = new();
    
    public override string ToString()
    {
        return $"Order #{Id}: Customer {CustomerId} - ${TotalAmount:F2} ({Status})";
    }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Order line item
/// </summary>
[Table("OrderItems")]
public class OrderItem
{
    [PrimaryKey, Identity]
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal Subtotal => Quantity * UnitPrice;
    
    public override string ToString()
    {
        return $"OrderItem: Product {ProductId} x{Quantity} @ ${UnitPrice:F2}";
    }
}