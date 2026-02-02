namespace BLL.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Paid, Shipped, Delivered, Cancelled
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        
        public List<OrderItemDto> OrderItems { get; set; } = new();
        public PaymentDto? Payment { get; set; }
        public ShippingDto? Shipping { get; set; }
    }
}
