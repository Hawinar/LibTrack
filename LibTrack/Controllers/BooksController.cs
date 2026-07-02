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
        [FromQuery] BookFilter filter,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        const int pageSize = 20;

        page = Math.Max(page, 1);

        IQueryable<Book> query = _context.Books
            .AsNoTracking();

        // Поиск по названию и автору.
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string search = filter.Search.Trim();

            query = query.Where(book =>
                book.Name.Contains(search) ||
                book.Author.Contains(search));
        }

        // Жанр.
        if (filter.GenreId.HasValue)
        {
            query = query.Where(book => book.GenreId == filter.GenreId.Value);
        }

        // Наличие.
        if (filter.OnlyAvailable)
        {
            query = query.Where(book =>
                !book.BookUsers.Any(bookUser =>
                    bookUser.ActualReturnDate == null));
        }

        IOrderedQueryable<Book> orderedQuery = filter.SortBy switch
        {
            BookSortBy.Popularity when filter.SortDirection == BookSortDirection.Descending
                => query.OrderByDescending(book => book.BookUsers.Count()),

            BookSortBy.Popularity
                => query.OrderBy(book => book.BookUsers.Count()),

            BookSortBy.PublicationYear when filter.SortDirection == BookSortDirection.Descending
                => query.OrderByDescending(book => book.PublicationYear),

            BookSortBy.PublicationYear
                => query.OrderBy(book => book.PublicationYear),

            BookSortBy.AddDate when filter.SortDirection == BookSortDirection.Descending
                => query.OrderByDescending(book => book.AddDate),

            BookSortBy.AddDate
                => query.OrderBy(book => book.AddDate),

            BookSortBy.UpdateDate when filter.SortDirection == BookSortDirection.Descending
                => query.OrderByDescending(book => book.UpdateDate),

            BookSortBy.UpdateDate
                => query.OrderBy(book => book.UpdateDate),

            _ => query.OrderByDescending(book => book.AddDate)
        };

        // Стабильный порядок: при одинаковых датах, рейтингах и т. п.
        IQueryable<BookListItemViewModel> booksQuery = orderedQuery
            .ThenByDescending(book => book.Id)
            .Select(book => new BookListItemViewModel
            {
                Id = book.Id,
                Name = book.Name,
                ImagePath = book.Image,
                Genre = book.Genre.Name,
                Date = book.AddDate
            });

        var pagedBooks = await PaginatedList<BookListItemViewModel>
            .CreateAsync(booksQuery, page, pageSize);

        var viewModel = new BookIndexViewModel(pagedBooks)
        {
            Filter = filter
        };

        _logger.LogInformation(
            "Каталог загружен. Page={Page}, Count={Count}, Search={Search}, GenreId={GenreId}, OnlyAvailable={OnlyAvailable}",
            page,
            pagedBooks.Count,
            filter.Search,
            filter.GenreId,
            filter.OnlyAvailable);

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
                        ImagePath = p.Image,
                        Genre = p.Genre,
                        PublicationYear = p.PublicationYear,
                        AddDate = p.AddDate,
                        IsAvailable = !p.BookUsers.Any(bookUser =>
                            bookUser.ActualReturnDate == null)
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