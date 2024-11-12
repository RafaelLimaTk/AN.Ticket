using AN.Ticket.Domain.Entities.Base;

namespace AN.Ticket.Domain.Entities;
public class DepartmentMember : EntityBase
{
    public Guid DepartmentId { get; private set; }
    public Department Department { get; private set; }

    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    public Guid? ContactId { get; private set; }
    public Contact? Contact { get; private set; }

    protected DepartmentMember() { }

    public DepartmentMember(Guid departmentId, Guid? userId = null, Guid? contactId = null)
    {
        if (departmentId == Guid.Empty) throw new ArgumentException("DepartmentId não pode ser vazio.", nameof(departmentId));
        if (userId == null && contactId == null) throw new ArgumentException("Deve ser fornecido UserId ou ContactId.");

        DepartmentId = departmentId;
        UserId = userId;
        ContactId = contactId;
    }

    public void AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId não pode ser vazio.", nameof(userId));

        UserId = userId;
        ContactId = null;
    }

    public void AssignToContact(Guid contactId)
    {
        if (contactId == Guid.Empty) throw new ArgumentException("ContactId não pode ser vazio.", nameof(contactId));

        ContactId = contactId;
        UserId = null;
    }

    public void CancelAssignment()
    {
        UserId = null;
        ContactId = null;
    }
}
