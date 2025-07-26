using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SmartComply.Controllers
{
  public class BaseController : Controller
  {
    protected int? CurrentStaffId =>
        int.TryParse(User.FindFirst("StaffId")?.Value, out var id) ? id : null;

    protected string? CurrentStaffName =>
        User.Identity?.Name;

    protected string? CurrentStaffRole =>
        User.FindFirst(ClaimTypes.Role)?.Value;

    protected string? CurrentStaffEmail =>
        User.FindFirst("StaffEmail")?.Value;

    protected int? CurrentBranchId => HttpContext.Session.GetInt32("BranchId");
  }
}
