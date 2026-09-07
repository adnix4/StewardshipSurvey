using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StewardshipSurvey.Data;
using StewardshipSurvey.Models;

namespace StewardshipSurvey.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class InvolvementAreasModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvolvementAreasModel> _logger;

        public InvolvementAreasModel(ApplicationDbContext context, ILogger<InvolvementAreasModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InvolvementAreas NewInvolvementArea { get; set; } = new();

        [BindProperty]
        public InvolvementAreas? EditInvolvementArea { get; set; }

        [BindProperty]
        public int? EditInvolvementAreaId { get; set; }

        public List<InvolvementAreas> InvolvementAreasList { get; set; } = new();

        public void OnGet()
        {
            LoadAreas();
        }

        // Add new involvement area
        public IActionResult OnPost()
        {
            FixModelStateBinding("NewInvolvementArea", "AreaOfInvolvement", NewInvolvementArea.AreaOfInvolvement);

            ModelState.Remove("EditInvolvementArea.AreaOfInvolvement");

            if (!ModelStateCheck("OnPost NewInvolvementArea"))
                return Page();

            try
            {
                var existing = _context.InvolvementAreas
                    .FirstOrDefault(ia => EF.Functions.Like(ia.AreaOfInvolvement, NewInvolvementArea.AreaOfInvolvement));

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        ModelState.AddModelError("NewInvolvementArea.AreaOfInvolvement",
                            "This involvement area already exists.");
                        ModelStateCheck("OnPost Duplicate");
                        return Page();
                    }

                    // Reactivate inactive area
                    existing.IsActive = true;
                    existing.Description = NewInvolvementArea.Description;
                    existing.UpdatedDate = DateTime.UtcNow;
                    _context.SaveChanges();

                    _logger.LogInformation("Reactivated involvement area {Area}", existing.AreaOfInvolvement);
                    return RedirectToPage();
                }

                // Create new area
                NewInvolvementArea.CreatedDate = DateTime.UtcNow;
                NewInvolvementArea.IsActive = true;

                _context.InvolvementAreas.Add(NewInvolvementArea);
                _context.SaveChanges();

                _logger.LogInformation("Added new involvement area {Area}", NewInvolvementArea.AreaOfInvolvement);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving new involvement area");
                ModelState.AddModelError(string.Empty, "Error saving the involvement area.");
                LoadAreas();
                return Page();
            }
        }

        // Load edit form
        public IActionResult OnPostEdit(int id)
        {
            ModelState.Remove("NewInvolvementArea.AreaOfInvolvement");
            ModelState.Remove("NewInvolvementArea.Description");
            ModelState.Remove("AreaOfInvolvement");

            var existingArea = _context.InvolvementAreas.Find(id);

            if (existingArea == null)
            {
                _logger.LogWarning("Involvement area with ID {Id} not found for editing", id);
                return NotFound();
            }        

            EditInvolvementAreaId = id;
            EditInvolvementArea = existingArea;

            LoadAreas();
            return Page();
        }

        // Save edit
        public IActionResult OnPostSaveEdit()
        {
            if (EditInvolvementArea == null)
            {

                return BadRequest();
            }
            FixModelStateBinding("EditInvolvementArea", "AreaOfInvolvement", EditInvolvementArea.AreaOfInvolvement);
            ModelState.Remove("NewInvolvementArea.AreaOfInvolvement");

            if (!ModelStateCheck("OnPostSaveEdit"))
                return Page();

            var existingArea = _context.InvolvementAreas.Find(EditInvolvementArea.InvolvementAreaID);
            if (existingArea == null) return NotFound();

            // Prevent duplicate names
            var duplicate = _context.InvolvementAreas
                .FirstOrDefault(ia =>
                    ia.InvolvementAreaID != EditInvolvementArea.InvolvementAreaID &&
                    EF.Functions.Like(ia.AreaOfInvolvement, EditInvolvementArea.AreaOfInvolvement) &&
                    ia.IsActive);

            if (duplicate != null)
            {
                ModelState.AddModelError("EditInvolvementArea.AreaOfInvolvement",
                    "Another active involvement area already has this name.");
                ModelStateCheck("Save Edit Duplicate");
                return Page();
            }

            existingArea.AreaOfInvolvement = EditInvolvementArea.AreaOfInvolvement;
            existingArea.Description = EditInvolvementArea.Description;
            existingArea.UpdatedDate = DateTime.UtcNow;

            _context.SaveChanges();
            _logger.LogInformation("Edited involvement area {Area}", existingArea.AreaOfInvolvement);

            return RedirectToPage();
        }

        public IActionResult OnPostCancelEdit() => RedirectToPage();

        // Soft delete
        public IActionResult OnPostDelete(int id)
        {
            ModelState.Remove("NewInvolvementArea.AreaOfInvolvement");
            ModelState.Remove("NewInvolvementArea.Description");
            ModelState.Remove("AreaOfInvolvement");
            if (!ModelStateCheck("OnPostDelete"))
                return Page();
           

            var involvementArea = _context.InvolvementAreas.Find(id);
            if (involvementArea != null)
            {
                involvementArea.IsActive = false;
                involvementArea.UpdatedDate = DateTime.UtcNow;
                _context.SaveChanges();

                _logger.LogInformation("Soft deleted involvement area {Area}", involvementArea.AreaOfInvolvement);
            }
            return RedirectToPage();
        }

        // Reactivate deleted
        public IActionResult OnPostReactivate(int id)
        {
            ModelState.Remove("AreaOfInvolvement");
            ModelState.Remove("NewInvolvementArea.AreaOfInvolvement");
            ModelState.Remove("NewInvolvementArea.Description");
            if (!ModelStateCheck("OnPostReactivate"))
                return Page();

            var involvementArea = _context.InvolvementAreas.Find(id);
            if (involvementArea != null && !involvementArea.IsActive)
            {
                involvementArea.IsActive = true;
                involvementArea.UpdatedDate = DateTime.UtcNow;
                _context.SaveChanges();

                _logger.LogInformation("Reactivated involvement area {Area}", involvementArea.AreaOfInvolvement);
            }
            return RedirectToPage();
        }

        // Helper: Load areas (with optional inactive)
        private void LoadAreas()
        {
           
            InvolvementAreasList = _context.InvolvementAreas
                .OrderBy(ia => ia.AreaOfInvolvement)
                .ToList();
        }

        // ModelState debugger
        private bool ModelStateCheck(string contextName = "")
        {
            if (ModelState.IsValid) return true;

            _logger.LogWarning("ModelState invalid in {Context}. Submitted values: {@Values}",
                contextName,
                Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString()));

            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    _logger.LogWarning("Field {Key}: {Error}", entry.Key, error.ErrorMessage);
                }
            }

            LoadAreas();
            return false;
        }

        // Fix Model State binding issue
        private void FixModelStateBinding(string propertyPrefix, string propertyName, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var key = $"{propertyPrefix}.{propertyName}";
                ModelState.Remove(propertyName);
                ModelState.SetModelValue(key, new ValueProviderResult(value));
            }
        }
    }
}
