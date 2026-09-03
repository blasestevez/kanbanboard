namespace Trellochocero.Api.Models;

public class CardLabel
{
    public Guid CardId { get; set; }
    public Guid LabelId { get; set; }

    // Navigation properties
    public Card Card { get; set; } = null!;
    public Label Label { get; set; } = null!;
}
