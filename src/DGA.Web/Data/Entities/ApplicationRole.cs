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
}
