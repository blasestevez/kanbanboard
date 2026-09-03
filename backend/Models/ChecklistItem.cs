namespace Trellochocero.Api.Models;

public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChecklistId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; } = false;
    public int Position { get; set; } = 0;

    // Navigation properties
    public Checklist Checklist { get; set; } = null!;
}
