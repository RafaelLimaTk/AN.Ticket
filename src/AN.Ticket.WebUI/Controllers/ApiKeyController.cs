using AN.Ticket.WebUI.ViewModels.Setting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class ApiKeyController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly string _appSettingsPath;

    public ApiKeyController(IConfiguration configuration)
    {
        _configuration = configuration;
        _appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    }

    [HttpPost]
    public async Task<IActionResult> SaveApiKey(ApiKeyViewModel model)
    {
        if (ModelState.IsValid)
        {
            var json = await System.IO.File.ReadAllTextAsync(_appSettingsPath);
            var jsonObj = JObject.Parse(json);
            jsonObj["OpenAI"]["ApiKey"] = model.ApiKey;
            await System.IO.File.WriteAllTextAsync(_appSettingsPath, jsonObj.ToString());

            TempData["SuccessMessage"] = "API Key salva com sucesso!";
        }
        else
        {
            TempData["ErrorMessage"] = "Erro ao salvar a API Key.";
        }

        TempData["SuccessRedirect"] = true;
        return RedirectToAction("Index", "Setting");
    }
}
