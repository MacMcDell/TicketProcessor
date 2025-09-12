using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Application.Interfaces;

public interface IPaymentGateway
{
    Task<string> ChargeAsync(Request.PaymentProcessorRequestDto payload, CancellationToken ct);
}