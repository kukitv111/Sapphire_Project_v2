using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace Sapphire.Shared.Security;

public static class TrustedProxyHeaders
{
    public static IApplicationBuilder UseTrustedProxyHeaders(this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var addresses = configuration.GetSection("TrustedProxyIps").Get<string[]>() ?? [];
        if (addresses.Length == 0) return app;
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            ForwardLimit = 1
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var address in addresses)
        {
            if (!IPAddress.TryParse(address, out var ip))
                throw new InvalidOperationException("TrustedProxyIps must contain literal IP addresses");
            options.KnownProxies.Add(ip);
        }
        return app.UseForwardedHeaders(options);
    }
}
