using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain.Requests;

namespace TicketProcessor.Api.Public;

public class PublicReservationsHttpTrigger
{
    private ILogger<PublicReservationsHttpTrigger> _logger;
    private readonly IEventService _eventService;

    public PublicReservationsHttpTrigger(ILogger<PublicReservationsHttpTrigger> logger, IEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
    }
    
    // POST /events/{id}/reservations
    [OpenApiOperation(nameof(CreateReservation))]
    [OpenApiRequestBody("application/json", typeof(Request.CreateReservationRequestDto), Description = "The reservation to create")]
    [Function("Public_CreateReservation")]
    public async Task<HttpResponseData> CreateReservation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/reservations")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        Request.CreateReservationRequestDto? input;
        try
        {
            input = await req.ReadFromJsonAsync<Request.CreateReservationRequestDto>(ct);
            if (input is null) return await req.BadRequestEnvelope("Body is required.", ct: ct);
        }
        catch
        {
            return await req.BadRequestEnvelope("Invalid JSON.", ct: ct);
        }

        try
        {
            var result = await _eventService.CreateReservationAsync(input, ct);
            return await req.CreatedEnvelope(result, "Reservation created.", ct);
        }
        catch (InvalidOperationException ex)
        {
            // For “not found”, “not enough tickets”, “duplicate idempotency key”, “invalid qty”
            return await req.BadRequestEnvelope(ex.Message, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating reservation");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }

    // GET /reservations/{id}
    //todo return reservation details
    [Function("GetReservationDetails")]
    public async Task<HttpResponseData> GetReservationDetails(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/reservations/{reservationId}")] HttpRequestData req,
        string reservationId)
    {
        _logger.LogInformation($"Getting details for reservation ID: {reservationId}");
        return req.CreateResponse(HttpStatusCode.OK);

    }

    [Function("CancelReservation")]
    [OpenApiOperation(nameof(CancelReservation))]
    [OpenApiParameter("reservationId", Required = true, Description = "the reservation id of the reservation to cancel")]
    public async Task<HttpResponseData> CancelReservation(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/reservations/{reservationId}/cancel")] HttpRequestData req, string reservationId, CancellationToken ct = default)
    {
        _logger.LogInformation($"Cancelling reservation ID: {reservationId}");
        var reservationIsGuid = Guid.TryParse(reservationId, out var reservationGuid);
       
        if (!reservationIsGuid) 
            return await req.BadRequestEnvelope("ReservationId required.", ct: ct);
        try
        {
            await _eventService.DeleteReservationAsync(reservationGuid,ct);
            return await req.OkEnvelope("Reservation deleted.", ct: ct);
        }
        catch (InvalidOperationException ex)
        {
            return await req.BadRequestEnvelope(ex.Message, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating reservation");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }
}



    

// Data Transfer Objects (DTOs) for the reservation request body
// You would typically define these in separate files/folders (e.g., Models/DTOs)
public class ReservationRequest
{
    public List<ReservationItem> Items { get; set; } = [];
    public string CustomerRef { get; set; } = string.Empty;
}

public class ReservationItem
{
    public string TicketTypeId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}