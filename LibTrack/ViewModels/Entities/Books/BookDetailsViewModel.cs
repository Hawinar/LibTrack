namespace LibTrack.ViewModels.Entities.Books
{
    public class BookDetailsViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Author { get; set; } = null!;
        
        public string? Description { get; set; }

        public string? Image { get; set; }

        public string Genre { get; set; } = null!;

        public DateTime Date { get; set; }
    }
}
