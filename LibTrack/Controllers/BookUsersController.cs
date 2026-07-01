using LibTrack.Models;
using LibTrack.Models.Data;
using LibTrack.Models.Entities;
using LibTrack.ViewModels.Entities.Books;
using LibTrack.ViewModels.Entities.BookUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibTrack.Controllers;

[Authorize(Roles = "Admin")]
public class BookUsersController(
    ILogger<BookUsersController> logger,
    LibTrackDbContext context) : Controller
{
    private readonly ILogger<BookUsersController> _logger = logger;
    private readonly LibTrackDbContext _context = context;

    [HttpGet("user-loan")]
    public async Task<IActionResult> Index( 
        [FromQuery] BookUsersFilter filter,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        const int pageSize = 40;

        page = Math.Max(page, 1);

        IQueryable<BookUser> query = _context.BookUsers;

        // Наличие.
        if (filter.OnlyActive)
        {
            query = query.Where(bookUsers => bookUsers.ActualReturnDate == null);
        }

        IOrderedQueryable<BookUser> orderedQuery = filter.SortBy switch
        {
            BookUsersSortBy.IssueDate when filter.SortDirection == BookUsersSortDirection.Descending
                => query.OrderByDescending(bookUsers => bookUsers.IssueDate),

            BookUsersSortBy.IssueDate
                => query.OrderBy(bookUsers => bookUsers.IssueDate),

            BookUsersSortBy.EstimatedReturnDate when filter.SortDirection == BookUsersSortDirection.Descending
                => query.OrderByDescending(bookUsers => bookUsers.EstimatedReturnDate),

            BookUsersSortBy.EstimatedReturnDate
                => query.OrderBy(bookUsers => bookUsers.EstimatedReturnDate),

            BookUsersSortBy.ActualReturnDate when filter.SortDirection == BookUsersSortDirection.Descending
                => query.OrderByDescending(bookUsers => bookUsers.ActualReturnDate),

            BookUsersSortBy.ActualReturnDate
                => query.OrderBy(bookUsers => bookUsers.ActualReturnDate),
            _ => query.OrderByDescending(bookUsers => bookUsers.IssueDate)
        };

        // Стабильный порядок: при одинаковых датах, рейтингах и т. п.
        IQueryable<BookUsersListItemViewModel> bookUsersQuery = orderedQuery
            .ThenByDescending(bookUsers => bookUsers.Id)
            .Select(bookUsers => new BookUsersListItemViewModel
            {
                Id = bookUsers.Id,
                BookId = bookUsers.BookId,
                UserId = bookUsers.UserId,
                IssueDate = bookUsers.IssueDate,
                EstimatedReturnDate = bookUsers.EstimatedReturnDate,
                ActualReturnDate = bookUsers.ActualReturnDate,
                BookName = bookUsers.Book.Name,
                UserEmail = bookUsers.User.Email
            });

        var pagedBooks = await PaginatedList<BookUsersListItemViewModel>
            .CreateAsync(bookUsersQuery, page, pageSize);

        var viewModel = new BookUsersIndexViewModel(pagedBooks)
        {
            Filter = filter
        };

        _logger.LogInformation(
            "[Admin Panel] Каталог загружен. Page={Page}, Count={Count}, Search={Search}, OnlyActive={OnlyActive}",
            page,
            pagedBooks.Count,
            filter.Search,
            filter.OnlyActive);

        return View(viewModel);
    }
    [HttpGet("user-loan/{id}")]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken ct = default)
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
                "[Admin Panel] Информация о долге не найдена (Id={id}).",
                id);
            return NotFound();
        }

        _logger.LogInformation(
             "[Admin Panel] Информация о долге получена (Id={id}).",
             id);
        return View(book);
    }

    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct = default)
    {
        try
        {
            await _context.BookUsers.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "[Admin Panel] Информация о долге удалена (Id={id}).",
                id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin Panel] Ошибка при удалении долга (UserBookId={id})", id);
            return StatusCode(500, "Произошла ошибка при удалении книги.");
        }
        return RedirectToAction("Index");
    }

    [HttpGet("user-loan/create")]
    public async Task<IActionResult> Create(
        CancellationToken ct = default)
    {
        return View(new BookUsersDetailsViewModel());
    }

    [HttpPost("user-loan/create")]
    public async Task<IActionResult> Create(
        BookUsersDetailsViewModel bookUsers,
        CancellationToken ct = default)
    {
        try
        {
            DateTime tmp = DateTime.Now;
            await _context.BookUsers.AddAsync(new BookUser
            {
                BookId = bookUsers.BookId,
                UserId = bookUsers.UserId,
                IssueDate = tmp,
                EstimatedReturnDate = tmp.AddDays(7),
                ActualReturnDate = null,
                Extented = false,
                Book = bookUsers.Book,
                User = bookUsers.User
            }, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning(
                    "[Admin Panel] Задолженность добавлена (Id={id}).",
                    bookUsers.Id);
        }
        catch(Exception ex)
        {
            _logger.LogWarning(
                               "[Admin Panel] Произошла ошибка (Exception={ex}).",
                               ex);
            return StatusCode(500, "Произошла ошибка при удалении книги.");
        }


        return RedirectToAction("Index");
    }

    [HttpGet("user-loan/{id}/edit")]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken ct = default)
    {
        BookUsersDetailsViewModel? bookUsers;
        bookUsers = await (
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
        if (bookUsers is null)
        {
            _logger.LogWarning(
                "[Admin Panel] Долг не найден (Id={id}).",
                id);
            return NotFound();
        }

        _logger.LogInformation(
             "[Admin Panel] Долг получен (Id={id}).",
             id);
        return View(bookUsers);
    }

    [HttpPost("user-loan/{id}/edit")]
    public async Task<IActionResult> Edit(
        BookUsersDetailsViewModel bookUsers,
        CancellationToken ct = default)
    {
        _context.BookUsers.Update(new BookUser
        {
            Id = bookUsers.Id,
            BookId = bookUsers.BookId,
            UserId = bookUsers.UserId,
            IssueDate = bookUsers.IssueDate,
            EstimatedReturnDate = bookUsers.EstimatedReturnDate,
            ActualReturnDate = bookUsers.ActualReturnDate,
            Extented = bookUsers.Extented,
            Book = bookUsers.Book,
            User = bookUsers.User
        });
        await _context.SaveChangesAsync(ct);
        _logger.LogWarning(
                "[Admin Panel] Информация о задолженности изменена (Id={id}).",
                bookUsers.Id);
        return RedirectToAction("Index");
    }
}