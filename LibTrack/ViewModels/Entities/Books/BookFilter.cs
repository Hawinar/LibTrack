namespace LibTrack.ViewModels.Entities.Books;

public class BookFilter
{
    /// <summary>
    /// Поиск будет вести и по названию книги, и по имени автора.
    /// </summary>
    public string? Search { get; set; }

    public int? GenreId { get; set; }

    public bool OnlyAvailable { get; set; }

    public BookSortBy SortBy { get; set; } = BookSortBy.Popularity;

    public BookSortDirection SortDirection { get; set; } =
        BookSortDirection.Descending;

}
public enum BookSortBy
{
    // По популярности (кол-во записей в таблице BookUser у конкретной книги)
    Popularity,
    PublicationYear,
    AddDate,
    UpdateDate
}
public enum BookSortDirection
{
    // По убыванию
    Descending,
    // По возрастанию
    Ascending
}