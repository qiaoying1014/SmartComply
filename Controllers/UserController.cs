using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Helper;

namespace SmartComply.Controllers
{
  public class UserController : Controller
  {
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
      _context = context;
    }
    public IActionResult Index()
    {
      return View();
    }

    // PUBLIC: audit‐only page for external auditors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalAudit(int auditId)
    {
      var audit = await _context.Audits
          .Include(a => a.Category)
          .Include(a => a.Staff)
          .Include(a => a.FormResponders)
              .ThenInclude(fr => fr.Form)
      .FirstOrDefaultAsync(a => a.AuditId == auditId);

      if (audit == null)
        return NotFound();

      var correctiveActions = await _context.CorrectiveActions
          .Where(ca => ca.AuditId == auditId && !ca.IsDeleted)
          .ToListAsync();

      ViewBag.CorrectiveActions = correctiveActions;

      // point explicitly at the new folder/view
      return View("~/Views/ExternalAuditor/ViewAudit.cshtml", audit);
    }

    // PUBLIC: form‐only page for external auditors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalViewForm(int id)
    {
      var responder = await _context.FormResponders
          .Include(fr => fr.Form)
              .ThenInclude(f => f.Category)
          .Include(fr => fr.Form)
              .ThenInclude(f => f.FormElements)
          .Include(fr => fr.Staff)
          .Include(fr => fr.Branch)
          .Include(fr => fr.Responses)
          .FirstOrDefaultAsync(fr => fr.FormResponderId == id);

      if (responder == null)
        return NotFound();

      return View("~/Views/ExternalAuditor/ViewForm.cshtml", responder);
    }

    // PUBLIC: corrective‐action page for external auditors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalViewCorrectiveAction(int id)
    {
      var ca = await _context.CorrectiveActions
          .Include(c => c.Audit)
              .ThenInclude(a => a.Staff)
          .Include(c => c.Audit)
              .ThenInclude(a => a.Category)
          .FirstOrDefaultAsync(c => c.CorrectiveActionId == id);

      if (ca == null)
        return NotFound();

      // explicitly point at our ExternalAuditor view
      return View("~/Views/ExternalAuditor/ViewCorrectiveAction.cshtml", ca);
    }

    // (Also update your Qr action to point at ExternalAudit:)
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Qr(int auditId)
    {
      var url = Url.Action(
          action: nameof(ExternalAudit),
          controller: "Manager",
          values: new { auditId },
          protocol: Request.Scheme);

      var qrBytes = QrHelper.GeneratePngQr(url);
      return File(qrBytes, "image/png");
    }
  }
}
