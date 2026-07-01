namespace LibTrack.ViewModels.Entities.BookUsers;

public class BookUsersFilter
{
    /// <summary>
    /// Поиск будет вестись по email читателя.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Только действующие задолженности
    /// </summary>
    public bool OnlyActive { get; set; }

    public BookUsersSortBy SortBy { get; set; } = BookUsersSortBy.IssueDate;

    public BookUsersSortDirection SortDirection { get; set; } =
        BookUsersSortDirection.Descending;

}
public enum BookUsersSortBy
{
    IssueDate,
    EstimatedReturnDate,
    ActualReturnDate
}
public enum BookUsersSortDirection
{
    // По убыванию
    Descending,
    // По возрастанию
    Ascending
}