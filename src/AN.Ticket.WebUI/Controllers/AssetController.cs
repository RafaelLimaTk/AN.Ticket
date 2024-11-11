using AN.Ticket.Application.DTOs.Asset;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.Components;
using AN.Ticket.WebUI.ViewModels.Asset;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;
public class AssetController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IUserService _userService;
    private readonly IAssetAssignmentService _assetAssignmentService;
    private readonly IContactService _contactService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssetController(
        IAssetService assetService,
        IUserService userService,
        IAssetAssignmentService assetAssignmentService,
        IContactService contactService,
        UserManager<ApplicationUser> userManager
    )
    {
        _assetService = assetService;
        _userService = userService;
        _assetAssignmentService = assetAssignmentService;
        _contactService = contactService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var paginatedAssets = await _assetService.GetPaginatedAssetsAsync(pageNumber, pageSize, searchTerm);

        return View(new AssetListViewModel
        {
            Assets = paginatedAssets.Items,
            PageNumber = paginatedAssets.PageNumber,
            PageSize = paginatedAssets.PageSize,
            TotalItems = paginatedAssets.TotalItems,
            SearchTerm = searchTerm
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AssetDto
        {
            PurchaseDate = DateTime.Now,
            UserId = Guid.Empty
        };

        ViewBag.UserContacts = await GetUserContactsAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetDto model)
    {
        if (!ModelState.IsValid || !model.IsValidFileType())
        {
            ModelState.AddModelError("Files", "Tipo de arquivo não suportado. Somente pdf, jpg, jpeg, e png são permitidos.");
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(model);
        }

        try
        {
            await _assetService.CreateAssetAsync(model);
            TempData["SuccessMessage"] = "Ativo criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao criar o ativo: {ex.Message}";
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "ID do ativo inválido.";
            return View(nameof(Index));
        }

        var asset = await _assetService.GetByIdAsync(id);
        if (asset is null)
        {
            TempData["ErrorMessage"] = "Ativo não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var userId = await _assetAssignmentService.GetAssignmentUserIdAsync(asset.Id);
        var assetFiles = await _assetService.GetAssetFilesAsync(id);

        var assetDto = new AssetDto
        {
            Id = asset.Id,
            Name = asset.Name,
            SerialNumber = asset.SerialNumber,
            AssetType = asset.AssetType,
            PurchaseDate = asset.PurchaseDate,
            Value = asset.Value,
            Description = asset.Description,
            UserId = userId,
            ExistingFiles = assetFiles.Select(f => new AssetFileDto
            {
                Id = f.Id,
                FileName = f.FileName,
                FileContent = f.FileContent
            }).ToList()
        };

        ViewBag.UserContacts = await GetUserContactsAsync();

        return View(nameof(Create), assetDto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssetDto model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(nameof(Create), model);
        }

        try
        {
            await _assetService.UpdateAssetAsync(model);
            TempData["SuccessMessage"] = "Ativo atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao atualizar o ativo: {ex.Message}";
            ViewBag.UserContacts = await GetUserContactsAsync();
            return View(nameof(Create), model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _assetService.DeleteAssetAsync(id);
            TempData["SuccessMessage"] = "Ativo excluído com sucesso.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao excluir o ativo: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAssets(List<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            TempData["ErrorMessage"] = "Nenhum ativo foi selecionado.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _assetService.DeleteAssetsAsync(ids);
            TempData["SuccessMessage"] = "Ativos deletados com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao deletar os ativos";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDetails(Guid userId, UserContactType type)
    {
        var assetUserDetailsViewComponent = new AssetUserDetailsViewComponent(_userService, _contactService, _userManager);
        var result = await assetUserDetailsViewComponent.InvokeAsync(userId, type);
        return ViewComponent("AssetUserDetails", new { userId, type });
    }

    private async Task<List<UserContactDto>> GetUserContactsAsync()
    {
        var users = await _userService.GetAllAsync();
        var contacts = await _contactService.GetAllAsync();

        var userContactList = new List<UserContactDto>();

        userContactList.AddRange(users.Select(user => new UserContactDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Type = UserContactType.User
        }));

        userContactList.AddRange(contacts.Select(contact => new UserContactDto
        {
            Id = contact.Id,
            FullName = contact.GetFullName(),
            Type = UserContactType.Contact
        }));

        return userContactList;
    }
}
