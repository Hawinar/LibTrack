using LibTrack.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LibTrack.Controllers
{
    public class DataScienceController(
    ILogger<DataScienceController> logger,
    LibTrackDbContext context,
    IMemoryCache cache) : Controller
    {
        private readonly ILogger<DataScienceController> _logger = logger;
        private readonly LibTrackDbContext _context = context;
        private readonly IMemoryCache _cache = cache;

        public async Task<IActionResult> Index()
        {
            // =========================
            // Процент возвратов с задержкой
            // =========================

            var totalReturned = await _context.BookUsers
                .Where(x => x.ActualReturnDate != null)
                .CountAsync();

            var delayedReturned = await _context.BookUsers
                .Where(x =>
                    x.ActualReturnDate != null &&
                    x.ActualReturnDate > x.EstimatedReturnDate)
                .CountAsync();

            var normalReturned = totalReturned - delayedReturned;

            ViewBag.ReturnStatsLabels = new[]
            {
                "Возвращено вовремя",
                "Возвращено с задержкой"
            };

            ViewBag.ReturnStatsData = new[]
            {
                normalReturned,
                delayedReturned
            };

            // =========================
            // Количество книг по жанрам
            // =========================

            var genres = await _context.Books
                .GroupBy(x => x.Genre.Name)
                .Select(g => new
                {
                    Genre = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.GenreLabels = genres.Select(x => x.Genre).ToArray();
            ViewBag.GenreData = genres.Select(x => x.Count).ToArray();

            // =========================
            // Количество книг по авторам
            // =========================

            var authors = await _context.Books
                .GroupBy(x => x.Author)
                .Select(g => new
                {
                    Author = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewBag.AuthorLabels = authors.Select(x => x.Author).ToArray();
            ViewBag.AuthorData = authors.Select(x => x.Count).ToArray();

            return View();
        }
    }
}
