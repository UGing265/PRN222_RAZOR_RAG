using System;
using System.Collections.Generic;

namespace DAL.Entities;

public partial class Role
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<AccountRequest> AccountRequests { get; set; } = new List<AccountRequest>();
}
