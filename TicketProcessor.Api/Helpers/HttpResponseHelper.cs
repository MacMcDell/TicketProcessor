using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using TicketProcessor.Domain.Response;

namespace TicketProcessor.Api.Helpers;

public static class HttpResponseHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static async Task<HttpResponseData> WriteEnvelopeAsync<T>(
        HttpRequestData req,
        HttpStatusCode status,
        ApiResponse<T> body,
        CancellationToken ct = default)
    {
        var res = req.CreateResponse(status);
        res.Headers.Add("Content-Type", "application/json; charset=utf-8");

        // Manually serialize to avoid WriteAsJsonAsync overload issues
        var json = JsonSerializer.Serialize(body, JsonOpts);
        await res.WriteStringAsync(json, ct);

        return res;
    }
    public static Task<HttpResponseData> OkEnvelope<T>(this HttpRequestData req, T data, string? message = null, CancellationToken ct = default)
        => WriteEnvelopeAsync(req, HttpStatusCode.OK, ApiResponse<T>.Ok(data, message), ct);

    public static Task<HttpResponseData> CreatedEnvelope<T>(this HttpRequestData req, T data, string? message = null, CancellationToken ct = default)
        => WriteEnvelopeAsync(req, HttpStatusCode.Created, ApiResponse<T>.Ok(data, message), ct);
    
    public static Task<HttpResponseData> ErrorEnvelope(this HttpRequestData req, HttpStatusCode status, string message, IEnumerable<string>? errors = null, CancellationToken ct = default)
        => WriteEnvelopeAsync<object?>(req, status, ApiResponse<object?>.Fail(message, errors ?? Array.Empty<string>()), ct);

    // Convenience shortcuts
    public static Task<HttpResponseData> BadRequestEnvelope(this HttpRequestData req, string message, IEnumerable<string>? errors = null, CancellationToken ct = default)
        => req.ErrorEnvelope(HttpStatusCode.BadRequest, message, errors, ct);

    public static Task<HttpResponseData> NotFoundEnvelope(this HttpRequestData req, string message, IEnumerable<string>? errors = null, CancellationToken ct = default)
        => req.ErrorEnvelope(HttpStatusCode.NotFound, message, errors, ct);

    public static Task<HttpResponseData> ConflictEnvelope(this HttpRequestData req, string message, IEnumerable<string>? errors = null, CancellationToken ct = default)
        => req.ErrorEnvelope(HttpStatusCode.Conflict, message, errors, ct);

    public static Task<HttpResponseData> ServerErrorEnvelope(this HttpRequestData req, string message, IEnumerable<string>? errors = null, CancellationToken ct = default)
        => req.ErrorEnvelope(HttpStatusCode.InternalServerError, message, errors, ct);
}