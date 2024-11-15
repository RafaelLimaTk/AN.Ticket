using AN.Ticket.Application.Interfaces;
using AN.Ticket.WebUI.ViewModels.Setting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AN.Ticket.WebUI.Controllers;

[Authorize]
public class SettingController : Controller
{
    private readonly ILogger<SettingController> _logger;
    private readonly IPaymantPlanService _paymentPlanService;


    public SettingController(
        ILogger<SettingController> logger,
        IPaymantPlanService paymentPlanService
    )
    {
        _paymentPlanService = paymentPlanService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(SettingViewModel settingViewModel)
    {
        if (TempData["SettingViewModel"] != null)
            settingViewModel = JsonConvert.DeserializeObject<SettingViewModel>(TempData["SettingViewModel"].ToString());
        else
            settingViewModel = new SettingViewModel();

        return View(settingViewModel);
    }
}