using BackendApi.Data;
using BackendApi.DTOs.Auth;
using BackendApi.Services;
using BackendApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwordService;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, PasswordService passwordService, IConfiguration config)
    {
        _db = db;
        _passwordService = passwordService;
        _config = config;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var email = InputNormalization.NormalizeEmail(request.Email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            _passwordService.VerifyAgainstDummyHash(request.Password);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (!_passwordService.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (_passwordService.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordService.Hash(request.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var jwtKey = _config["Jwt:Key"]!;
        var key = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("token_version", user.TokenVersion.ToString())
        };

        var expiryMinutes = Math.Clamp(_config.GetValue("Jwt:ExpiryMinutes", 60), 5, 120);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        );

        return Ok(new
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = user.Role,
            Email = user.Email,
            ExpiresAt = expiresAt
        });
    }
}
