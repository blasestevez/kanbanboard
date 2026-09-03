namespace Trellochocero.Api.Models;

public class CardMember
{
    public Guid CardId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public Card Card { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
