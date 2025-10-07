// ============================================
// CAMINHO: FCG.Game.API\Controllers\ApiBaseController.cs
// A��O: CRIAR arquivo novo
// ============================================

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG.Game.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiBaseController : ControllerBase
{
    /// <summary>
    /// Extrai o ID do usu�rio do token JWT
    /// </summary>
    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
    /// Extrai o nome do usu�rio do token JWT
    /// </summary>
    protected string GetUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
    }

    /// <summary>
    /// Extrai o email do usu�rio do token JWT
    /// </summary>
    protected string GetUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}