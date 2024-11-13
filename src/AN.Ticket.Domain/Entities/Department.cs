using AN.Ticket.Domain.Entities.Base;
using AN.Ticket.Domain.Enums;

namespace AN.Ticket.Domain.Entities;
public class Department : EntityBase
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string? Description { get; private set; }
    public DepartmentStatus Status { get; private set; }

    public ICollection<DepartmentMember> Members { get; private set; }

    protected Department()
    {
        Members = new List<DepartmentMember>();
    }

    public Department(string name, string code, string description, DepartmentStatus status)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("Nome do departamento é obrigatório", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException("Código do departamento é obrigatório", nameof(code));

        Name = name;
        Code = code;
        Description = description;
        Status = status;
        Members = new List<DepartmentMember>();
    }

    public void UpdateStatus(DepartmentStatus status)
    {
        Status = status;
    }

    public void AddMember(DepartmentMember member)
    {
        Members.Add(member);
    }

    public void RemoveMember(DepartmentMember member)
    {
        Members.Remove(member);
    }

    public void UpdateDepartment(string name, string code, string description, DepartmentStatus status)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("Nome do departamento é obrigatório", nameof(name));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException("Código do departamento é obrigatório", nameof(code));

        Name = name;
        Code = code;
        Description = description;
        Status = status;
    }
}
