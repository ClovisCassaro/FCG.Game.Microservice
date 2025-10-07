//// ============================================
//// MÉTODO 2: Criar um endpoint /auth/login MOCK para gerar tokens
//// CAMINHO: FCG.Game.API\Controllers\AuthController.cs
//// AÇÃO: CRIAR arquivo novo (APENAS PARA TESTES!)
//// ============================================

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//namespace FCG.Game.API.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class AuthController : ControllerBase
//{
//    private readonly IConfiguration _configuration;

//    public AuthController(IConfiguration configuration)
//    {
//        _configuration = configuration;
//    }

//    /// <summary>
//    /// ENDPOINT MOCK para gerar token JWT para testes
//    /// ⚠️ REMOVER EM PRODUÇÃO!
//    /// </summary>
//    [HttpPost("login")]
//    public IActionResult MockLogin([FromBody] MockLoginRequest request)
//    {
//        // Validação simples (apenas para testes!)
//        if (string.IsNullOrEmpty(request.Username))
//        {
//            return BadRequest("Username é obrigatório");
//        }

//        // Gerar token
//        var token = GenerateJwtToken(request.Username, request.UserId);

//        return Ok(new
//        {
//            token = token,
//            userId = request.UserId ?? Guid.NewGuid(),
//            username = request.Username,
//            expiresIn = 3600 // 1 hora
//        });
//    }

//    /// <summary>
//    /// Gera um token JWT válido
//    /// </summary>
//    private string GenerateJwtToken(string username, Guid? userId = null)
//    {
//        var key = _configuration["Jwt:Key"]
//            ?? throw new InvalidOperationException("Jwt:Key não configurada");
//        var issuer = _configuration["Jwt:Issuer"];
//        var audience = _configuration["Jwt:Audience"];

//        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
//        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

//        var userGuid = userId ?? Guid.NewGuid();

//        var claims = new[]
//        {
//            new Claim(JwtRegisteredClaimNames.Sub, userGuid.ToString()),
//            new Claim(JwtRegisteredClaimNames.UniqueName, username),
//            new Claim(ClaimTypes.NameIdentifier, userGuid.ToString()),
//            new Claim(ClaimTypes.Name, username),
//            new Claim(ClaimTypes.Email, $"{username}@test.com"),
//            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//        };

//        var token = new JwtSecurityToken(
//            issuer: issuer,
//            audience: audience,
//            claims: claims,
//            expires: DateTime.UtcNow.AddHours(1),
//            signingCredentials: credentials
//        );

//        return new JwtSecurityTokenHandler().WriteToken(token);
//    }
//}

//public record MockLoginRequest(
//    string Username,
//    Guid? UserId = null
//);

//// ============================================
//// COMO USAR:
//// ============================================

///*

//1. Copie este arquivo para: FCG.Game.API\Controllers\AuthController.cs

//2. Reinicie a API

//3. No PowerShell, adicione ANTES do script principal:

//# Fazer login e obter token
//$loginRequest = @{
//    username = "testuser"
//    userId = "123e4567-e89b-12d3-a456-426614174000"
//} | ConvertTo-Json

//$loginResponse = Invoke-RestMethod `
//    -Uri "http://localhost:5284/api/auth/login" `
//    -Method Post `
//    -Body $loginRequest `
//    -ContentType "application/json"

//$token = $loginResponse.token
//Write-Host "Token gerado: $token" -ForegroundColor Green

//# Usar o token nos headers
//$headers = @{
//    "Content-Type" = "application/json"
//    "Authorization" = "Bearer $token"
//}

//# ... resto do script ...


//4. ⚠️ IMPORTANTE: Este endpoint é APENAS para testes locais!
//   REMOVA ou DESABILITE antes de ir para produção!

//*/