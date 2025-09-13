using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Api.Admin;

public class AdminTicketsHttpTrigger
{
    private readonly ILogger<AdminTicketsHttpTrigger> _logger;
    private readonly IEventService _eventService;

    public AdminTicketsHttpTrigger(ILogger<AdminTicketsHttpTrigger> logger, IEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    }


    [OpenApiOperation(nameof(UpdateTicketTypes), tags: ["Tickets"])]
    [OpenApiRequestBody("application/json", typeof(EventTicketTypeDto), Description = "The ticket types to add/update")]
    [Function(nameof(UpdateTicketTypes))]
    public async Task<HttpResponseData> UpdateTicketTypes(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "v1/admin/tickets")]
        HttpRequestData req, string eventId, CancellationToken ct = default)
    {
        try
        {
            EventTicketTypeDto? input;
            input = await req.ReadFromJsonAsync<EventTicketTypeDto>(ct);

            if (input is null)
                return await req.BadRequestEnvelope("Body is required.", ct: ct);

            var result = await _eventService.UpsertTicketAsync(input, ct);

            if (result is null)
                return await req.BadRequestEnvelope("Ticket type not found.", ct: ct);

            return await req.OkEnvelope(result, $"Updated ticket type for event {result.EventId}.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating event");
            return await req.ServerErrorEnvelope($"Unexpected error. ", [ex.Message], ct: ct);
        }
    }

    [OpenApiOperation(nameof(CreateTicketType), tags: ["Tickets"])]
    [OpenApiRequestBody("application/json", typeof(AddEventTicketTypeDto),
        Description = "The ticket types to add/update")]
    [Function(nameof(CreateTicketType))]
    public async Task<HttpResponseData> CreateTicketType(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/admin/tickets")]
        HttpRequestData req, string eventId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating a new ticket type for an event.");
            EventTicketTypeDto? input;
            input = await req.ReadFromJsonAsync<EventTicketTypeDto>(ct);

            if (input is null)
                return await req.BadRequestEnvelope("Body is required.", ct: ct);

            var result = await _eventService.UpsertTicketAsync(input, ct);

            if (result is null)
                return await req.BadRequestEnvelope("Ticket type not found.", ct: ct);

            return await req.OkEnvelope(result, $"Created ticket type for event. {result.EventId}", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating event");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }

    [OpenApiOperation(nameof(DeleteTicketType), tags: ["Tickets"])]
    [OpenApiParameter("ticketTypeId", Required = true, Description = "The ID of the ticket type to delete")]
    [Function(nameof(DeleteTicketType))]
    public async Task<HttpResponseData> DeleteTicketType(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/admin/tickets/{ticketTypeId}")]
        HttpRequestData req,
        string ticketTypeId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Deleting ticket type {TicketTypeId}", ticketTypeId);

            if (!Guid.TryParse(ticketTypeId, out var ticketTypeGuid))
                return await req.BadRequestEnvelope("Invalid ticket type ID format.", ct: ct);

            await _eventService.DeleteTicketAsync(ticketTypeGuid, ct);
            return await req.OkEnvelope("Ticket type deleted successfully.", ct: ct);
        }
        catch (InvalidOperationException ex)
        {
            return await req.BadRequestEnvelope(ex.Message, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting ticket type");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }


    [OpenApiOperation(nameof(GetTicketTypes), tags: ["Tickets"], Description = "Get all ticket types")]
    [Function(nameof(GetTicketTypes))]
    public async Task<HttpResponseData> GetTicketTypes(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/tickets")]
        HttpRequestData req,
        CancellationToken ct = default)
    {
        try
        {
            var query = req.ToPageQuery("ticketTypeId");
            var result = await _eventService.GetTicketTypesAsync(query, ct);
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
}