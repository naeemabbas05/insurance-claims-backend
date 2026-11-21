namespace Chubb.Apac.ClaimsManagement.Domain.Common;

public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    protected Entity(Guid id)
    {
        DomainRule.Require(id != Guid.Empty, "Entity identifiers cannot be empty.");
        Id = id;
    }

    public Guid Id { get; private set; }
}
