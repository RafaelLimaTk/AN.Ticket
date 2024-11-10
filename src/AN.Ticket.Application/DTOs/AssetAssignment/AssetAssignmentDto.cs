namespace AN.Ticket.Application.DTOs.AssetAssignment;
public class AssetAssignmentDto
{
    public Guid AssetId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ContactId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}
