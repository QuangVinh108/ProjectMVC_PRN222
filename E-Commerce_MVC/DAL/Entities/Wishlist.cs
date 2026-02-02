using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Wishlist
{
    public int WishlistId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WishlistProduct> WishlistProducts { get; set; } = new List<WishlistProduct>();
}
