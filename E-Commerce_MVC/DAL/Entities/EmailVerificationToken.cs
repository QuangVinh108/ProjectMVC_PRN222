using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class EmailVerificationToken
{
    public int TokenId { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }

    public virtual User User { get; set; } = null!;
}
