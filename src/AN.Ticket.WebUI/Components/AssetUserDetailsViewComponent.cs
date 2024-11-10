using AN.Ticket.Application.Interfaces;
using AN.Ticket.Domain.Enums;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.ViewModels.Asset;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Components;

[ViewComponent]
public class AssetUserDetailsViewComponent : ViewComponent
{
    private readonly IUserService _userService;
    private readonly IContactService _contactService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssetUserDetailsViewComponent(
        IUserService userService,
        IContactService contactService,
        UserManager<ApplicationUser> userManager
    )
    {
        _userService = userService;
        _contactService = contactService;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(Guid userId, UserContactType type)
    {
        if (userId == Guid.Empty)
        {
            return View(new AssetUserDetailViewModel());
        }

        if (type == UserContactType.User)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user is null)
            {
                return View(new AssetUserDetailViewModel());
            }

            var userMangement = await _userManager.FindByIdAsync(userId.ToString());
            if (userMangement is null)
            {
                return View(new AssetUserDetailViewModel());
            }

            var role = await _userManager.GetRolesAsync(userMangement);

            var viewModel = new AssetUserDetailViewModel
            {
                UserId = user.Id,
                ProfilePicture = user.ProfilePicture,
                FullName = user.FullName,
                Email = user.Email,
                Role = role.FirstOrDefault()
            };

            return View(viewModel);
        }
        else if (type == UserContactType.Contact)
        {
            var contact = await _contactService.GetContactDetailsAsync(userId);
            if (contact is null)
            {
                return View(new AssetUserDetailViewModel());
            }

            var viewModel = new AssetUserDetailViewModel
            {
                UserId = contact.ContactId,
                ProfilePicture = contact.ProfileImageUrl != "~/img/user-default.webp" ? contact.ProfileImageUrl : "",
                FullName = contact.FullName,
                Email = contact.Email,
                Role = "Contato"
            };

            return View(viewModel);
        }

        return View(new AssetUserDetailViewModel());
    }
}
