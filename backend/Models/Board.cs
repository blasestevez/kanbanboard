namespace Trellochocero.Api.Models;

public class Board
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? BackgroundColor { get; set; } = "#0079bf";
    public string? BackgroundImageUrl { get; set; }
    public bool IsClosed { get; set; } = false;
    public int Position { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Workspace Workspace { get; set; } = null!;
    public ICollection<BoardList> Lists { get; set; } = new List<BoardList>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
}
