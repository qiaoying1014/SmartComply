using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;
using SmartComply.Helper;

namespace SmartComply.Controllers
{
  public class ViewAuditController : Controller
  {
    private readonly ApplicationDbContext _context;

    public ViewAuditController(ApplicationDbContext context)
    {
      _context = context;
    }
    ////AuditList
    // GET: Audit List
    public async Task<IActionResult> Index(string statusFilter = null, string searchTerm = null)
    {
      // Ensure user is logged in
      int? staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId == null)
        return RedirectToAction("Login", "Auth");

      // Fetch staff information with branch
      var staff = await _context.Staffs
          .Include(s => s.StaffBranch)
          .FirstOrDefaultAsync(s => s.StaffId == staffId);

      if (staff == null || staff.StaffBranch == null)
        return Unauthorized();

      // Start building the query for fetching audits
      var query = _context.Audits
          .Include(a => a.Staff)
          .Include(a => a.Category)
          .Include(a => a.FormResponders)
          .Where(a => a.Staff.StaffBranchId == staff.StaffBranchId)
          .OrderByDescending(a => a.CreatedAt)
          .AsQueryable();

      // Apply status filter if provided
      if (!string.IsNullOrEmpty(statusFilter))
      {
        query = query.Where(a => a.Status.ToLower() == statusFilter.ToLower());
      }

      // Apply search filter by Audit Name or Auditor Name if provided
      if (!string.IsNullOrEmpty(searchTerm))
      {
        string lowerSearchTerm = searchTerm.ToLower();
        query = query.Where(a => a.AuditName.ToLower().Contains(lowerSearchTerm) || a.Staff.StaffName.ToLower().Contains(lowerSearchTerm));
      }

      // Execute the query to get the list of audits
      var audits = await query.ToListAsync();

      var auditIds = audits.Select(a => a.AuditId).ToList();

      // Get corrective action count for each audit
      var correctiveActionGroups = await _context.CorrectiveActions
          .Where(ca => auditIds.Contains(ca.AuditId))
          .GroupBy(ca => ca.AuditId)
          .Select(g => new { AuditId = g.Key, Count = g.Count() })
          .ToListAsync();

      // Create the view model list with audit, form count, and corrective action count
      var viewModelList = audits.Select(a => new AuditSummaryViewModel
      {
        Audit = a,
        FormCount = a.FormResponders?.Count ?? 0,
        CorrectiveActionCount = correctiveActionGroups
              .FirstOrDefault(g => g.AuditId == a.AuditId)?.Count ?? 0
      }).ToList();

      // Pass search and filter values back to the view
      ViewBag.StatusFilter = statusFilter;
      ViewBag.SearchTerm = searchTerm;

      // Check if no audits were found
      bool noResultsFound = !viewModelList.Any();
      ViewBag.NoResultsFound = noResultsFound;

      // Return the view with the filtered and searched audits
      return View(viewModelList);
    }


    ////AuditDetails
    [Route("Manager/AuditDetails")]
    public async Task<IActionResult> AuditDetails(int auditId)
    {
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId == null) return RedirectToAction("Login","Auth");

      var manager = await _context.Staffs.FirstOrDefaultAsync(s => s.StaffId == staffId);
      if (manager == null) return Unauthorized();

      var audit = await _context.Audits
          .Include(a => a.Staff)
          .Include(a => a.Category)
          .Include(a => a.FormResponders)
              .ThenInclude(fr => fr.Form)
          .Include(a => a.FormResponders)
              .ThenInclude(fr => fr.Staff)
          .Include(a => a.FormResponders)
              .ThenInclude(fr => fr.Responses)
                  .ThenInclude(r => r.FormElement)
          .FirstOrDefaultAsync(a => a.AuditId == auditId);

      if (audit == null || audit.Staff.StaffBranchId != manager.StaffBranchId)
      {
        return Forbid();
      }

      // Get related corrective actions by AuditId
      var correctiveActions = await _context.CorrectiveActions
          .Where(ca => ca.AuditId == auditId && !ca.IsDeleted)
          .ToListAsync();

      ViewBag.CorrectiveActions = correctiveActions;

      return View(audit);
    }


    ////ViewForm
    public IActionResult ViewForm(int id)
    {
      var responder = _context.FormResponders
          .Include(fr => fr.Form)
              .ThenInclude(f => f.Category)
          .Include(fr => fr.Form)
              .ThenInclude(f => f.FormElements)
          .Include(fr => fr.Staff)
          .Include(fr => fr.Branch)
          .Include(fr => fr.Responses)
          .FirstOrDefault(fr => fr.FormResponderId == id);

      if (responder == null)
        return NotFound();

      return View(responder);
    }

    ///ViewCorrectiveAction
    public async Task<IActionResult> ViewCorrectiveAction(int id)
    {
      var correctiveAction = await _context.CorrectiveActions
          .Include(ca => ca.Audit)
              .ThenInclude(a => a.Staff)
          .Include(ca => ca.Audit)
              .ThenInclude(a => a.Category)
          .FirstOrDefaultAsync(ca => ca.CorrectiveActionId == id);

      if (correctiveAction == null)
        return NotFound();

      return View(correctiveAction);
    }

    // PUBLIC: audit‐only page for external auditors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalAudit(string token)
    {
      if (string.IsNullOrWhiteSpace(token))
        return BadRequest();

      var audit = await _context.Audits
          .Include(a => a.Category)
          .Include(a => a.Staff)
          .Include(a => a.FormResponders)
              .ThenInclude(fr => fr.Form)
          .FirstOrDefaultAsync(a => a.ShareToken == token);

      if (audit == null)
        return NotFound();

      // <<— add this line:
      ViewData["Token"] = token;

      ViewBag.CorrectiveActions = await _context.CorrectiveActions
          .Where(ca => ca.AuditId == audit.AuditId && !ca.IsDeleted)
          .ToListAsync();

      return View("~/Views/ExternalAuditor/ViewAudit.cshtml", audit);
    }



    // PUBLIC: form‐only page for external auditors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalViewForm(int id, string token)
    {
      if (string.IsNullOrWhiteSpace(token))
        return BadRequest();

      // find the audit by token
      var audit = await _context.Audits
          .FirstOrDefaultAsync(a => a.ShareToken == token);
      if (audit == null)
        return NotFound();

      // only fetch this form if it belongs to that audit
      var responder = await _context.FormResponders
          .Include(fr => fr.Form).ThenInclude(f => f.Category)
          .Include(fr => fr.Form).ThenInclude(f => f.FormElements)
          .Include(fr => fr.Staff)
          .Include(fr => fr.Branch)
          .Include(fr => fr.Responses)
          .FirstOrDefaultAsync(fr => fr.FormResponderId == id
                                     && fr.AuditId == audit.AuditId);
      if (responder == null)
        return NotFound();

      ViewData["Token"] = token;
      return View("~/Views/ExternalAuditor/ViewForm.cshtml", responder);
    }

    // PUBLIC: form‐only page for external auditors
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ExternalViewCorrectiveAction(int id, string token)
    {
      if (string.IsNullOrWhiteSpace(token))
        return BadRequest();

      var audit = await _context.Audits
          .FirstOrDefaultAsync(a => a.ShareToken == token);
      if (audit == null)
        return NotFound();

      var ca = await _context.CorrectiveActions
          .Include(c => c.Audit).ThenInclude(a => a.Staff)
          .Include(c => c.Audit).ThenInclude(a => a.Category)
          .FirstOrDefaultAsync(c => c.CorrectiveActionId == id
                                     && c.AuditId == audit.AuditId);
      if (ca == null)
        return NotFound();

      ViewData["Token"] = token;
      return View("~/Views/ExternalAuditor/ViewCorrectiveAction.cshtml", ca);
    }

    // (Also update your Qr action to point at ExternalAudit:)
    [AllowAnonymous]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Qr(int auditId)
    {
      try
      {
        var audit = await _context.Audits.FindAsync(auditId);
        if (audit == null)
          return NotFound();

        audit.ShareToken = Guid.NewGuid().ToString("N");
        await _context.SaveChangesAsync();

        var url = Url.Action(
            nameof(ExternalAudit),
            "ViewAudit",
            new { token = audit.ShareToken },
            Request.Scheme);

        var qrBytes = QrHelper.GeneratePngQr(url);

        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
          var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value); // Retrieve the staff who performed the action
          var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback if staff name is not found

          _context.ActivityLogs.Add(new ActivityLog
          {
            StaffId = staffId.Value,
            ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
            ActionType = ActivityLog.ActionTypeEnum.Update, // Enum for update action
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) generated a QR code for the audit named {audit.AuditName}"
          });

          await _context.SaveChangesAsync(); // Save the log after generating the QR code
        }

        return File(qrBytes, "image/png");
      }
      catch (Exception)
      {
        return StatusCode(503, "Unable to generate QR code at this time.");
      }
    }
  }
}
