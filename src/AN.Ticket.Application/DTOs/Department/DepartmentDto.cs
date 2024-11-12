using AN.Ticket.Domain.DTOs;
using AN.Ticket.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AN.Ticket.Application.DTOs.Department;
public class DepartmentDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O Nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome do departamento deve ter no máximo 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; }

    [Required(ErrorMessage = "O Código é obrigatório.")]
    [StringLength(10, ErrorMessage = "O código do departamento deve ter no máximo 10 caracteres.")]
    [Display(Name = "Código")]
    public string Code { get; set; }

    [Display(Name = "Descrição")]
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "O Status é obrigatório.")]
    [Display(Name = "Status")]
    public DepartmentStatus Status { get; set; }

    public int MembersCount { get; set; }
    public List<DepartmentMemberDto> Members { get; set; } = new List<DepartmentMemberDto>();
}

public class DepartmentMemberDto : IDepartmentMemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
