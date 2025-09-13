using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketProcessor.Api.Helpers;
using TicketProcessor.Infrastructure;


namespace TicketProcessor.Api;

public class HealthCheck
{
    private readonly ILogger<HealthCheck> _logger;
    private readonly TicketingDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public HealthCheck(ILogger<HealthCheck> logger, TicketingDbContext db, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
    }

    [OpenApiOperation(nameof(HealthCheckAsync), tags: ["Health"])]
    [Function("healthheck")]
    public async Task<HttpResponseData> HealthCheckAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthheck")]
        HttpRequestData req)
    {
        var res = req.CreateResponse();

        string pg = "ok";
        string rd = "ok";
        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1;");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB check failed");
            pg = $"error: {ex.Message}";
        }

        try
        {
            var pong = await _redis.GetDatabase().PingAsync();
            rd = $"ok ({pong.TotalMilliseconds:0} ms)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis check failed");
            rd = $"error: {ex.Message}";
        }

        var result = new { pg, rd };
        return await req.OkEnvelope(result, "healthy");
    }
}