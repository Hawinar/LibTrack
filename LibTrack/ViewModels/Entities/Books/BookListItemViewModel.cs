namespace LibTrack.ViewModels.Entities.Books
{
    public class BookListItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? ImagePath { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string Genre { get; set; } = null!;

        public DateTime Date { get; set; }
    }
}
