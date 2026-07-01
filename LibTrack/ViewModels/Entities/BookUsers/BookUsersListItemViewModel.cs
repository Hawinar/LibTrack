using LibTrack.Models.Entities;

namespace LibTrack.ViewModels.Entities.BookUsers
{
    public class BookUsersListItemViewModel
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public string BookName { get; set; } = null!;

        public int UserId { get; set; }

        public string UserEmail { get; set; } = null!;

        public DateTime IssueDate { get; set; }

        public DateTime EstimatedReturnDate { get; set; }

        public DateTime? ActualReturnDate { get; set; }
    }
}
