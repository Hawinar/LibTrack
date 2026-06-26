using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LibTrack.Models.Entities;

public partial class LibTrackDbContext : DbContext
{
    public LibTrackDbContext()
    {
    }

    public LibTrackDbContext(DbContextOptions<LibTrackDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BookUser> BookUsers { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;User Id=sa;Password=Your_Password123!;Database=LibTrackDB;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Book");

            entity.Property(e => e.Author).HasMaxLength(50);
            entity.Property(e => e.Image).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Genre).WithMany(p => p.Books)
                .HasForeignKey(d => d.GenreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Book_Genre");
        });

        modelBuilder.Entity<BookUser>(entity =>
        {
            entity.ToTable("BookUser");

            entity.Property(e => e.ActualReturnDate).HasColumnType("datetime");
            entity.Property(e => e.EstimatedReturnDate).HasColumnType("datetime");
            entity.Property(e => e.IssueDate).HasColumnType("datetime");

            entity.HasOne(d => d.Book).WithMany(p => p.BookUsers)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookUser_Book");

            entity.HasOne(d => d.User).WithMany(p => p.BookUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookUser_User");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genre");

            entity.Property(e => e.Name).HasMaxLength(32);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.Name).HasMaxLength(32);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FullName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_User_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
