using Domain.Entities.Abstract;

namespace Domain.Entities.Implement;

public class DomainUser : BaseEntity
{
    public string IdentityUserId { get; set; } = null!;
}
