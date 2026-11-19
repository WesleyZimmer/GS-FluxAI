using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace FluxAI.Api.Security;

internal static class FluxAiAuthentication
{
    public const string Scheme = "FluxAI.Bearer";
    public const string OperatorPolicy = "FluxAiOperator";
    public const string WorkerPolicy = "FluxAiWorker";
    public const string InternalReadPolicy = "FluxAiInternalRead";
}

internal sealed class FluxAiBearerHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public FluxAiBearerHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerValue = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(headerValue))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(headerValue, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
            return Task.FromResult(AuthenticateResult.Fail("Use Authorization: Bearer <token>."));

        var presentedToken = Encoding.UTF8.GetBytes(header.Parameter);
        foreach (var credential in new[]
        {
            (Role: "operator", Token: _configuration["Authentication:OperatorToken"]),
            (Role: "worker", Token: _configuration["Authentication:WorkerToken"])
        })
        {
            if (string.IsNullOrEmpty(credential.Token))
                continue;

            var expectedToken = Encoding.UTF8.GetBytes(credential.Token);
            if (presentedToken.Length != expectedToken.Length ||
                !CryptographicOperations.FixedTimeEquals(presentedToken, expectedToken))
                continue;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, credential.Role),
                new Claim(ClaimTypes.Role, credential.Role)
            };
            var identity = new ClaimsIdentity(claims, FluxAiAuthentication.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, FluxAiAuthentication.Scheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.Fail("Bearer token is invalid."));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = "Bearer";
        Response.ContentType = "application/problem+json";
        return Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.2",
            Title = "Autenticação necessária",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Informe uma credencial Bearer válida."
        }, options: null, contentType: "application/problem+json", cancellationToken: Context.RequestAborted);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/problem+json";
        return Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4",
            Title = "Permissão insuficiente",
            Status = StatusCodes.Status403Forbidden,
            Detail = "A credencial é válida, mas não tem a função exigida para esta operação."
        }, options: null, contentType: "application/problem+json", cancellationToken: Context.RequestAborted);
    }
}
