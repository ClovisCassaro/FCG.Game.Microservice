using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG.Game.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiBaseController : ControllerBase
{
    /// <summary>
    /// Extrai o ID do usuário do token JWT.
    /// CORREÇÃO: Verifica ClaimTypes.NameIdentifier (padrão) e "sub" (comum em JWTs).
    /// </summary>
    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("jti")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Extrai o token JWT do header Authorization
    /// </summary>
    protected string GetUserToken()
    {
        return Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    }

    /// <summary>
    /// Extrai o nome do usuário do token JWT
    /// </summary>
    protected string GetUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    }

    /// <summary>
    /// Extrai o email do usuário do token JWT
    /// </summary>
    protected string GetUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}
