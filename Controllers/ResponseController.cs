using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;

namespace SmartComply.Controllers
{
  [Authorize(Roles = "User")]
  public class ResponseController : BaseController
  {
    private readonly ApplicationDbContext _context;

    public ResponseController(ApplicationDbContext context)
    {
      _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Fill(int id, int auditId)
    {
      // Step 1: Load the form
      var form = await _context.Forms
          .Include(f => f.FormElements)
          .Include(f => f.Category)
          .FirstOrDefaultAsync(f => f.FormId == id && f.Status == "Published");

      if (form == null)
        return NotFound();

      // Step 2: Get StaffId from current user
      var staffId = CurrentStaffId;
      if (staffId == null)
        return Unauthorized();

      // Step 3: Load staff and branch
      var staff = await _context.Staffs
          .Include(s => s.StaffBranch)
          .FirstOrDefaultAsync(s => s.StaffId == staffId);

      if (staff == null)
        return Unauthorized();

      // Step 4: Look up existing FormResponder with answers
      // Check if we have saved form data in TempData (i.e., coming back from Preview)
      FormFillViewModel viewModel;
      if (TempData["FormFillModel"] != null)
      {
        viewModel = JsonConvert.DeserializeObject<FormFillViewModel>(TempData["FormFillModel"].ToString());
        TempData.Remove("FormFillModel");


        viewModel.StaffEmail = staff?.StaffEmail;
        viewModel.BranchAddress = staff?.StaffBranch?.BranchAddress;
        viewModel.BranchIsActive = staff?.StaffBranch?.BranchIsActive ?? false;
        viewModel.BranchId = staff?.StaffBranch?.BranchId;
        viewModel.Category = form.Category.CategoryName;
      }

      else
      {
        // Otherwise, load the data from the database if no TempData exists (first time or not editing)
        var responder = await _context.FormResponders
            .Include(r => r.Responses)
            .FirstOrDefaultAsync(r => r.FormId == id && r.AuditId == auditId && r.StaffId == staffId);

        viewModel = new FormFillViewModel
        {
          FormId = form.FormId,
          AuditId = auditId,
          Category = form.Category.CategoryName,
          StaffEmail = staff.StaffEmail,
          BranchAddress = staff.StaffBranch?.BranchAddress,
          BranchIsActive = staff.StaffBranch?.BranchIsActive ?? false,
          FormResponderId = responder?.FormResponderId ?? 0,
          Elements = form.FormElements
                .OrderBy(e => e.Order)
                .Select(e =>
                {
                  var savedAnswers = responder?.Responses
                      .Where(r => r.FormElementId == e.FormElementId)
                      .OrderBy(r => r.FormResponseId)
                      .Select(r => r.Answer)
                      .ToList();

                  return new FormElementResponseViewModel
                  {
                    FormElementId = e.FormElementId,
                    Label = e.Label,
                    ElementType = e.ElementType,
                    IsRequired = e.IsRequired,
                    Options = e.Options,
                    Placeholder = e.Placeholder,
                    Answer = savedAnswers ?? new List<string>()
                  };
                }).ToList()
        };
      }

      return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(FormFillViewModel model)
    {
      // Step 1: Custom server-side validation for required fields
      foreach (var element in model.Elements)
      {
        if (!element.IsRequired)
          continue;

        bool isEmpty = element.ElementType switch
        {
          "Checkbox" => element.Answer == null || !element.Answer.Any(),
          "RadioButton" or "Dropdown" or "Email" or "Number" or "Date" or "Time" or "TextInput" or "TextArea" =>
              string.IsNullOrWhiteSpace(element.Answer?.FirstOrDefault()),
          "FileUpload" => (element.Files == null || !element.Files.Any()) &&
                          (element.Answer == null || string.IsNullOrWhiteSpace(element.Answer.FirstOrDefault())),
          _ => false
        };

        if (isEmpty)
        {
          ModelState.AddModelError($"Elements[{element.FormElementId}].Answer", $"{element.Label} is required.");
        }
      }

      // Step 2: If validation fails, reload form data and redirect to Fill
      if (!ModelState.IsValid)
      {
        var form = await _context.Forms
            .Include(f => f.FormElements)
            .FirstOrDefaultAsync(f => f.FormId == model.FormId);

        if (form != null)
        {
          model.Elements = form.FormElements
              .OrderBy(e => e.Order)
              .Select(e => new FormElementResponseViewModel
              {
                FormElementId = e.FormElementId,
                Label = e.Label,
                ElementType = e.ElementType,
                IsRequired = e.IsRequired,
                Options = e.Options,
                Placeholder = e.Placeholder,
                Answer = model.Elements?.FirstOrDefault(x => x.FormElementId == e.FormElementId)?.Answer ?? new List<string>(),
                Files = model.Elements?.FirstOrDefault(x => x.FormElementId == e.FormElementId)?.Files ?? new List<IFormFile>()
              }).ToList();
        }

        return View("Fill", model); // Razor view: Views/Response/Fill.cshtml

      }

      // Step 3: Handle file uploads
      foreach (var element in model.Elements)
      {
        if (element.ElementType == "FileUpload" && element.Files?.Any() == true)
        {
          var file = element.Files.First();

          var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "formfile");
          if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

          var fileName = Path.GetFileName(file.FileName);
          var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
          var filePath = Path.Combine(uploadsDir, uniqueFileName);

          using var stream = new FileStream(filePath, FileMode.Create);
          await file.CopyToAsync(stream);

          element.Answer = new List<string> { $"/uploads/formfile/{uniqueFileName}" };
        }
      }

      // Step 4: Store filled model in TempData for confirmation
      TempData["FormFillModel"] = JsonConvert.SerializeObject(model);

      // Step 5: Return preview view
      return View("Preview", model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitConfirmed()
    {
      if (TempData["FormFillModel"] == null)
        return RedirectToAction("AvailableForms");

      var model = JsonConvert.DeserializeObject<FormFillViewModel>(TempData["FormFillModel"].ToString() ?? "");

      var staff = await _context.Staffs
        .Include(s => s.StaffBranch)
        .FirstOrDefaultAsync(s => s.StaffId == CurrentStaffId);

      var branchId = staff.StaffBranchId;
      var staffName = staff?.StaffName ?? "Unknown"; // Retrieve the staff name (or "Unknown" if not found)
                                                     // Get the FormName directly from the Form model
      var form = await _context.Forms.FindAsync(model.FormId);
      var formName = form?.FormName ?? "Unknown Form"; // Retrieve form name

      var auditId = model.AuditId;

      var responder = new FormResponder
      {
        FormId = model.FormId,
        Name = staffName,
        SubmittedAt = DateTime.UtcNow,
        StaffId = CurrentStaffId,
        BranchId = branchId,
        AuditId = model.AuditId
      };

      _context.FormResponders.Add(responder);
      await _context.SaveChangesAsync();

      int fileCount = Convert.ToInt32(TempData["FileCount"] ?? 0);
      int fileUploadIndex = 0;

      if (model.Elements != null)
      {
        for (int i = 0; i < model.Elements.Count; i++)
        {
          var element = model.Elements[i];

          if (element.ElementType == "SectionHeader" || element.ElementType == "Description")
            continue;

          string? answerText = null;

          if (element.ElementType == "FileUpload")
          {
            answerText = element.Answer?.FirstOrDefault(); // Use saved path from Preview
          }

          else
          {
            if (element.Answer != null && element.Answer.Count > 0)
            {
              answerText = element.ElementType == "Checkbox"
                  ? string.Join(",", element.Answer)
                  : element.Answer.FirstOrDefault();
            }
          }

          var response = new FormResponse
          {
            FormResponderId = responder.FormResponderId,
            FormElementId = element.FormElementId,
            Answer = answerText
          };

          _context.FormResponses.Add(response);
        }

        await _context.SaveChangesAsync();
      }


      // Log the Add action with more detailed description
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
          ActionType = ActivityLog.ActionTypeEnum.Add, // Use the enum for add action
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) submitted a response for form '{formName}'."
        });

        await _context.SaveChangesAsync(); // Save the activity log
      }

      TempData["SuccessMessage"] = $"Audit for '{formName}' was added successfully!";

      // Redirect to MyAuditSummary action in AuditController
      return RedirectToAction("MyAuditSummary", "Audit");
    }

    //public IActionResult ThankYou()
    //    {
    //        return View();
    //    }

    //    public IActionResult AvailableForms()
    //    {
    //        var publishedForms = _context.Forms
    //            .Where(f => f.Status == "Published")
    //            .Include(f => f.Category)
    //            .OrderByDescending(f => f.CreatedDate)
    //            .ToList();

    //        return View(publishedForms);
    //    }

    ///Update Response

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int auditId)
    {
      var form = await _context.Forms
          .Include(f => f.FormElements)
          .FirstOrDefaultAsync(f => f.FormId == id);

      // Get audit with staff and branch
      var audit = await _context.Audits
          .Include(a => a.Staff)
              .ThenInclude(s => s.StaffBranch)
          .FirstOrDefaultAsync(a => a.AuditId == auditId);

      if (audit == null)
      {
        return NotFound("Audit not found.");
      }

      string staffEmail = audit.Staff?.StaffEmail ?? "Unknown";
      string branchAddress = audit.Staff?.StaffBranch?.BranchAddress ?? "Unknown";
      bool branchIsActive = audit.Staff?.StaffBranch?.BranchIsActive ?? false;


      if (form == null)
      {
        return NotFound();
      }

      var responder = await _context.FormResponders
          .FirstOrDefaultAsync(r => r.FormId == id && r.AuditId == auditId);

      bool isEdit = responder != null;

      int responderId = isEdit ? responder.FormResponderId : 0;

      var elementsWithAnswers = form.FormElements
          .OrderBy(e => e.Order)
          .Select(e =>
          {
            var responses = isEdit
              ? _context.FormResponses
                  .Where(r => r.FormResponderId == responder.FormResponderId && r.FormElementId == e.FormElementId)
                  .ToList()
              : new List<FormResponse>();

            return new FormElementResponseViewModel
            {
              FormElementId = e.FormElementId,
              ElementType = e.ElementType,
              Label = e.Label,
              Placeholder = e.Placeholder,
              IsRequired = e.IsRequired,
              Options = e.Options,
              Order = e.Order,
              Answer = responses.Select(r => r.Answer).ToList()
            };
          }).ToList();

      // Retrieve category name using CategoryId if needed
      string categoryName = await _context.ComplianceCategories
          .Where(c => c.CategoryId == form.CategoryId)
          .Select(c => c.CategoryName)
          .FirstOrDefaultAsync() ?? "Unknown";

      var model = new FormFillViewModel
      {
        FormId = id,
        AuditId = auditId,
        FormResponderId = responderId,
        IsEdit = isEdit,
        Category = categoryName,
        Elements = elementsWithAnswers,
        StaffEmail = audit?.Staff?.StaffEmail,
        BranchAddress = audit?.Staff?.StaffBranch?.BranchAddress,
        BranchIsActive = audit?.Staff?.StaffBranch?.BranchIsActive ?? false,
        BranchId = audit?.Staff?.StaffBranch?.BranchId
      };

      return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(FormFillViewModel model, IFormFile[] FormFiles)
    {
      // Validate model and custom required fields
      if (!ModelState.IsValid)
      {
        foreach (var element in model.Elements.Where(e => e.IsRequired))
        {
          bool isEmpty = element.ElementType switch
          {
            "Checkbox" => element.Answer == null || !element.Answer.Any(),
            "RadioButton" or "Dropdown" or "Email" or "Number" or "Date" or "Time" or "TextInput" or "TextArea" =>
                string.IsNullOrWhiteSpace(element.Answer?.FirstOrDefault()),
            "FileUpload" => (element.Files == null || !element.Files.Any()) &&
                            (element.Answer == null || string.IsNullOrWhiteSpace(element.Answer.FirstOrDefault())),
            _ => false
          };

          if (isEmpty)
          {
            ModelState.AddModelError($"Elements[{model.Elements.IndexOf(element)}].Answer", $"{element.Label} is required.");
          }
        }

        if (!ModelState.IsValid)
        {
          return View(model.IsEdit ? "Edit" : "Fill", model);
        }
      }

      var staffId = CurrentStaffId;
      if (staffId == null)
      {
        return Unauthorized();
      }

      IActionResult result = null;
      var strategy = _context.Database.CreateExecutionStrategy();

      await strategy.ExecuteAsync(async () =>
      {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
          try
          {
            FormResponder responder;

            if (model.IsEdit)
            {
              responder = await _context.FormResponders
                  .Include(r => r.Responses)
                  .FirstOrDefaultAsync(r => r.FormResponderId == model.FormResponderId);

              if (responder == null)
                throw new Exception("Responder not found.");

              _context.FormResponses.RemoveRange(responder.Responses);

              // Log the update action for updating responses
              var staffId = HttpContext.Session.GetInt32("StaffId");
              if (staffId.HasValue)
              {
                var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
                var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

                _context.ActivityLogs.Add(new ActivityLog
                {
                  StaffId = staffId.Value,
                  ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
                  ActionType = ActivityLog.ActionTypeEnum.Update, // Use the enum for update action
                  ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated responses for form '{model.Category}'."
                });

                await _context.SaveChangesAsync(); // Save the activity log
              }
            }
            else
            {
              var staff = await _context.Staffs
                  .Include(s => s.StaffBranch)
                  .FirstOrDefaultAsync(s => s.StaffId == staffId.Value);

              if (staff == null)
                throw new Exception("Staff not found.");

              var branchId = model.BranchId ?? staff.StaffBranch?.BranchId ?? 0;

              responder = new FormResponder
              {
                FormId = model.FormId,
                AuditId = model.AuditId,
                StaffId = staffId.Value,
                BranchId = branchId,
                Name = staff.StaffName,
                Responses = new List<FormResponse>()
              };

              _context.FormResponders.Add(responder);
            }

            // Handle responses and file uploads
            foreach (var element in model.Elements)
            {
              if (element.Files != null && element.Files.Any())
              {
                var file = element.Files.FirstOrDefault();
                if (file != null)
                {
                  var filePath = await SaveFileAsync(file);
                  element.Answer = new List<string> { filePath };
                }
              }

              if (element.Answer != null && element.Answer.Any())
              {
                foreach (var ans in element.Answer)
                {
                  responder.Responses.Add(new FormResponse
                  {
                    FormElementId = element.FormElementId,
                    Answer = ans
                  });
                }
              }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var form = await _context.Forms.FindAsync(model.FormId);
            var formName = form?.FormName ?? "Unknown Form"; // Retrieve form name


            TempData["SuccessMessage"] = model.IsEdit
                ? $"Audit for '{formName}' was updated successfully!"
                : $"Audit for '{formName}' was submitted successfully!";

            result = RedirectToAction("MyAuditSummary", "Audit");
          }
          catch
          {
            await transaction.RollbackAsync();
            throw;
          }
        }
      });

      return result;
    }


    // Helper method to save uploaded files (example implementation)
    private async Task<string> SaveFileAsync(IFormFile file)
    {
      var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
      var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/formfile", fileName);
      using (var stream = new FileStream(filePath, FileMode.Create))
      {
        await file.CopyToAsync(stream);
      }
      return $"/uploads/formfile/{fileName}";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BackToEdit(string PreviewJson)
    {
      if (string.IsNullOrWhiteSpace(PreviewJson))
      {
        return BadRequest("Preview data is missing.");
      }

      FormFillViewModel model;
      try
      {
        model = JsonConvert.DeserializeObject<FormFillViewModel>(PreviewJson);
      }
      catch (JsonException)
      {
        return BadRequest("Invalid preview data.");
      }

      // Store the model in TempData so it can be used in Fill action
      TempData["FormFillModel"] = PreviewJson;

      return RedirectToAction("Fill", new { id = model.FormId, auditId = model.AuditId });
    }



  }
}
