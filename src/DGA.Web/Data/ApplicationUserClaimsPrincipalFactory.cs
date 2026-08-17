using System.Security.Claims;
using DGA.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace DGA.Web.Data;

/// <summary>Agrega el nombre para mostrar como claim, así el layout no necesita una consulta extra a la BD en cada request.</summary>
public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    Microsoft.Extensions.Options.IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimTypes.GivenName, user.Nombre));
        return principal;
    }
}
