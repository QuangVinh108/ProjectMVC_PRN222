using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Payment? Payment { get; set; }

    public virtual Shipping? Shipping { get; set; }

    public virtual User User { get; set; } = null!;
}
