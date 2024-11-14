using AN.Ticket.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AN.Ticket.Application.DTOs.User;
public class UserDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    [Display(Name = "Nome Completo")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; }

    [Required(ErrorMessage = "A função é obrigatória.")]
    [Display(Name = "Função")]
    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }

    [Display(Name = "Senha")]
    public string? Password { get; set; }

    [Display(Name = "Confirmar Senha")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Senha Atual")]
    public string? CurrentPassword { get; set; }

    public string? ProfilePicture { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id == Guid.Empty)
        {
            if (string.IsNullOrEmpty(Password))
            {
                yield return new ValidationResult("A senha é obrigatória para criação de um novo usuário.", new[] { nameof(Password) });
            }

            if (Password != ConfirmPassword)
            {
                yield return new ValidationResult("A senha e a confirmação devem coincidir.", new[] { nameof(ConfirmPassword) });
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(Password) || !string.IsNullOrEmpty(ConfirmPassword))
            {
                if (Password != ConfirmPassword)
                {
                    yield return new ValidationResult("A nova senha e a confirmação devem coincidir.", new[] { nameof(ConfirmPassword) });
                }
                if (string.IsNullOrEmpty(CurrentPassword))
                {
                    yield return new ValidationResult("A senha atual é obrigatória para atualizar a senha.", new[] { nameof(CurrentPassword) });
                }
            }
        }
    }
}
