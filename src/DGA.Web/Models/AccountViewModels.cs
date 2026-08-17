using System.ComponentModel.DataAnnotations;

namespace DGA.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresá tu correo institucional.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo válido.")]
    [Display(Name = "Correo Institucional")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Ingresá tu correo institucional.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo válido.")]
    [Display(Name = "Correo institucional")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
