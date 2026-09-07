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
    public class InterestAreasModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InterestAreasModel> _logger;

        public InterestAreasModel(ApplicationDbContext context, ILogger<InterestAreasModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InterestAreas NewInterestArea { get; set; } = new();

        [BindProperty]
        public InterestAreas? EditInterestArea { get; set; }

        [BindProperty]
        public int? EditInterestAreaId { get; set; }

        public List<InterestAreas> InterestAreasList { get; set; } = new();

        public void OnGet()
        {
            LoadAreas();
        }

        // Add new interest area
        public IActionResult OnPost()
        {
            FixModelStateBinding("NewInterestArea", "InterestArea", NewInterestArea.InterestArea);

            ModelState.Remove("EditInterestArea.InterestArea");

            if (!ModelStateCheck("OnPost NewInterestArea"))
                return Page();

            try
            {
                var existing = _context.InterestAreas
                    .FirstOrDefault(ia => EF.Functions.Like(ia.InterestArea, NewInterestArea.InterestArea));

                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        ModelState.AddModelError("NewInterestArea.InterestArea",
                            "An active Interest Area with this name already exists.");
                        ModelStateCheck("OnPost Duplicate");
                        return Page();
                    }

                    // Reactivate inactive area
                    existing.IsActive = true;
                    existing.Description = NewInterestArea.Description;
                    existing.UpdatedDate = DateTime.UtcNow;
                    _context.SaveChanges();

                    _logger.LogInformation("Reactivated Interest Area: {InterestArea}", existing.InterestArea);
                    return RedirectToPage();
                }

                // Add new area
                NewInterestArea.CreatedDate = DateTime.UtcNow;
                NewInterestArea.IsActive = true;

                _context.InterestAreas.Add(NewInterestArea);
                _context.SaveChanges();

                _logger.LogInformation("Added new Interest Area: {InterestArea}", NewInterestArea.InterestArea);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new Interest Area");
                ModelState.AddModelError(string.Empty, "An error occurred while adding the Interest Area.");
                LoadAreas();
                return Page();
            }
        }

        // Load edit form
        public IActionResult OnPostEdit(int id)
        {
            ModelState.Remove("NewInterestArea.InterestArea");
            ModelState.Remove("NewInterestArea.Description");
            ModelState.Remove("InterestArea");

            var existingArea = _context.InterestAreas.Find(id);

            if (existingArea == null)
            {
                _logger.LogWarning("Interest Area with ID {Id} not found for editing", id);
                return NotFound();
            }

            EditInterestAreaId = id;
            EditInterestArea = existingArea;

            LoadAreas();
            return Page();
        }

        // Save edited interest area
        public IActionResult OnPostSaveEdit()
        {
            if (EditInterestArea == null)
            {
                _logger.LogWarning("EditInterestArea is null in OnPostSaveEdit");
                return BadRequest();
            }
            FixModelStateBinding("EditInterestArea", "InterestArea", EditInterestArea.InterestArea);
            ModelState.Remove("NewInterestArea.InterestArea");
            ModelState.Remove("NewInterestArea.Description");
            if (!ModelStateCheck("OnPostSaveEdit"))
                return Page();
            try
            {
                var existing = _context.InterestAreas.Find(EditInterestArea.InterestAreaID);
                if (existing == null)
                {
                    _logger.LogWarning("Interest Area with ID {Id} not found for saving edits", EditInterestArea.InterestAreaID);
                    return NotFound();
                }
                // Check for duplicates
                var duplicate = _context.InterestAreas
                    .FirstOrDefault(ia => 
                        ia.InterestAreaID != EditInterestArea.InterestAreaID &&
                        EF.Functions.Like(ia.InterestArea, EditInterestArea.InterestArea));

                if (duplicate != null)
                {
                    ModelState.AddModelError("EditInterestArea.InterestArea",
                        "An Interest Area with this name already exists.");
                    ModelStateCheck("OnPostSaveEdit Duplicate");
                    //LoadAreas();
                    return Page();
                }
                // Update area
                existing.InterestArea = EditInterestArea.InterestArea;
                existing.Description = EditInterestArea.Description;                
                existing.UpdatedDate = DateTime.UtcNow;

                _context.SaveChanges();
                _logger.LogInformation("Updated Interest Area {InterestArea}",  existing.InterestArea);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving edits to Interest Area ID {Id}", EditInterestArea.InterestAreaID);
                ModelState.AddModelError(string.Empty, "An error occurred while saving the Interest Area.");
                LoadAreas();
                return Page();
            }
        }

        public IActionResult OnPostCancelEdit() => RedirectToPage();

        // Deactivate interest area
        public IActionResult OnPostDeactivate(int id)
        {
            try
            {
                ModelState.Remove("InterestArea");
                var existing = _context.InterestAreas.Find(id);
                if (existing == null)
                {
                    _logger.LogWarning("Interest Area with ID {Id} not found for deactivation", id);
                    return NotFound();
                }
                existing.IsActive = false;
                existing.UpdatedDate = DateTime.UtcNow;
                _context.SaveChanges();
                _logger.LogInformation("Deactivated Interest Area ID {Id}: {InterestArea}", existing.InterestAreaID, existing.InterestArea);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating Interest Area ID {Id}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while deactivating the Interest Area.");
                LoadAreas();
                return Page();
            }
        }

        // Reactivate interest area
        public IActionResult OnPostReactivate(int id)
        {
            try
            {
                var existing = _context.InterestAreas.Find(id);
                if (existing == null)
                {
                    _logger.LogWarning("Interest Area with ID {Id} not found for reactivation", id);
                    return NotFound();
                }
                existing.IsActive = true;
                existing.UpdatedDate = DateTime.UtcNow;
                _context.SaveChanges();
                _logger.LogInformation("Reactivated Interest Area ID {Id}: {InterestArea}", existing.InterestAreaID, existing.InterestArea);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating Interest Area ID {Id}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while reactivating the Interest Area.");
                LoadAreas();
                return Page();
            }
        }

        // Helper: Load areas
        private void LoadAreas()
        {
            InterestAreasList = _context.InterestAreas
                .OrderBy(ia => ia.InterestArea)
                .ToList();
        }

        // ModelState debugger
        private bool ModelStateCheck(string contextName = "")
        {
            if (ModelState.IsValid) return true;

            _logger.LogWarning("ModelState is invalid in {Context}. Submitted values: {@Values}",
                contextName,
                Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString()));


            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    _logger.LogWarning("Field {Key}: {Error}",entry.Key, error.ErrorMessage);
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
