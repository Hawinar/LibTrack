using LibTrack.Models.Data;
using LibTrack.ViewModels.Entities.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LibTrack.Controllers;

[Authorize]
public class UsersController(
    ILogger<UsersController> logger,
    LibTrackDbContext context,
    IMemoryCache cache) : Controller
{
    private readonly ILogger<UsersController> _logger = logger;
    private readonly LibTrackDbContext _context = context;
    private readonly IMemoryCache _cache = cache;

    [HttpGet("profile")]
    public async Task<IActionResult> Index(
        
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        return await Index( page, ct);
    }
}