﻿using AN.Ticket.Application.Interfaces;
using AN.Ticket.Infra.Data.Identity;
using AN.Ticket.WebUI.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSenderService _emailSenderService;

    public AccountController(
        ILogger<AccountController> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSenderService emailSenderService
    )
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSenderService = emailSenderService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);
                var emailMessage = $@"
                <html>
                <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;'>
                    <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width: 600px; background-color: #ffffff; margin: 40px auto; border-radius: 8px; box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);'>
                        <tr>
                            <td style='padding: 30px; text-align: center;'>
                                <h2 style='color: #333333;'>Confirmação de E-mail</h2>
                                <p style='color: #666666; font-size: 16px; line-height: 1.5;'>Olá,</p>
                                <p style='color: #666666; font-size: 16px; line-height: 1.5;'>Por favor, confirme seu e-mail clicando no botão abaixo:</p>
                                <a href='{callbackUrl}' style='display: inline-block; padding: 6px 12px; font-size: 12px; color: #ffffff; background-color: #007bff; text-decoration: none; border-radius: 5px; margin-top: 20px;'>Confirmar E-mail</a>
                                <p style='color: #666666; font-size: 14px; line-height: 1.5; margin-top: 30px;'>Se você não solicitou esta ação, por favor, ignore este e-mail.</p>
                                <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;' />
                                <p style='color: #999999; font-size: 12px; text-align: center;'>Este é um e-mail automático. Por favor, não responda.</p>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";

                await _emailSenderService.SendEmailAsync(user.Email, "Confirme seu e-mail", emailMessage);

                return RedirectToAction(nameof(VerifyEmail));
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyEmail()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (userId == null || code == null)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"Não foi possível carregar o usuário com ID '{userId}'.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        return View("Error");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser(model.FullName, model.Email, model.Email, false, false);
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, model.RememberMe);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
    }

    [HttpGet("enable-2fa")]
    public async Task<IActionResult> Enable2FA()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(authenticatorKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var model = new Enable2FAViewModel { AuthenticatorKey = authenticatorKey };
        return View(model);
    }

    [HttpPost("enable-2fa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable2FA(Enable2FAViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

        if (!is2faTokenValid)
        {
            ModelState.AddModelError("Code", "Código de verificação inválido.");
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        var backupCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        TempData["BackupCodes"] = backupCodes;

        return RedirectToAction(nameof(DisplayBackupCodes));
    }

    [HttpGet("display-backup-codes")]
    public IActionResult DisplayBackupCodes()
    {
        var backupCodes = TempData["BackupCodes"] as IEnumerable<string>;
        if (backupCodes == null)
        {
            return RedirectToAction(nameof(Enable2FA));
        }

        var model = new DisplayBackupCodesViewModel { BackupCodes = backupCodes };
        return View(model);
    }

    [HttpGet("verify-2fa")]
    [AllowAnonymous]
    public IActionResult Verify2FA()
    {
        return View();
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify2FA(Verify2FAViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, model.RememberMachine);

        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Código de autenticação inválido.");
            return View(model);
        }
    }
}