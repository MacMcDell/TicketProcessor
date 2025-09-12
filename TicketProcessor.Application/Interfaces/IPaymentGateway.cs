using TicketProcessor.Domain;

namespace TicketProcessor.Application.Interfaces;

public interface IPaymentGateway
{
    Task<string> ChargeAsync(PaymentProcessorRequestDto payload, CancellationToken ct);
}