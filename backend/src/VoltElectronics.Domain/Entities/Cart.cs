namespace VoltElectronics.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
