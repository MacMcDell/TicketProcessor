using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Infrastructure.PaymentProcessors;

public class FakePaymentProcessor : IPaymentGateway
{
    private readonly HttpClient _client;

    public FakePaymentProcessor(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> ChargeAsync(PaymentProcessorRequestDto payload, CancellationToken ct)
    {
        using var res = await _client.PostAsJsonAsync("https://httpbin.org/post", payload, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Payment failed: {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);

        return json;
    }
}