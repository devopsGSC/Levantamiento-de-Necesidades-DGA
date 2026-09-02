using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DGA.Web.Data.Entities;

public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Usuario = "Usuario";
    public const string ComprasDGA = "ComprasDGA";
    public const string MantenimientoDGA = "MantenimientoDGA";
    public const string Otro = "Otro";

    /// <summary>
    /// Roles a los que el Administrador delega la tramitación de una solicitud ya
    /// Aprobada, según la Unidad Ejecutora que le asignó al aprobarla (ids del catálogo
    /// UnidadesEjecutoras, ver database/11_unidad_ejecutora.sql). El admin actúa de
    /// dispatcher: al aprobar elige la Unidad Ejecutora y la solicitud le "cae"
    /// automáticamente al usuario con el rol correspondiente.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, byte> UnidadEjecutoraPorRolDelegado = new Dictionary<string, byte>
    {
        [MantenimientoDGA] = 1,
        [ComprasDGA] = 2,
        [Otro] = 3,
    };

    public static readonly string[] Delegados = [MantenimientoDGA, ComprasDGA, Otro];
    public static readonly string[] Todos = [Administrador, Usuario, .. Delegados];

    public static bool EsRolDelegado(ClaimsPrincipal user) => Delegados.Any(user.IsInRole);

    public static byte? UnidadEjecutoraDelRolDelegado(ClaimsPrincipal user) =>
        UnidadEjecutoraPorRolDelegado
            .Where(kv => user.IsInRole(kv.Key))
            .Select(kv => (byte?)kv.Value)
            .FirstOrDefault();
}
