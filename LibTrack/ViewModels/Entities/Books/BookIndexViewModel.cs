using LibTrack.Models;

namespace LibTrack.ViewModels.Entities.Books
{
    public class BookIndexViewModel
    {
        public BookIndexViewModel(PaginatedList<BookListItemViewModel> items)
        {
            Items = items;
        }

        public PaginatedList<BookListItemViewModel> Items { get; set; } = default!;
        public BookFilter Filter { get; set; } = new();

    }
}
