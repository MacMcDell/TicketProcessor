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
    [OpenApiOperation(nameof(ListUpcomingEvents), tags: ["Events"], Description = "Show upcoming events")]
    [Function(nameof(ListUpcomingEvents))]
    public async Task<HttpResponseData> ListUpcomingEvents(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "V1/events")]
        HttpRequestData req, CancellationToken ct = default)
    {
        _logger.LogInformation("Listing upcoming events.");
        var query = req.ToPageQuery("venuId");


        try
        {
            var result = await _eventService.GetEventsListAsync(query, ct);
            var res = await req.OkEnvelope(result, "OK", ct);
            res.Headers.Add("X-Total-Count", result.Total.ToString());
            res.Headers.Add("X-Page", query.Page.ToString());
            res.Headers.Add("X-Page-Size", query.PageSize.ToString());
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch public events");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }


    // GET /events/{id} — include ticket types + availability snapshot
    [OpenApiOperation(nameof(GetEventDetails), tags: ["Events"], Description = "Get details for a specific event")]
    [OpenApiParameter("id", Required = true, Description = "The event ID")]
    [Function("GetEventDetails")]
    public async Task<HttpResponseData> GetEventDetails(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "V1/events/{id}")]
        HttpRequestData req,
        string id,
        CancellationToken ct = default)
    {
        _logger.LogInformation($"Getting details for event ID: {id}");

        if (!Guid.TryParse(id, out var eventId))
        {
            return await req.BadRequestEnvelope("Invalid event ID format.", ct: ct);
        }

        try
        {
            var result = await _eventService.GetEventDetailsAsync(eventId, ct);
            if (result == null)
            {
                return await req.NotFoundEnvelope("Event not found.", ct: ct);
            }

            return await req.OkEnvelope(result, "OK", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch event details");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }
}