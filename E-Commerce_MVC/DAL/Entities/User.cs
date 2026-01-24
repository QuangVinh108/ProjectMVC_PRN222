using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public string? GoogleId { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public string? LoginProvider { get; set; }

    public string? CccdNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? CccdFrontImage { get; set; }

    public bool IsIdentityVerified { get; set; }

    public string? IdentityRejectReason { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;

    public virtual Wishlist? Wishlist { get; set; }
}
