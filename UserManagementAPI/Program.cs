using System.Collections.Concurrent;
using System.Net.Mail;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();

string? apiToken = builder.Configuration["ApiToken"];
if (string.IsNullOrWhiteSpace(apiToken))
{
    throw new InvalidOperationException(
        "Set the ApiToken configuration value before starting the API.");
}

var app = builder.Build();
var users = new ConcurrentDictionary<int, User>();
int nextId = 0;

// Required pipeline order: errors first, authentication next, request logging last.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TokenAuthenticationMiddleware>(apiToken);
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapGet("/api/users", () =>
    Results.Ok(users.Values.OrderBy(user => user.Id)));

app.MapGet("/api/users/{id:int}", (int id) =>
{
    return users.TryGetValue(id, out User? user)
        ? Results.Ok(user)
        : Results.NotFound(new { error = $"User with ID {id} was not found." });
})
.WithName("GetUserById");

app.MapPost("/api/users", (UserRequest request) =>
{
    if (!UserValidation.TryValidate(request, out Dictionary<string, string[]> errors))
    {
        return Results.ValidationProblem(errors);
    }

    int id = Interlocked.Increment(ref nextId);
    var user = new User(id, request.Name!.Trim(), request.Email!.Trim());
    users[id] = user;

    return Results.CreatedAtRoute("GetUserById", new { id }, user);
});

app.MapPut("/api/users/{id:int}", (int id, UserRequest request) =>
{
    if (!UserValidation.TryValidate(request, out Dictionary<string, string[]> errors))
    {
        return Results.ValidationProblem(errors);
    }

    if (!users.ContainsKey(id))
    {
        return Results.NotFound(new { error = $"User with ID {id} was not found." });
    }

    var updatedUser = new User(id, request.Name!.Trim(), request.Email!.Trim());
    users[id] = updatedUser;
    return Results.Ok(updatedUser);
});

app.MapDelete("/api/users/{id:int}", (int id) =>
{
    return users.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound(new { error = $"User with ID {id} was not found." });
});

app.Run();

sealed class UserRequest
{
    public string? Name { get; init; }
    public string? Email { get; init; }
}

sealed record User(int Id, string Name, string Email);

static class UserValidation
{
    public static bool TryValidate(
        UserRequest request,
        out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else
        {
            try
            {
                _ = new MailAddress(request.Email);
            }
            catch (FormatException)
            {
                errors["email"] = ["Enter a valid email address."];
            }
        }

        return errors.Count == 0;
    }
}

sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal server error."
            });
        }
    }
}

sealed class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly string _apiToken;

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<TokenAuthenticationMiddleware> logger,
        string apiToken)
    {
        _next = next;
        _logger = logger;
        _apiToken = apiToken;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string expectedHeader = $"Bearer {_apiToken}";
        string? authorization = context.Request.Headers.Authorization.FirstOrDefault();

        if (!TokensMatch(authorization, expectedHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
            await context.Response.WriteAsJsonAsync(new
            {
                error = "A valid bearer token is required."
            });
            return;
        }

        await _next(context);
    }

    private static bool TokensMatch(string? supplied, string expected)
    {
        if (supplied is null)
        {
            return false;
        }

        byte[] suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

        return suppliedBytes.Length == expectedBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                suppliedBytes,
                expectedBytes);
    }
}

sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch
        {
            // Let the outer error middleware produce the JSON error response.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            _logger.LogInformation(
                "{Method} {Path} responded {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    }
}
