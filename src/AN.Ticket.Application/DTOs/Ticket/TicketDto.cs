using AN.Ticket.Application.DTOs.Activity;
using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.DTOs.Attachment;
using AN.Ticket.Application.DTOs.InteractionHistory;
using AN.Ticket.Application.DTOs.SatisfactionRating;
using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Domain.Enums;

namespace AN.Ticket.Application.DTOs.Ticket;
public class TicketDto
{
    public Guid Id { get; set; }
    public int TicketCode { get; set; }
    public string ContactName { get; set; }
    public string AccountName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Subject { get; set; }
    public string EmailMessageId { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public Guid? UserId { get; private set; }
    public UserDto? User { get; private set; }
    public List<TicketMessageDto> Messages { get; set; }
    public List<ActivityDto>? Activities { get; set; }
    public List<InteractionHistoryDto> InteractionHistories { get; set; }
    public SatisfactionRatingDto SatisfactionRating { get; set; }
    public string? Classification { get; set; }
    public ICollection<AttachmentDto> Attachments { get; set; }
    public ICollection<AssetDto> Assets { get; set; }
    public string? Resolution { get; set; }
}
