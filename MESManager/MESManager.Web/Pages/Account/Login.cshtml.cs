using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MESManager.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; } = "/";

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Il nome utente è obbligatorio")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet(string? error = null, string? returnUrl = null)
    {
        ReturnUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";
        ErrorMessage = error switch
        {
            "locked" => "Account bloccato temporaneamente per troppi tentativi. Riprova tra 15 minuti.",
            _ => null
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Compila tutti i campi richiesti.";
            return Page();
        }

        // Prevenzione open redirect attack
        var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";

        var result = await _signInManager.PasswordSignInAsync(
            Input.Username,
            Input.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Login effettuato: {Username}", Input.Username);
            return LocalRedirect(safeReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account bloccato: {Username}", Input.Username);
            return RedirectToPage(new { error = "locked", returnUrl = ReturnUrl });
        }

        ErrorMessage = "Nome utente o password non validi.";
        _logger.LogWarning("Login fallito: {Username}", Input.Username);
        return Page();
    }
}
