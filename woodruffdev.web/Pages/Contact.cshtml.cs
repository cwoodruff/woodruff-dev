using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WoodruffDev.Pages;

public class ContactModel : PageModel
{
    private readonly ILogger<ContactModel> _logger;

    public ContactModel(ILogger<ContactModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public ContactFormInput Input { get; set; } = new();

    public bool FormSubmitted { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _logger.LogInformation(
            "Contact form submitted by {Name} ({Email}) — Subject: {Subject}",
            Input.Name,
            Input.Email,
            Input.Subject);

        // TODO: Send email, save to database, or forward to an external service.

        FormSubmitted = true;
        return Page();
    }

    public class ContactFormInput
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone, StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }

        [Required, StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Message { get; set; }
    }
}
