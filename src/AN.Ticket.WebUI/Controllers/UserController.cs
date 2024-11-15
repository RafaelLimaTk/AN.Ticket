using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Accounts;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly ISeedUserRoleInitial _seedUserRoleInitial;
    private readonly IEmailSenderService _emailSenderService;

    public UserController(
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        ISeedUserRoleInitial seedUserRoleInitial,
        IEmailSenderService emailSenderService
    )
    {
        _userService = userService;
        _userManager = userManager;
        _fileService = fileService;
        _seedUserRoleInitial = seedUserRoleInitial;
        _emailSenderService = emailSenderService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var paginatedUsers = await _userService.GetPaginatedUsersAsync(pageNumber, pageSize, searchTerm);

        return View(new UserListViewModel
        {
            Users = paginatedUsers.Items,
            PageNumber = paginatedUsers.PageNumber,
            PageSize = paginatedUsers.PageSize,
            TotalItems = paginatedUsers.TotalItems,
            SearchTerm = searchTerm
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        var password = $"AtlasNetwork@{DateTime.Now.Year}";
        var userDto = new UserDto
        {
            Password = password,
            ConfirmPassword = password,
            Role = UserRole.Support
        };

        return View(userDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserDto model, IFormFile? profilePictureForm, string password, string confirmPassword)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (password != confirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "As senhas não coincidem.");
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            TempData["ErrorMessage"] = "E-mail já cadastrado!";
            return View(model);
        }

        try
        {
            var newUser = new ApplicationUser
            (
                model.FullName,
                model.Email,
                model.Email,
                true,
                false
            );

            if (profilePictureForm is not null)
            {
                var filePath = await _fileService.SaveFileAsync(profilePictureForm);
                newUser.UpdateProfilePicture(filePath);
            }

            var result = await _userManager.CreateAsync(newUser, password);
            if (result.Succeeded)
            {
                await _seedUserRoleInitial.SeedRolesAsync();
                await _userManager.AddToRoleAsync(newUser, model.Role.ToString());

                model.Id = Guid.Parse(newUser.Id);

                var resultUser = await _userService.CreateUserAsync(model, profilePictureForm);
                if (!resultUser)
                {
                    TempData["ErrorMessage"] = "Erro ao criar usuário!";
                    await _userManager.DeleteAsync(newUser);
                    return View(model);
                }

                await SendWelcomeEmail(model.Email, password);

                TempData["SuccessMessage"] = "Usuário criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao criar usuário: " + ex.Message;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do usuário inválido!";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userService.GetByIdAsync(id);
        if (user is null)
        {
            TempData["ErrorMessage"] = "Usuário não encontrado!";
            return RedirectToAction(nameof(Index));
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ProfilePicture = user.ProfilePicture
        };

        return View(nameof(Create), userDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserDto model, IFormFile? profilePictureForm, string? password, string? confirmPassword)
    {
        if (!ModelState.IsValid)
            return View(nameof(Create), model);

        if (!string.IsNullOrEmpty(password) && password != confirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "As senhas não coincidem.");
            return View(nameof(Create), model);
        }

        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null)
        {
            TempData["ErrorMessage"] = "Usuário não encontrado!";
            return RedirectToAction(nameof(Create), model);
        }

        try
        {
            user.UpdateFullName(model.FullName);
            user.Email = model.Email;

            if (profilePictureForm != null)
            {
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                    await _fileService.DeleteFileAsync(user.ProfilePicture);

                var filePath = await _fileService.SaveFileAsync(profilePictureForm);
                user.UpdateProfilePicture(filePath);
            }

            if (!string.IsNullOrEmpty(password))
                await _userManager.RemovePasswordAsync(user);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(password))
                    await _userManager.AddPasswordAsync(user, password);

                await _userManager.RemoveFromRolesAsync(user, await _userManager.GetRolesAsync(user));
                await _userManager.AddToRoleAsync(user, model.Role.ToString());

                var resultUser = await _userService.UpdateUserAsync(model, profilePictureForm);
                if (!resultUser)
                {
                    TempData["ErrorMessage"] = "Erro ao criar usuário!";
                    return View(nameof(Create), model);
                }

                TempData["SuccessMessage"] = "Usuário atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao atualizar usuário: " + ex.Message;
        }

        return View(nameof(Create), model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do usuário inválido!";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _userService.DeleteUserAsync(id);
            if (result)
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user != null)
                {
                    var identityResult = await _userManager.DeleteAsync(user);
                    if (identityResult.Succeeded)
                        TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
                    else
                        TempData["ErrorMessage"] = "Erro ao excluir usuário do sistema de identidade!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Usuário não encontrado no sistema de identidade!";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Usuário não encontrado!";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao excluir usuário: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUsers(List<Guid> ids)
    {
        if (ids.Count == 0)
        {
            TempData["ErrorMessage"] = "Nenhum usuário selecionado!";
            return RedirectToAction(nameof(Index));
        }

        var errorMessages = new List<string>();

        try
        {
            var deleteTasks = ids.Select(async id =>
            {
                var result = await _userService.DeleteUserAsync(id);
                if (result)
                {
                    var user = await _userManager.FindByIdAsync(id.ToString());
                    if (user != null)
                    {
                        var identityResult = await _userManager.DeleteAsync(user);
                        if (!identityResult.Succeeded)
                            errorMessages.Add($"Erro ao excluir usuário {id} do sistema de identidade!");
                    }
                    else
                    {
                        errorMessages.Add($"Usuário {id} não encontrado no sistema de identidade!");
                    }
                }
                else
                {
                    errorMessages.Add($"Usuário {id} não encontrado!");
                }
            });

            await Task.WhenAll(deleteTasks);

            if (errorMessages.Any())
            {
                TempData["ErrorMessage"] = string.Join(" ", errorMessages);
            }
            else
            {
                TempData["SuccessMessage"] = "Usuários excluídos com sucesso!";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Erro ao excluir usuários: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task SendWelcomeEmail(string email, string password)
    {
        string emailContent = $@"
        <html>
            <body style='font-family: Arial, sans-serif; color: #333; padding: 20px;'>
                <div style='max-width: 600px; margin: auto; padding: 20px; border-radius: 10px; background-color: #ffffff;'>
            
                    <h2 style='color: #007bff; text-align: center; font-size: 24px; font-weight: bold; margin-top: 0;'>Bem-vindo ao AtlasNetworks!</h2>
                    <p style='text-align: center; font-size: 16px; color: #555;'>Estamos muito felizes em tê-lo conosco! Abaixo estão os detalhes da sua nova conta:</p>
            
                    <div style='padding: 20px; background-color: #f1f9ff; border-radius: 8px; margin: 20px 0;'>
                        <p style='font-size: 16px; margin: 0;'><strong>Email:</strong> {email}</p>
                        <p style='font-size: 16px; margin: 5px 0;'><strong>Senha Temporária:</strong> {password}</p>
                    </div>
            
                    <p style='font-size: 16px; color: #555; line-height: 1.6;'>
                        Para manter a segurança da sua conta, recomendamos que altere sua senha ao acessar o sistema pela primeira vez. 
                    </p>
            
                    <hr style='border: 0; border-top: 1px solid #ddd; margin: 30px 0;' />
            
                    <p style='text-align: center; font-size: 14px; color: #777;'>
                        Atenciosamente,<br>
                        <strong>Equipe de Suporte - AtlasNetworks</strong>
                    </p>
            
                    <p style='text-align: center; font-size: 12px; color: #aaa; margin-top: 30px;'>
                        Este é um e-mail automático, por favor, não responda diretamente a esta mensagem.
                    </p>
                </div>
            </body>
        </html>";

        await _emailSenderService.SendEmailAsync(email, "Bem-vindo ao Sistema", emailContent);
    }
}