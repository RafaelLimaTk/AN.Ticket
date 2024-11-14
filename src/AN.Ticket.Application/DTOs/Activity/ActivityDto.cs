using AN.Ticket.Application.DTOs.Contact;
using AN.Ticket.Application.DTOs.Ticket;
using AN.Ticket.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AN.Ticket.Application.DTOs.Activity;
public class ActivityDto
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "O campo Assunto é obrigatório.")]
    [StringLength(100, ErrorMessage = "O campo Assunto pode ter no máximo 100 caracteres.")]
    [Display(Name = "Assunto")]
    public string Subject { get; set; }

    [Required(ErrorMessage = "O campo Tipo é obrigatório.")]
    [Display(Name = "Tipo de Atividade")]
    public ActivityType Type { get; set; }

    [StringLength(500, ErrorMessage = "O campo Descrição pode ter no máximo 500 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Description { get; set; }

    [Display(Name = "Data Agendada")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "O campo Data Agendada é obrigatório.")]
    public DateTime ScheduledDate { get; set; }

    [Required(ErrorMessage = "O campo Duração é obrigatório.")]
    [Display(Name = "Duração")]
    [DataType(DataType.Time)]
    public TimeSpan Duration { get; set; }

    [Display(Name = "Prioridade")]
    [Required(ErrorMessage = "O campo Prioridade é obrigatório.")]
    public ActivityPriority Priority { get; set; }

    [Display(Name = "Status")]
    [Required(ErrorMessage = "O campo Status é obrigatório.")]
    public ActivityStatus Status { get; set; }

    public Guid? ContactId { get; set; }

    [Display(Name = "Contato")]
    public ContactDto? Contact { get; set; }

    [Required(ErrorMessage = "O campo Ticket é obrigatório.")]
    public Guid TicketId { get; set; }

    [Display(Name = "Ticket")]
    public TicketDto? Ticket { get; set; }

    public bool IsEditTicket { get; set; }

    public bool IsScreenTicket { get; set; }
}
