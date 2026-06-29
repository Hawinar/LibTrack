namespace LibTrack.Models.Entities;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public virtual ICollection<BookUser> BookUsers { get; set; } = new List<BookUser>();

    public virtual Role? Role { get; set; }
}
