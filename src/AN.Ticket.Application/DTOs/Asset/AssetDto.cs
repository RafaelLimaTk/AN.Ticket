using AN.Ticket.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AN.Ticket.Application.DTOs.Asset;
public class AssetDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O Nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome do ativo deve ter no máximo 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; }

    [Required(ErrorMessage = "O Número de Série é obrigatório.")]
    [StringLength(100, ErrorMessage = "O número de série deve ter no máximo 100 caracteres.")]
    [Display(Name = "Número de Série")]
    public string SerialNumber { get; set; }

    [Required(ErrorMessage = "O Tipo de Ativo é obrigatório.")]
    [StringLength(50, ErrorMessage = "O tipo do ativo deve ter no máximo 50 caracteres.")]
    [Display(Name = "Tipo de Ativo")]
    public string AssetType { get; set; }

    [Required(ErrorMessage = "A Data de Compra é obrigatória.")]
    [Display(Name = "Data de Compra")]
    [DataType(DataType.Date)]
    public DateTime PurchaseDate { get; set; }

    [Required(ErrorMessage = "O Valor é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    [Display(Name = "Valor")]
    public decimal Value { get; set; }

    [Display(Name = "Descrição")]
    [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
    public string? Description { get; set; }

    public List<IFormFile>? Files { get; set; }
    public List<AssetFileDto> ExistingFiles { get; set; } = new List<AssetFileDto>();

    public Guid? DepartmentId { get; set; }

    public Guid? UserId { get; set; }
    public UserContactType? Type { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsValidFileType()
    {
        if (Files == null) return true;

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

        foreach (var file in Files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return false;
            }
        }
        return true;
    }
}

public class AssetFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }
}
