using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Api.Public;

public class PublicEventsHttpTrigger
{
    private readonly ILogger<PublicEventsHttpTrigger> _logger;
    private readonly IEventService _eventService;

    public PublicEventsHttpTrigger(ILogger<PublicEventsHttpTrigger> logger, IEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    }

    // GET /events — list upcoming
    [OpenApiOperation(nameof(ListUpcomingEvents), tags:["Events"])]
    [Function(nameof(ListUpcomingEvents))]
    public async Task<HttpResponseData> ListUpcomingEvents(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "V1/events")] HttpRequestData req, CancellationToken ct = default)
    {
        _logger.LogInformation("Listing upcoming events.");
        var qp = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        DateTimeOffset? from = DateTimeOffset.TryParse(qp["from"], out var f) ? f : null;
        DateTimeOffset? to   = DateTimeOffset.TryParse(qp["to"], out var t) ? t : null;
        Guid? venueId        = Guid.TryParse(qp["venueId"], out var v) ? v : null;
        string? search       = qp["q"];
        int page             = int.TryParse(qp["page"], out var p) ? Math.Max(1, p) : 1;
        int pageSize         = int.TryParse(qp["pageSize"], out var s) ? Math.Clamp(s, 1, 100) : 20;

        var query = new PublicEventsQuery(from, to, venueId, search, page, pageSize);

        try
        {
            var result = await _eventService.GetEventsListAsync(query, ct);
            var res = await req.OkEnvelope(result, "OK", ct);
            res.Headers.Add("X-Total-Count", result.Total.ToString());
            res.Headers.Add("X-Page", page.ToString());
            res.Headers.Add("X-Page-Size", pageSize.ToString());
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch public events");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }


    //todo - add more details to the event details
    // GET /events/{id} — include ticket types + availability snapshot
    [Function("GetEventDetails")]
    public async Task<HttpResponseData> GetEventDetails(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "V1/events/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation($"Getting details for event ID: {id}");

        // --- Your logic to fetch event details, ticket types, and availability from a database/service ---

        // For demonstration:
        bool eventFound = true; // Replace with actual check
        if (!eventFound)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var eventDetails = new
        {
            id = id,
            name = $"Sample Event {id}",
            description = "This is a sample event description.",
            date = "2025-12-01",
            location = "Virtual",
            ticketTypes = new[]
            {
                new { type = "Standard", price = 50.00, capacity = 100, available = 80 },
                new { type = "VIP", price = 150.00, capacity = 20, available = 15 }
            },
            availabilitySnapshotTime = System.DateTime.UtcNow // Current time of snapshot
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(eventDetails));
        return response;
    }

}