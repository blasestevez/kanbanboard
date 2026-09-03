namespace Trellochocero.Api.Models;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Position { get; set; } = 0;
    public DateTime? DueDate { get; set; }
    public bool IsComplete { get; set; } = false;
    public string? CoverColor { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public BoardList List { get; set; } = null!;
    public ICollection<CardMember> Members { get; set; } = new List<CardMember>();
    public ICollection<CardLabel> Labels { get; set; } = new List<CardLabel>();
    public ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();
    public ICollection<CardComment> Comments { get; set; } = new List<CardComment>();
    public ICollection<CardAttachment> Attachments { get; set; } = new List<CardAttachment>();
}
