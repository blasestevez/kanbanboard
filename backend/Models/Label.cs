namespace Trellochocero.Api.Models;

public class Label
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#61bd4f";

    // Navigation properties
    public Board Board { get; set; } = null!;
    public ICollection<CardLabel> CardLabels { get; set; } = new List<CardLabel>();
}
