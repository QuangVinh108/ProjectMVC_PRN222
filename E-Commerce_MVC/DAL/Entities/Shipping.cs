using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Shipping
{
    public int ShippingId { get; set; }

    public int OrderId { get; set; }

    public string Address { get; set; } = null!;

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? PostalCode { get; set; }

    public string? Carrier { get; set; }

    public string? TrackingNumber { get; set; }

    public DateTime? ShippedDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public virtual Order Order { get; set; } = null!;
}
