using ECommerce.Payment.Models;

namespace ECommerce.Payment.Contracts;

public sealed record PaymentDto(Guid Id, Guid OrderId, decimal Amount, string Currency, PaymentStatus Status);
