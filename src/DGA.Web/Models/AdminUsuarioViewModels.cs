using System.ComponentModel.DataAnnotations;

namespace DGA.Web.Models;

public class AdminUsuarioListItemViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? Departamento { get; set; }
    public bool Activo { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool EsAdminPrincipal { get; set; }
    public bool EsFilaPropia { get; set; }

    /// <summary>False cuando es la fila del propio admin logueado, o cuando ya es
    /// Administrador y quien mira el listado no es el admin principal.</summary>
    public bool RolEditable { get; set; }
}

public class AdminUsuarioIndexViewModel
{
    public List<AdminUsuarioListItemViewModel> Usuarios { get; set; } = new();
    public string? Busqueda { get; set; }
}

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "Ingresá el correo institucional.")]
    [EmailAddress(ErrorMessage = "Ingresá un correo válido.")]
    [Display(Name = "Correo Institucional")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá el nombre completo.")]
    [Display(Name = "Nombre Completo")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccioná el rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;

    [Display(Name = "Departamento")]
    public string? Departamento { get; set; }
}

public class CargaMasivaResultadoViewModel
{
    public List<string> Creados { get; set; } = new();
    public List<CargaMasivaErrorViewModel> Errores { get; set; } = new();
}

public class CargaMasivaErrorViewModel
{
    public int Fila { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}
