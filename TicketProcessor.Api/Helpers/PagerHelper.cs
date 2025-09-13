using Microsoft.Azure.Functions.Worker.Http;
using TicketProcessor.Domain;

namespace TicketProcessor.Api.Helpers;

public static class PagerHelper
{
    public static PageQuery ToPageQuery(this HttpRequestData req, string entityId)
    {
        var qp = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        DateTimeOffset? from = DateTimeOffset.TryParse(qp["from"], out var f) ? f : null;
        DateTimeOffset? to = DateTimeOffset.TryParse(qp["to"], out var t) ? t : null;
        Guid? id = Guid.TryParse(qp[entityId], out var v) ? v : null;
        string? search = qp["q"];
        int page = int.TryParse(qp["page"], out var p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(qp["pageSize"], out var s) ? Math.Clamp(s, 1, 100) : 20;

        var query = new PageQuery(from, to, id, search, page, pageSize);
        return query;
    }
}