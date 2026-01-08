using System;
using System.Collections.Generic;

namespace TestAPI.Models;

public partial class User
{
    public byte UserId { get; set; }

    public string? Username { get; set; }

    public string? UserLogin { get; set; }

    public string? UserPassword { get; set; }

    public byte? UserRole { get; set; }

    public virtual Role? UserRoleNavigation { get; set; }
}
