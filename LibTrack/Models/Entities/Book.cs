using System;
using System.Collections.Generic;

namespace LibTrack.Models.Entities;

public partial class Book
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Author { get; set; } = null!;

    public short PublicationYear { get; set; }

    public int GenreId { get; set; }

    public string? Image { get; set; }

    public virtual ICollection<BookUser> BookUsers { get; set; } = new List<BookUser>();

    public virtual Genre Genre { get; set; } = null!;
}
