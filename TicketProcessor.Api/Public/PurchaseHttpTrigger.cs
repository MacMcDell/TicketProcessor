using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Api.Public;

public class PublicPurchaseHttpTrigger
{
    private readonly ILogger<PublicPurchaseHttpTrigger> _logger;
    private readonly IEventService _eventService;

    public PublicPurchaseHttpTrigger(ILogger<PublicPurchaseHttpTrigger> logger, IEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    }

    // POST /purchases
    [OpenApiOperation(nameof(PurchaseTickets), tags: ["Purchases"])]
    [OpenApiRequestBody("application/json", typeof(PurchaseRequestDto), Description = "The purchase to process")]
    [Function("PurchaseTickets")]
    public async Task<HttpResponseData> PurchaseTickets(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/purchases")]
        HttpRequestData req, CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting to process ticket purchase.");
        PurchaseRequestDto? input;
        try
        {
            input = await req.ReadFromJsonAsync<PurchaseRequestDto>(ct);
            if (input is null) return await req.BadRequestEnvelope("Body is required.", ct: ct);
        }
        catch
        {
            return await req.BadRequestEnvelope("Invalid JSON.", ct: ct);
        }

        try
        {
            var result = await _eventService.PurchaseAsync(input, ct);
            return await req.CreatedEnvelope(result, "Purchase completed.", ct);
        }
        catch (InvalidOperationException ex)
        {
            // reservation not found/expired/not pending/capacity exceeded/payment declined
            return await req.BadRequestEnvelope(ex.Message, ct: ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await req.ConflictEnvelope("Concurrency conflict while finalizing purchase.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during purchase");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }
}