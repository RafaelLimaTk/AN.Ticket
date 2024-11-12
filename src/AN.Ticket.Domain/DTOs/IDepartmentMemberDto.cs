namespace AN.Ticket.Domain.DTOs;
public interface IDepartmentMemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
