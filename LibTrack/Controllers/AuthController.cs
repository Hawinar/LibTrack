using LibTrack.Models;
using LibTrack.Models.Data;
using LibTrack.Models.Entities;
using LibTrack.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibTrack.Controllers;

[Route("auth")]
public class AuthController(
    ILogger<AuthController> logger,
    LibTrackDbContext context) : Controller
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly LibTrackDbContext _context = context;

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }
    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginRequest request,
        string? returnUrl = null,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(request);
        }
        PasswordHasher passwordHasher = new PasswordHasher();
        User? user = await _context.Users.Where(x => x.Email.ToLower() == request.Email.ToLower()).FirstOrDefaultAsync(ct);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Данного пользователя не существует.");
            return View(request);
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
            return View(request);
        }

        await SignInAsync(user, rememberMe: request.RememberMe);
        _logger.LogInformation(
            "Пользователь (UserId={UserId}) успешно вошёл в систему.",
            user.Id);
        return RedirectAfterLogin(returnUrl);
    }
    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
    RegisterRequest request,
        string? returnUrl = null,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(request);
        }
        bool match = await _context.Users.Where(x => x.Email.ToLower() == request.Email.ToLower()).AnyAsync(ct);
        if (match)
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким email уже существует.");
            return View(request);
        }
        Role role = await _context.Roles.Where(x => x.Name == AppRoles.User).FirstAsync(ct);
        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName.Trim(),
            RoleId = role.Id,
            Role = role
        };
        PasswordHasher passwordHasher = new PasswordHasher();
        user.PasswordHash = passwordHasher.HashPassword(request.Password);
        await _context.Users.AddAsync(user, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Например, другой запрос зарегистрировал этот email
            // между AnyAsync и SaveChangesAsync.
            ModelState.AddModelError(
                nameof(RegisterRequest.Email),
                "Пользователь с таким email уже зарегистрирован.");

            ViewBag.ReturnUrl = returnUrl;
            return View(request);
        }



        await SignInAsync(user, rememberMe: false);

        _logger.LogInformation(
            "Создан новый пользователь (UserId={UserId}).",
            user.Id);

        return RedirectAfterLogin(returnUrl);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        string? returnUrl = null,
        CancellationToken ct = default)
    {
        await HttpContext.SignOutAsync();
        _logger.LogInformation(
            "Пользователь (UserId={UserId}) успешно вышел из системы.",
            HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);

        return RedirectAfterLogin(returnUrl);
    }

    private async Task SignInAsync(
        User user, 
        bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),

            // Роль берётся из БД и попадает в cookie claim.
            new(ClaimTypes.Role, AppRoles.GetRoleName(user.RoleId))
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            // false: cookie удалится при закрытии браузера.
            // true: cookie сохранится в браузере до истечения срока.
            IsPersistent = rememberMe,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(
            actionName: "Index",
            controllerName: "Home")!;
    }
}