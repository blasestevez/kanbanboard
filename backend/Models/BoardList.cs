namespace Trellochocero.Api.Models;

public class BoardList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; } = 0;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Board Board { get; set; } = null!;
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
