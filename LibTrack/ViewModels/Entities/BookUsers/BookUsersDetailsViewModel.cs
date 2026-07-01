using LibTrack.Models.Entities;

namespace LibTrack.ViewModels.Entities.BookUsers
{
    public class BookUsersDetailsViewModel
    {
        public int Id { get; set; }

        public int BookId { get; set; }

        public int UserId { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime EstimatedReturnDate { get; set; }

        public DateTime? ActualReturnDate { get; set; }

        public bool Extented { get; set; }

        public virtual Book Book { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}
