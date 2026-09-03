namespace Trellochocero.Api.Models;

public class Checklist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; } = 0;

    // Navigation properties
    public Card Card { get; set; } = null!;
    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}
