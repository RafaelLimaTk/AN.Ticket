using AN.Ticket.Application.DTOs.PaymantPlan;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.WebUI.ViewModels.PaymantPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AN.Ticket.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public class PaymantPlanController : Controller
{
    private readonly IPaymantPlanService _paymentPlanService;

    public PaymantPlanController(
        IPaymantPlanService paymentPlanService
    )
        => _paymentPlanService = paymentPlanService;

    [HttpGet]
    public async Task<IActionResult> GetPagedPlans(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
    {
        var paginatedPlans = await _paymentPlanService.GetPaginatedPlansAsync(pageNumber, pageSize, searchTerm);

        var viewModel = new PaymantPlanListViewModel
        {
            PaymentPlans = paginatedPlans.Items,
            PageNumber = paginatedPlans.PageNumber,
            PageSize = paginatedPlans.PageSize,
            TotalItems = paginatedPlans.TotalItems,
            SearchTerm = searchTerm
        };

        return PartialView("~/Views/Shared/Partials/PaymantPlan/_PaymantPlansTable.cshtml", viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty)
        {
            TempData["ErrorMessage"] = "O id do plano de pagamento não foi informado!";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }

        try
        {
            var result = await _paymentPlanService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Plano de pagamento excluído com sucesso!";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }
        catch
        {
            TempData["ErrorMessage"] = "Erro ao excluir o plano de pagamento!";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeletePlans(Guid[] ids)
    {
        if (ids.Length == 0)
        {
            TempData["ErrorMessage"] = "Nenhum plano de pagamento foi selecionado!";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }

        try
        {
            foreach (var id in ids)
                await _paymentPlanService.DeleteAsync(id);

            TempData["SuccessMessage"] = "Planos de pagamento excluídos com sucesso!";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }
        catch
        {
            TempData["ErrorMessage"] = "Erro ao excluir os planos de pagamento!";
            TempData["SuccessRedirect"] = true;
            return RedirectToAction("Index", "Setting");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(PaymantPlanDto model)
    {
        if (!ModelState.IsValid)
        {
            TempData["SuccessRedirect"] = true;
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        await _paymentPlanService.CreateAsync(model);
        TempData["SuccessMessage"] = "Plano de pagamento criado com sucesso!";
        TempData["SuccessRedirect"] = true;
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PaymantPlanDto model)
    {
        if (!ModelState.IsValid)
        {
            TempData["SuccessRedirect"] = true;
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        await _paymentPlanService.UpdateAsync(model);
        TempData["SuccessMessage"] = "Plano de pagamento atualizado com sucesso!";
        TempData["SuccessRedirect"] = true;
        return Json(new { success = true });
    }
}
