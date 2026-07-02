using LibTrack.Models;
using LibTrack.Models.Data;
using LibTrack.Models.Entities;
using LibTrack.ViewModels.Entities.Books;
using LibTrack.ViewModels.Entities.BookUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

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
        const int pageSize = 20;
        string cacheKey = $"profile:index";
        List<BookUsersListItemViewModel>? bookUsers;
        if (_cache.TryGetValue(cacheKey, out bookUsers))
        {
            _logger.LogInformation(
                "[Cache HIT] - Profile");
        }
        else
        {
            _logger.LogInformation(
                "[Cache MISS] - Profile");
            bookUsers = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);

                var data = await (
                    from p in _context
                        .Set<BookUser>()
                        .OrderByDescending(p => p.Id)
                        .Where(p => p.UserId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value))
                        .Take(pageSize)
                        .AsNoTracking()
                    select new BookUsersListItemViewModel
                    {
                        Id = p.Id,
                        BookId = p.BookId,
                        BookName = p.Book.Name,
                        UserId = p.UserId,
                        UserEmail = p.User.Email,
                        IssueDate = p.IssueDate,
                        EstimatedReturnDate = p.EstimatedReturnDate,
                        ActualReturnDate = p.ActualReturnDate
                    }
                    ).ToListAsync(ct);
                return data;
            });

        }
        var paged = await PaginatedList<BookUsersListItemViewModel>.CreateAsync(
                bookUsers!, 1, pageSize);

        BookUsersIndexViewModel viewModel = new BookUsersIndexViewModel(paged);
        _logger.LogInformation(
            "Долги получены (Count={Count}).",
            viewModel.Items!.Count);
        return View(viewModel);
    }

    [HttpGet("profile-loan/{id}")]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct = default)
    {
        int userid = _context.BookUsers.FirstAsync(x => x.Id == id, ct).Result.UserId;
        if(userid == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value))
        {
            BookUsersDetailsViewModel? book;
            book = await (
                    from p in _context
                        .Set<BookUser>()
                        .Where(p => p.Id == id)
                    select new BookUsersDetailsViewModel
                    {
                        Id = p.Id,
                        BookId = p.BookId,
                        UserId = p.UserId,
                        IssueDate = p.IssueDate,
                        EstimatedReturnDate = p.EstimatedReturnDate,
                        ActualReturnDate = p.ActualReturnDate,
                        Extented = p.Extented,
                        Book = p.Book,
                        User = p.User
                    }
                    ).FirstOrDefaultAsync(ct);
            if (book is null)
            {
                _logger.LogWarning(
                    "Информация о долге не найдена (Id={id}).",
                    id);
                return NotFound();
            }

            _logger.LogInformation(
                 "Информация о долге получена (Id={id}).",
                 id);
            return View(book);
        }
        return StatusCode(403);
    }
}