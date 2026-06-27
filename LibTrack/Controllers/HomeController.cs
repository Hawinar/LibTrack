using LibTrack.Models;
using LibTrack.Models.Data;
using LibTrack.Models.Entities;
using LibTrack.ViewModels.Entities.Books;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace LibTrack.Controllers;

public class HomeController(
    ILogger<HomeController> logger,
    LibTrackDbContext context,
    IMemoryCache cache) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly LibTrackDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    public async Task<IActionResult> Index(
        CancellationToken ct = default)
    {
        int pageSize = 9;
        string cacheKey = $"home:index";
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


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}