using System.Text;
using System.Text.Encodings.Web;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using DGA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace DGA.Web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IWebHostEnvironment environment,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Ver comentario en el GET de ResetPassword: mismo riesgo de colisión de
        // ModelState entre el parámetro "returnUrl" y LoginViewModel.ReturnUrl.
        ModelState.Clear();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await userManager.FindByEmailAsync(model.Email);
        if (usuario is null || !usuario.Activo)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(usuario, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("Inicio de sesión: {Email}", model.Email);
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente por demasiados intentos fallidos. Intentá de nuevo más tarde.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await userManager.FindByEmailAsync(model.Email);

        // No revelar si la cuenta existe o no — evita enumeración de usuarios.
        if (usuario is not null && usuario.Activo)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var enlace = Url.Action("ResetPassword", "Account",
                new { email = usuario.Email, token = tokenCodificado }, protocol: Request.Scheme)!;

            if (environment.IsDevelopment())
            {
                logger.LogInformation("[DEV] Enlace de recuperación (no se muestra en producción): {Enlace}", enlace);
            }

            await emailSender.SendAsync(
                usuario.Email!,
                "Recuperación de contraseña — Levantamiento de Necesidades DGA",
                $"Hacé clic en el siguiente enlace para restablecer tu contraseña: " +
                $"<a href=\"{HtmlEncoder.Default.Encode(enlace)}\">{HtmlEncoder.Default.Encode(enlace)}</a>. " +
                "Si no solicitaste este cambio, ignorá este correo.");
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (email is null || token is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var tokenDecodificado = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

        // Importante: sin este Clear(), el tag helper asp-for="Token" en la vista
        // renderiza el valor de ModelState (el "token" crudo de la query string, todavía
        // codificado en Base64Url) en vez del valor real del modelo, porque el parámetro
        // de esta acción se llama "token" — mismo nombre que ResetPasswordViewModel.Token
        // (ModelState no distingue mayúsculas/minúsculas). Sin esto, el POST reenvía el
        // token codificado y ResetPasswordAsync falla con "Invalid token".
        ModelState.Clear();

        return View(new ResetPasswordViewModel { Email = email, Token = tokenDecodificado });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var usuario = await userManager.FindByEmailAsync(model.Email);
        if (usuario is null)
        {
            // No revelar si la cuenta existe o no.
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await userManager.ResetPasswordAsync(usuario, model.Token, model.Password);
        if (result.Succeeded)
        {
            usuario.PasswordTemporal = false;
            await userManager.UpdateAsync(usuario);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
