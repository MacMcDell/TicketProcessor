using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Application.Interfaces;
using TicketProcessor.Domain.Dto;
using TicketProcessor.Domain.Requests;


namespace TicketProcessor.Api.Admin;

//TODO add authorization here
public class AdminVenueHttpTrigger
{
    private readonly ILogger<AdminVenueHttpTrigger> _logger;
    private readonly IVenueService _venueService;
    private readonly IValidator<Request.CreateVenueDto> _validator;

    public AdminVenueHttpTrigger(ILogger<AdminVenueHttpTrigger> logger, IVenueService venueService, IValidator<Request.CreateVenueDto> validator)
    {
        _logger = logger;
        _venueService = venueService;
        _validator = validator;
    }
    
    [OpenApiOperation(nameof(CreateVenue))]
    [OpenApiRequestBody("application/json", typeof(Request.CreateVenueDto), Description = "The venue to create")]
    [Function(nameof(CreateVenue))]
    public async Task<HttpResponseData> CreateVenue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/admin/venues")] HttpRequestData req, CancellationToken ct)
    {
        _logger.LogInformation("Creating a new venue.");
        Request.CreateVenueDto? input;
        try
        {
            input = await req.ReadFromJsonAsync<Request.CreateVenueDto>( ct);
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
            var created = await _venueService.CreateVenueAsync(input, ct);
            // Optionally add Location header:
            var res = await req.CreatedEnvelope(created, "Venue created successfully", ct);
            res.Headers.Add("Location", $"/api/admin/events/{created.Id}");
            return res;
        }
        catch (InvalidOperationException ioex)
        {
            return await req.BadRequestEnvelope(ioex.Message, ct: ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return await req.ConflictEnvelope("Concurrency conflict.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating venue");
            return await req.ServerErrorEnvelope("Unexpected error.", ct: ct);
        }
    }

    // PUT /events/{id} — update event metadata (with If-Match ETag)
    [OpenApiOperation(nameof(UpdateVenue))]
    [OpenApiRequestBody("application/json", typeof(VenueDto), Description = "The venue to create")]
    [Function("UpdateVenue")]
    public async Task<HttpResponseData> UpdateVenue(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "v1/admin/venues")] HttpRequestData req,CancellationToken ct)
    {
       
        VenueDto? input; 
        input = await req.ReadFromJsonAsync<VenueDto>( ct);
        
        if (input is null)
            return await req.BadRequestEnvelope("Body is required.", ct: ct);
        try
        {
         
            var result = await _venueService.UpdateVenueAsync(input, ct);

            if (result is null)
                return await req.BadRequestEnvelope("Ticket type not found.", ct: ct);

            return await req.OkEnvelope(result, $"Updated venue {result}.", ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating event");
            return await req.ServerErrorEnvelope($"Unexpected error. ",[ex.Message], ct: ct);
        }
    }

  

}