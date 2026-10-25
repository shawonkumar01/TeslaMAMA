using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeslaMAMA.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RideRequestId { get; set; }

    [ForeignKey(nameof(RideRequestId))]
    public RideRequest? RideRequest { get; set; }

    [Required]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    [Required]
    public PaymentStatus Status { get; set; }

    public string? TransactionId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? Notes { get; set; }
}
