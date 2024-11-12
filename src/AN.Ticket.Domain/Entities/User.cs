using AN.Ticket.Domain.Entities.Base;
using AN.Ticket.Domain.Enums;

namespace AN.Ticket.Domain.Entities;
public class User : EntityBase
{
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string? ProfilePicture { get; private set; }
    public UserRole Role { get; private set; }

    public ICollection<AssetAssignment> AssetAssignments { get; private set; }
    public ICollection<DepartmentMember> Departments { get; private set; }

    protected User()
    {
        AssetAssignments = new List<AssetAssignment>();
        Departments = new List<DepartmentMember>();
    }

    public User(
        Guid id,
        string fullName,
        string email,
        UserRole role,
        string profilePicture = null
    )
    {
        Id = id;
        FullName = fullName;
        Email = email;
        Role = role;
        ProfilePicture = profilePicture;
    }

    public void UpdateProfilePicture(string profilePicture)
        => ProfilePicture = profilePicture;

    public void UpdateFullName(string fullName)
        => FullName = fullName;

    public void UpdateEmail(string email)
        => Email = email;

    public void UpdateRole(UserRole role)
        => Role = role;
}
