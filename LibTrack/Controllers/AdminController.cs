using LibTrack.Models;
using LibTrack.Models.Data;
using LibTrack.Models.Entities;
using LibTrack.ViewModels.Entities.Books;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibTrack.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    IWebHostEnvironment environment,
    ILogger<AdminController> logger,
    LibTrackDbContext context) : Controller
{
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<AdminController> _logger = logger;
    private readonly LibTrackDbContext _context = context;

    [HttpGet("admin-catalog")]
    public async Task<IActionResult> Index(
        [FromQuery] BookFilter filter,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        const int pageSize = 40;

        page = Math.Max(page, 1);

        IQueryable<Book> query = _context.Books;

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
            "[Admin Panel] Каталог загружен. Page={Page}, Count={Count}, Search={Search}, GenreId={GenreId}, OnlyAvailable={OnlyAvailable}",
            page,
            pagedBooks.Count,
            filter.Search,
            filter.GenreId,
            filter.OnlyAvailable);

        return View(viewModel);
    }
    [HttpGet("admin-catalog/{id}")]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct = default)
    {
        BookDetailsViewModel? book;
        book = await (
                from p in _context
                    .Set<Book>()
                    .Where(p => p.Id == id)
                select new BookDetailsViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Author = p.Author,
                    Description = p.Description,
                    ImagePath = p.Image,
                    GenreId = p.GenreId,
                    Genre = p.Genre,
                    AddDate = p.AddDate,
                    UpdateDate = p.UpdateDate,
                    IsAvailable = !p.BookUsers.Any(bookUser =>
                        bookUser.ActualReturnDate == null)
                }
                ).FirstOrDefaultAsync(ct);
        if (book is null)
        {
            _logger.LogWarning(
                "[Admin Panel] Книга не найдена (Id={id}).",
                id);
            return NotFound();
        }

        _logger.LogInformation(
             "[Admin Panel] Книга получена (Id={id}).",
             id);
        return View(book);
    }

    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct = default)
    {
        try
        {
            await _context.Books.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "[Admin Panel] Книга удалена (Id={id}).",
                id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin Panel] Отказ удаления книги. Убедитесь, что записи в BookUser отсутствуют. (BookId={id})", id);
            return StatusCode(500, "Произошла ошибка при удалении книги.");
        }
        return RedirectToAction("Index");
    }

    [HttpGet("admin-catalog/create")]
    public async Task<IActionResult> Create(
        CancellationToken ct = default)
    {
        ViewBag.Genres = await _context.Genres
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync(ct);
        return View(new BookDetailsViewModel());
    }

    [HttpPost()]
    public async Task<IActionResult> Create(
        BookDetailsViewModel book,
        CancellationToken ct = default)
    {
        if (book.ImageFile is not null && book.ImageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(book.ImageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await book.ImageFile.CopyToAsync(stream, ct);

            book.ImagePath = $"/uploads/{fileName}";
        }


        await _context.Books.AddAsync(new Book
        {
            Name = book.Name,
            Author = book.Author,
            Description = book.Description,
            Image = book.ImagePath,
            GenreId = book.GenreId,
            Genre = book.Genre,
            AddDate = DateTime.Now
        }, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogWarning(
                "[Admin Panel] Книга добавлена (Id={id}).",
                book.Id);
        return RedirectToAction("Index");
    }

    [HttpGet("admin-catalog/{id}/edit")]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken ct = default)
    {

        ViewBag.Genres = await _context.Genres
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Name
            })
            .ToListAsync(ct);

        BookDetailsViewModel? book;
        book = await (
                from p in _context
                    .Set<Book>()
                    .Where(p => p.Id == id)
                select new BookDetailsViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Author = p.Author,
                    Description = p.Description,
                    ImagePath = p.Image,
                    GenreId = p.GenreId,
                    Genre = p.Genre,
                    AddDate = p.AddDate,
                    UpdateDate = p.UpdateDate,
                    IsAvailable = !p.BookUsers.Any(bookUser =>
                        bookUser.ActualReturnDate == null)
                }
                ).FirstOrDefaultAsync(ct);
        if (book is null)
        {
            _logger.LogWarning(
                "[Admin Panel] Книга не найдена (Id={id}).",
                id);
            return NotFound();
        }

        _logger.LogInformation(
             "[Admin Panel] Книга получена (Id={id}).",
             id);
        return View(book);
    }


    [HttpPost()]
    public async Task<IActionResult> PutImage(
        BookDetailsViewModel book,
        CancellationToken ct = default)
    {
        
        return RedirectToAction("Edit", new { id = book.Id });
    }

    [HttpPost("admin-catalog/{id}/edit")]
    public async Task<IActionResult> Edit(
        BookDetailsViewModel book,
        CancellationToken ct = default)
    {
        if (book.ImageFile is not null && book.ImageFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(book.ImageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await book.ImageFile.CopyToAsync(stream, ct);

            book.ImagePath = $"/uploads/{fileName}";
        }

        _context.Books.Update(new Book
        {
            Id = book.Id,
            Name = book.Name,
            Author = book.Author,
            Description = book.Description,
            Image = book.ImagePath,
            GenreId = book.GenreId,
            Genre = _context.Genres.Where(x => x.Id == book.GenreId).First(),
            AddDate = book.AddDate,
            UpdateDate = DateTime.Now
        });
        await _context.SaveChangesAsync(ct);
        _logger.LogWarning(
                "[Admin Panel] Книга изменена (Id={id}).",
                book.Id);
        return RedirectToAction("Index");
    }
}