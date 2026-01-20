using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class WishlistProduct
{
    public int WishlistProductId { get; set; }

    public int WishlistId { get; set; }

    public int ProductId { get; set; }

    public DateTime AddedAt { get; set; }

    public string? Note { get; set; }

    public string? Image { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Wishlist Wishlist { get; set; } = null!;
}
