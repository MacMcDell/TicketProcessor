using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain;

namespace TicketProcessor.Api.Admin;

//TODO add authorization here
public class AdminEventsHttpTrigger
{
    private readonly ILogger<AdminEventsHttpTrigger> _logger;
    private readonly IEventService _eventService;
    private readonly IValidator<CreateEventDto> _validator;

    //todo add jsonSerializerOptions to program.cs
    public AdminEventsHttpTrigger(ILogger<AdminEventsHttpTrigger> logger, IEventService eventService, IValidator<CreateEventDto> validator)
    {
        _logger = logger;
        _eventService = eventService;
        _validator = validator;
    }

    // POST /events — create event (+ ticket types)
   
    [OpenApiOperation(nameof(CreateEvent),tags: ["Events"] )]
    [OpenApiRequestBody("application/json", typeof(CreateEventDto), Description = "The event to create - use venue name and capacity to create new venue. " +
        "Use Id to use same venue")]
    [Function("CreateEvent")]
    public async Task<HttpResponseData> CreateEvent(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/admin/events")] HttpRequestData req,  CancellationToken ct)
    {
        _logger.LogInformation("Creating a new event.");
        CreateEventDto? input;
        try
        {
            input = await req.ReadFromJsonAsync<CreateEventDto>( ct);
            if (input is null)
                return await req.BadRequestEnvelope("Body is required.", ct: ct);
        }
        catch
        {
            return await req.BadRequestEnvelope("Invalid JSON.", ct: ct);
        }

        var validation = await _validator.ValidateAsync(input, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
            return await req.BadRequestEnvelope("Validation failed", errors, ct);
        }

        try
        {
            var created = await _eventService.CreateEventAsync(input, ct);
            var res = await req.CreatedEnvelope(created, "Event created successfully", ct);
            res.Headers.Add("Location", $"/api/admin/events/{created.Id}");
            return res;
        }
        catch (InvalidOperationException ioex)
        {
            return await req.BadRequestEnvelope(ioex.Message, ct: ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await req.ConflictEnvelope("Concurrency conflict.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating event");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }
    
    [OpenApiOperation(nameof(UpdateEvent),tags: ["Events"] )]
    [OpenApiRequestBody("application/json", typeof(EventDto), Description = "The event to update")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(EventDto), Description = "The updated event")]
    [Function(nameof(UpdateEvent))]
    public async Task<HttpResponseData> UpdateEvent(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "v1/admin/events")] HttpRequestData req, CancellationToken ct)
    {
        EventDto? input; 
        input = await req.ReadFromJsonAsync<EventDto>( ct);
        
        if (input is null)
            return await req.BadRequestEnvelope("Body is required.", ct: ct);
        try
        {
            var result = await _eventService.UpdateEventAsync(input, ct);

            if (result is null)
                return await req.BadRequestEnvelope("Ticket type not found.", ct: ct);

            return await req.OkEnvelope(result, $"Updated event {result.Id}.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating event");
            return await req.ServerErrorEnvelope($"Unexpected error. ",[ex.Message], ct: ct);
        }
    }
    
    [OpenApiOperation(nameof(DeleteEvent), tags: ["Events"])]
    [OpenApiParameter("eventId", Required = true, Description = "The ID of the event to delete")]
    [Function(nameof(DeleteEvent))]
    public async Task<HttpResponseData> DeleteEvent(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/admin/events/{eventId}")] HttpRequestData req,
        string eventId,
        CancellationToken ct)
    {
        var eventIsGuid = Guid.TryParse(eventId, out var eventGuid);
        if (!eventIsGuid)
            return await req.BadRequestEnvelope("Invalid event ID format.", ct: ct);

        try
        {
            await _eventService.DeleteEventAsync(eventGuid, ct);
            return await req.OkEnvelope("Event deleted successfully.", ct: ct);
        }
        catch (InvalidOperationException ex)
        {
            return await req.BadRequestEnvelope(ex.Message, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting event");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }

    
    
}