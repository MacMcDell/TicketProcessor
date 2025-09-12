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
    [Function("GetReservationDetails")]
    public async Task<HttpResponseData> GetReservationDetails(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/reservations/{reservationId}")] HttpRequestData req,
        string reservationId)
    {
        _logger.LogInformation($"Getting details for reservation ID: {reservationId}");

        // --- Your logic to fetch reservation details from a database/service ---

        // For demonstration purposes:
        bool reservationFound = true; // Replace with actual check
        if (!reservationFound)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        var reservationDetails = new
        {
            reservationId = reservationId,
            eventId = "sample-event-123", // Example event ID
            customerRef = "customer-abc-123",
            status = "PendingPayment", // e.g., PendingPayment, Confirmed, Cancelled, Expired
            items = new[]
            {
                new { ticketTypeId = "type-a", quantity = 2 },
                new { ticketTypeId = "type-b", quantity = 1 }
            },
            reservationTimeUtc = DateTime.UtcNow.AddHours(-1),
            holdsUntilUtc = DateTime.UtcNow.AddMinutes(5)
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(reservationDetails));
        return response;
    }

    //todo need to imlement.
    [Function("CancelReservation")]
    public async Task<HttpResponseData> CancelReservation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/reservations/{reservationId}/cancel")] HttpRequestData req,
        string reservationId)
    {
        _logger.LogInformation($"Cancelling reservation ID: {reservationId}");

        // --- Your logic to cancel the reservation ---
        // - Mark reservation as cancelled in your database
        // - Release held tickets
        // - Handle refunds if applicable (not in scope of this function but conceptual)

        // For demonstration purposes:
        bool reservationExists = true; // Replace with actual check
        if (!reservationExists)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        bool cancelSuccessful = true; // Replace with actual cancellation logic result
        if (cancelSuccessful)
        {
            return req.CreateResponse(HttpStatusCode.NoContent); // 204 No Content for successful cancellation
        }
        else
        {
            // Handle cases where cancellation might fail (e.g., already confirmed, invalid state)
            var errorResponse = req.CreateResponse(HttpStatusCode.Conflict); // Or another appropriate error
            await errorResponse.WriteStringAsync("Reservation could not be cancelled in its current state.");
            return errorResponse;
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