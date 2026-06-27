using LibTrack.Models;
using LibTrack.Models.Data;
using LibTrack.Models.Entities;
using LibTrack.ViewModels.Entities.Books;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LibTrack.Controllers;

public class BooksController(
    ILogger<BooksController> logger,
    LibTrackDbContext context,
    IMemoryCache cache) : Controller
{
    private readonly ILogger<BooksController> _logger = logger;
    private readonly LibTrackDbContext _context = context;
    private readonly IMemoryCache _cache = cache;

    [HttpGet("catalog")]
    public async Task<IActionResult> Index(
        CancellationToken ct = default)
    {
        int pageSize = 10;
        string cacheKey = $"books:index";
        List<BookListItemViewModel>? books;
        if (_cache.TryGetValue(cacheKey, out books))
        {
            _logger.LogInformation(
                "[Cache HIT] - Home");
        }
        else
        {
            _logger.LogInformation(
                "[Cache MISS] - Home");
            books = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);

                var data = await (
                    from p in _context
                        .Set<Book>()
                        .OrderByDescending(p => p.Id)
                        .Take(pageSize)
                        .AsNoTracking()
                    select new BookListItemViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImagePath = p.Image,
                        Genre = p.Genre.Name,
                        Date = p.AddDate
                    }
                    ).ToListAsync(ct);
                return data;
            });

        }
        var paged = await PaginatedList<BookListItemViewModel>.CreateAsync(
                books!, 1, pageSize);

        BookIndexViewModel viewModel = new BookIndexViewModel(paged);
        _logger.LogInformation(
            "Книги получены (Count={Count}).",
            viewModel.Items!.Count);
        return View(viewModel);
    }

    [HttpGet("catalog/{id}")]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct = default)
    {
        string cacheKey = $"books:details:{id}";
        BookDetailsViewModel? book;
        if (_cache.TryGetValue(cacheKey, out book))
        {
            _logger.LogInformation(
                "[Cache HIT] - Книга (Id={id}).",
                id);
        }
        else
        {
            _logger.LogInformation(
                "[Cache MISS] - Книга (Id={id}).",
                id);

            book = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);

                var data = await (
                    from p in _context
                        .Set<Book>()
                        .Where(p => p.Id == id)
                        .AsNoTracking()
                    select new BookDetailsViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Author = p.Author,
                        Description = p.Description,
                        Image = p.Image,
                        Genre = p.Genre.Name,
                        Date = p.AddDate
                    }
                    ).FirstOrDefaultAsync(ct);
                return data;
            });
        }
        if (book is null)
        {
            _logger.LogWarning(
                "Книга не найдена (Id={id}).",
                id);
            return NotFound();
        }

        _logger.LogInformation(
             "Книга получена (Id={id}).",
             id);
        return View(book);
    }
}