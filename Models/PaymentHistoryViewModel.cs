namespace TeslaMAMA.Models;

public class PaymentHistoryViewModel
{
    public string FullName { get; set; } = string.Empty;
    public List<PaymentRecord> Payments { get; set; } = new();
    public decimal TotalSpent { get; set; }
}

public class PaymentRecord
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public string PickupZone { get; set; } = string.Empty;
    public string DestinationZone { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
