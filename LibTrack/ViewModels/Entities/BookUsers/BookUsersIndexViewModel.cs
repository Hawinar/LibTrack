using LibTrack.Models;
using LibTrack.ViewModels.Entities.BookUsers;

namespace LibTrack.ViewModels.Entities.Books
{
    public class BookUsersIndexViewModel
    {
        public BookUsersIndexViewModel(PaginatedList<BookUsersListItemViewModel> items)
        {
            Items = items;
        }

        public PaginatedList<BookUsersListItemViewModel> Items { get; set; } = default!;
        public BookUsersFilter Filter { get; set; } = new();
    }
}
