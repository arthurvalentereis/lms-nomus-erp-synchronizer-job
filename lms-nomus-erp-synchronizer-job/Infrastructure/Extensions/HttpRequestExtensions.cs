using Microsoft.AspNetCore.Http;
using System.Net;

namespace lms_nomus_erp_synchronizer_job.Infrastructure.Extensions;

public static class HttpRequestExtensions
{
    public static bool IsLocal(this HttpRequest request)
    {
        var connection = request.HttpContext.Connection;

        if (connection.RemoteIpAddress is null)
            return true;

        if (IPAddress.IsLoopback(connection.RemoteIpAddress))
            return true;

        if (connection.LocalIpAddress is not null)
            return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);

        return false;
    }
}