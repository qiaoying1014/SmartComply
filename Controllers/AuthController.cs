using Microsoft.AspNetCore.Mvc;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;


namespace SmartComply.Controllers
{
  
  public class AuthController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<Staff> _passwordHasher;

    public AuthController(ApplicationDbContext context, IPasswordHasher<Staff> passwordHasher)
    {
      _context = context;
      _passwordHasher = passwordHasher;
    }


    [HttpGet]
    public IActionResult Login()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      var staff = _context.Staffs
          .Include(s => s.StaffBranch)
          .FirstOrDefault(s => s.StaffEmail == model.StaffEmail && s.StaffIsActive);

      if (staff == null)
      {
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
      }

      var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(staff, staff.StaffPassword, model.StaffPassword);

      if (passwordVerificationResult == PasswordVerificationResult.Failed)
      {
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
      }

      var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, staff.StaffName),
        new Claim(ClaimTypes.Email, staff.StaffEmail),
        new Claim(ClaimTypes.Role, staff.StaffRole),
        new Claim("StaffId", staff.StaffId.ToString())
    };

      var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
      var principal = new ClaimsPrincipal(identity);

      await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
      HttpContext.Session.SetInt32("StaffId", staff.StaffId);

      // Log the login action in UTC time
      var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found
      _context.ActivityLogs.Add(new ActivityLog
      {
        StaffId = staff.StaffId,
        ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
        ActionType = ActivityLog.ActionTypeEnum.Login, // Use the enum for login
        ActionDescription = $"Staff {staffName} (ID: {staff.StaffId}) logged in."
      });

      await _context.SaveChangesAsync();

      return staff.StaffRole switch
      {
        "Admin" => RedirectToAction("Index", "Admin"),
        "Manager" => RedirectToAction("Index", "Manager"),
        "User" => RedirectToAction("Index", "User"),
        _ => RedirectToAction("Login", "Auth")
      };
    }

    public async Task<IActionResult> Logout()
    {
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        // Get the staff name (in case you want to log it as well)
        var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        // Log the logout action in UTC time
        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
          ActionType = ActivityLog.ActionTypeEnum.Logout, // Use the enum for logout
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) logged out."
        });
        await _context.SaveChangesAsync();
      }

      await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
      return RedirectToAction("Login", "Auth");
    }
  }
}
