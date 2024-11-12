using AN.Ticket.Domain.Entities.Base;
using AN.Ticket.Domain.EntityValidations;
using AN.Ticket.Domain.ValueObjects;

namespace AN.Ticket.Domain.Entities;

public class Contact : EntityBase
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Cpf { get; private set; }
    public string PrimaryEmail { get; private set; }
    public string SecondaryEmail { get; private set; }
    public string Phone { get; private set; }
    public string Mobile { get; private set; }
    public string Department { get; private set; }
    public string Title { get; private set; }
    public List<SocialNetwork> SocialNetworks { get; private set; }
    public Guid UserId { get; private set; }

    public User User { get; set; }
    public ICollection<AssetAssignment> AssetAssignments { get; private set; }
    public ICollection<DepartmentMember> Departments { get; private set; }

    protected Contact()
    {
        AssetAssignments = new List<AssetAssignment>();
        Departments = new List<DepartmentMember>();
    }

    public Contact(
        string firstName,
        string lastName,
        string primaryEmail,
        string secondaryEmail = null,
        string phone = null,
        string mobile = null,
        string department = null,
        string title = null
    )
    {
        if (string.IsNullOrEmpty(primaryEmail)) throw new EntityValidationException("Primary email is required.");

        FirstName = firstName;
        LastName = lastName;
        PrimaryEmail = primaryEmail;
        SecondaryEmail = secondaryEmail;
        Phone = phone;
        Mobile = mobile;
        Department = department;
        Title = title;
        SocialNetworks = new List<SocialNetwork>();
    }

    public void AddSocialNetwork(SocialNetwork socialNetwork)
    {
        if (socialNetwork == null) throw new EntityValidationException("Social network is required.");
        SocialNetworks.Add(socialNetwork);
    }

    public void AssignUser(Guid? userId)
    {
        if (!userId.HasValue) throw new EntityValidationException("User is required.");

        UserId = userId.Value;
    }

    public string GetFullName()
        => $"{FirstName} {LastName}";

    public void ChangedCpf(string cpf)
    {
        Cpf = cpf;
    }

    public void Update(
        string firstName,
        string lastName,
        string cpf,
        string primaryEmail,
        string secondaryEmail,
        string phone,
        string mobile,
        string department,
        string title
    )
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Cpf = cpf ?? throw new ArgumentNullException(nameof(cpf));
        PrimaryEmail = primaryEmail ?? throw new ArgumentNullException(nameof(primaryEmail));
        SecondaryEmail = secondaryEmail;
        Phone = phone;
        Mobile = mobile ?? throw new ArgumentNullException(nameof(mobile));
        Department = department;
        Title = title;
    }
}

