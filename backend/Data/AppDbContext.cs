using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Trellochocero.Api.Models;

namespace Trellochocero.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardList> BoardLists => Set<BoardList>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<CardLabel> CardLabels => Set<CardLabel>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<CardComment> CardComments => Set<CardComment>();
    public DbSet<CardAttachment> CardAttachments => Set<CardAttachment>();
    public DbSet<CardMember> CardMembers => Set<CardMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.AvatarUrl)
                .HasMaxLength(500);
        });

        builder.Entity<Workspace>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(w => w.Description)
                .HasMaxLength(500);

            entity.Property(w => w.LogoUrl)
                .HasMaxLength(500);

            entity.HasOne(w => w.Owner)
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(w => w.Members)
                .WithOne(wm => wm.Workspace)
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(wm => new { wm.WorkspaceId, wm.UserId });

            entity.Property(wm => wm.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.HasOne(wm => wm.User)
                .WithMany()
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Board>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(b => b.BackgroundColor)
                .HasMaxLength(50);

            entity.Property(b => b.BackgroundImageUrl)
                .HasMaxLength(500);

            entity.HasOne(b => b.Workspace)
                .WithMany()
                .HasForeignKey(b => b.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.Lists)
                .WithOne(l => l.Board)
                .HasForeignKey(l => l.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.Labels)
                .WithOne(l => l.Board)
                .HasForeignKey(l => l.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(b => b.WorkspaceId);
        });

        builder.Entity<BoardList>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(l => l.Board)
                .WithMany(b => b.Lists)
                .HasForeignKey(l => l.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(l => l.Cards)
                .WithOne(c => c.List)
                .HasForeignKey(c => c.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.BoardId);
        });

        builder.Entity<Card>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(c => c.Description)
                .HasMaxLength(5000);

            entity.Property(c => c.CoverColor)
                .HasMaxLength(50);

            entity.Property(c => c.CoverImageUrl)
                .HasMaxLength(500);

            entity.HasOne(c => c.List)
                .WithMany(l => l.Cards)
                .HasForeignKey(c => c.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Members)
                .WithOne(cm => cm.Card)
                .HasForeignKey(cm => cm.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Labels)
                .WithOne(cl => cl.Card)
                .HasForeignKey(cl => cl.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Checklists)
                .WithOne(ch => ch.Card)
                .HasForeignKey(ch => ch.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Comments)
                .WithOne(cc => cc.Card)
                .HasForeignKey(cc => cc.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Attachments)
                .WithOne(ca => ca.Card)
                .HasForeignKey(ca => ca.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.ListId);
        });

        builder.Entity<Label>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(l => l.Color)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(l => l.Board)
                .WithMany(b => b.Labels)
                .HasForeignKey(l => l.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(l => l.BoardId);
        });

        builder.Entity<CardLabel>(entity =>
        {
            entity.HasKey(cl => new { cl.CardId, cl.LabelId });

            entity.HasOne(cl => cl.Label)
                .WithMany(l => l.CardLabels)
                .HasForeignKey(cl => cl.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CardMember>(entity =>
        {
            entity.HasKey(cm => new { cm.CardId, cm.UserId });

            entity.HasOne(cm => cm.User)
                .WithMany()
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Checklist>(entity =>
        {
            entity.HasKey(ch => ch.Id);

            entity.Property(ch => ch.Title)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasMany(ch => ch.Items)
                .WithOne(i => i.Checklist)
                .HasForeignKey(i => i.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ch => ch.CardId);
        });

        builder.Entity<ChecklistItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Text)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(i => i.ChecklistId);
        });

        builder.Entity<CardComment>(entity =>
        {
            entity.HasKey(cc => cc.Id);

            entity.Property(cc => cc.Text)
                .HasMaxLength(2000)
                .IsRequired();

            entity.HasOne(cc => cc.Author)
                .WithMany()
                .HasForeignKey(cc => cc.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(cc => cc.CardId);
        });

        builder.Entity<CardAttachment>(entity =>
        {
            entity.HasKey(ca => ca.Id);

            entity.Property(ca => ca.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(ca => ca.FileUrl)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(ca => ca.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(ca => ca.UploadedBy)
                .WithMany()
                .HasForeignKey(ca => ca.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(ca => ca.CardId);
        });
    }
}
