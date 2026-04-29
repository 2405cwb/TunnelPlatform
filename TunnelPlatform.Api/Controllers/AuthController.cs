using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Api.Domain;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, CaptchaState> Captchas = new(StringComparer.OrdinalIgnoreCase);
    private readonly TunnelPlatformDbContext _dbContext;

    public AuthController(TunnelPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("captcha")]
    public ActionResult<CaptchaDto> GetCaptcha()
    {
        CleanupCaptchas();

        var code = RandomNumberGenerator.GetInt32(1000, 9999).ToString();
        var captchaId = Guid.NewGuid().ToString("N");
        Captchas[captchaId] = new CaptchaState(code, DateTimeOffset.UtcNow.AddMinutes(5));

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="128" height="44" viewBox="0 0 128 44">
              <defs>
                <linearGradient id="g" x1="0" x2="1">
                  <stop offset="0" stop-color="#07131f"/>
                  <stop offset="1" stop-color="#0f3341"/>
                </linearGradient>
              </defs>
              <rect width="128" height="44" rx="8" fill="url(#g)"/>
              <path d="M6 32 C28 6, 51 41, 74 16 S109 12, 122 31" fill="none" stroke="#27b7d6" stroke-width="1.4" opacity=".75"/>
              <path d="M4 13 L124 28" stroke="#ff684f" stroke-width="1" opacity=".34"/>
              <text x="64" y="30" text-anchor="middle" font-family="Consolas, monospace" font-size="24" font-weight="800" letter-spacing="5" fill="#d9e8f2">{code}</text>
            </svg>
            """;

        return Ok(new CaptchaDto
        {
            CaptchaId = captchaId,
            ImageDataUrl = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}",
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCaptcha(request.CaptchaId, request.CaptchaCode);
        var userName = NormalizeUserName(request.UserName);
        ValidatePassword(request.Password);

        if (await _dbContext.AppUsers.AnyAsync(x => x.UserName == userName, cancellationToken))
        {
            return Conflict(new { message = "用户名已存在。" });
        }

        var isFirstUser = !await _dbContext.AppUsers.AnyAsync(cancellationToken);
        var password = HashPassword(request.Password);
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = userName,
            PasswordHash = password.Hash,
            PasswordSalt = password.Salt,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var roleCode = isFirstUser ? "admin" : "viewer";
        var role = await _dbContext.AppRoles.FirstAsync(x => x.RoleCode == roleCode, cancellationToken);
        user.UserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });

        _dbContext.AppUsers.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await CreateSessionResponse(user, cancellationToken));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        ValidateCaptcha(request.CaptchaId, request.CaptchaCode);
        var userName = NormalizeUserName(request.UserName);
        var user = await _dbContext.AppUsers
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);

        if (user is null || !user.IsActive || !VerifyPassword(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            return Unauthorized(new { message = "用户名或密码错误。" });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await CreateSessionResponse(user, cancellationToken));
    }

    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var token = ReadBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { message = "未登录或登录已过期。" });
        }

        var tokenHash = HashToken(token);
        var session = await _dbContext.AppUserSessions
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                        .ThenInclude(x => x.RolePermissions)
                            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash
                    && x.RevokedAt == null
                    && x.ExpiresAt > DateTimeOffset.UtcNow
                    && x.User.IsActive,
                cancellationToken);

        return session is null
            ? Unauthorized(new { message = "未登录或登录已过期。" })
            : Ok(ToUserDto(session.User));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = ReadBearerToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            var tokenHash = HashToken(token);
            var sessions = await _dbContext.AppUserSessions
                .Where(x => x.TokenHash == tokenHash && x.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.RevokedAt = DateTimeOffset.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private async Task<AuthResponseDto> CreateSessionResponse(AppUser user, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var expiresAt = DateTimeOffset.UtcNow.AddHours(12);
        _dbContext.AppUserSessions.Add(new AppUserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(token),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var loadedUser = await _dbContext.AppUsers
            .AsNoTracking()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.RolePermissions)
                        .ThenInclude(x => x.Permission)
            .FirstAsync(x => x.Id == user.Id, cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToUserDto(loadedUser),
        };
    }

    private static AuthUserDto ToUserDto(AppUser user)
    {
        return new AuthUserDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName,
            Roles = user.UserRoles.Select(x => x.Role.RoleName).Distinct().OrderBy(x => x).ToList(),
            Permissions = user.UserRoles
                .SelectMany(x => x.Role.RolePermissions)
                .Select(x => x.Permission.PermissionCode)
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
        };
    }

    private static string NormalizeUserName(string userName)
    {
        userName = (userName ?? string.Empty).Trim();
        if (userName.Length is < 3 or > 32)
        {
            throw new InvalidOperationException("用户名长度需为 3-32 位。");
        }

        return userName;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new InvalidOperationException("密码长度至少 6 位。");
        }
    }

    private static PasswordResult HashPassword(string password)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return new PasswordResult(Convert.ToBase64String(saltBytes), Convert.ToBase64String(hashBytes));
    }

    private static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256,
            32);
        return CryptographicOperations.FixedTimeEquals(hashBytes, Convert.FromBase64String(expectedHash));
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;
    }

    private static void ValidateCaptcha(string captchaId, string captchaCode)
    {
        CleanupCaptchas();
        if (!Captchas.TryRemove(captchaId ?? string.Empty, out var captcha)
            || captcha.ExpiresAt < DateTimeOffset.UtcNow
            || !string.Equals(captcha.Code, (captchaCode ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("验证码错误或已过期。");
        }
    }

    private static void CleanupCaptchas()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in Captchas.Where(x => x.Value.ExpiresAt < now).ToList())
        {
            Captchas.TryRemove(item.Key, out _);
        }
    }

    private sealed record CaptchaState(string Code, DateTimeOffset ExpiresAt);

    private sealed record PasswordResult(string Salt, string Hash);
}
