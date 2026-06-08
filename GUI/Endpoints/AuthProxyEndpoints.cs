namespace GUI.Endpoints;

/// <summary>
/// Proxies all /api/auth/* requests to the Better Auth service (http://localhost:5000).
/// This solves the Mixed Content issue when the .NET app runs on HTTPS (7065)
/// but Better Auth runs on HTTP (5000).
/// Browser calls: https://localhost:7065/api/auth/* → .NET → http://localhost:5000/api/auth/*
/// </summary>
public static class AuthProxyEndpoints
{
    public static IEndpointRouteBuilder MapAuthProxyEndpoints(this IEndpointRouteBuilder routes)
    {
        // Proxy all /api/auth/* requests (GET + POST) to Better Auth
        routes.Map("/api/auth/{**slug}", async (HttpContext ctx, IHttpClientFactory factory, string slug) =>
        {
            var client = factory.CreateClient("BetterAuth");
            var targetUrl = $"/api/auth/{slug}";

            // Forward query string if any
            if (ctx.Request.QueryString.HasValue)
                targetUrl += ctx.Request.QueryString.Value;

            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(ctx.Request.Method),
                RequestUri = new Uri(targetUrl, UriKind.Relative),
            };

            // Forward request body
            if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                requestMessage.Content = new StreamContent(ctx.Request.Body);
                if (ctx.Request.ContentType is { } ct)
                    requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", ct);
            }

            // Forward relevant headers (skip Host and Hop-by-Hop headers)
            foreach (var (key, value) in ctx.Request.Headers)
            {
                if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                if (IsHopByHopHeader(key)) continue;
                requestMessage.Headers.TryAddWithoutValidation(key, (IEnumerable<string?>)value!);
            }

            // Forward Cookie header so Better Auth can validate sessions
            if (ctx.Request.Cookies.Count > 0)
            {
                var cookies = string.Join("; ", ctx.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
                requestMessage.Headers.TryAddWithoutValidation("Cookie", cookies);
            }

            var response = await client.SendAsync(requestMessage);

            // Forward status code
            ctx.Response.StatusCode = (int)response.StatusCode;

            // Forward response headers (especially Set-Cookie from Better Auth, skip Hop-by-Hop and Content-Length)
            foreach (var (key, value) in response.Headers)
            {
                if (IsHopByHopHeader(key)) continue;
                ctx.Response.Headers.Append(key, value.ToArray());
            }
            foreach (var (key, value) in response.Content.Headers)
            {
                if (IsHopByHopHeader(key)) continue;
                if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                ctx.Response.Headers.Append(key, value.ToArray());
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            ctx.Response.ContentLength = bytes.Length;
            await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        });

        return routes;
    }

    private static bool IsHopByHopHeader(string key)
    {
        return key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("TE", StringComparison.OrdinalIgnoreCase) ||
               key.Equals("Trailers", StringComparison.OrdinalIgnoreCase);
    }
}
