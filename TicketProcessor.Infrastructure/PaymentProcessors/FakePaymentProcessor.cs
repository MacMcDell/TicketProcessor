using System.Text.Json;
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

    /// <summary>
    /// returns the token of the transaction from the payload.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> ChargeAsync(PaymentProcessorRequestDto payload, CancellationToken ct)
    {
        using var res = await _client.PostAsJsonAsync("post", payload, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Payment failed: {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);
        
        string? paymentToken = null;
        using (JsonDocument doc = JsonDocument.Parse(json))
        {
            if (doc.RootElement.TryGetProperty("headers", out JsonElement jsonProperty))
            {
                if (jsonProperty.TryGetProperty("Traceparent", out JsonElement paymentTokenProperty))
                {
                    paymentToken = paymentTokenProperty.GetString();
                }
            }
        }
        return paymentToken ?? throw new InvalidOperationException("Payment failed");
    }
}