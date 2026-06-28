using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using PayBySquare.Api;
using PayBySquare.Core;
using PayBySquare.Core.Models;
using PayBySquare.Core.Qr;
using PayBySquare.Core.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "PAY by square API",
        Version = "v1",
        Description = "Database-free PAY by square QR generator & decoder for Slovak/Czech bank payments. " +
                      "Pure-managed pipeline (no native xz, no System.Drawing) — runs anywhere, including a NAS.",
    });
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Optional fixed-window rate limiting (set PBS_RATE_LIMIT to requests-per-minute to enable).
var rateLimit = ParseInt(Environment.GetEnvironmentVariable("PBS_RATE_LIMIT"));
if (rateLimit is > 0)
{
    builder.Services.AddRateLimiter(o =>
    {
        o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "global",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = rateLimit.Value, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    });
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PAY by square API v1"));
app.UseCors();
if (rateLimit is > 0) app.UseRateLimiter();

// Optional API key. Set PBS_API_KEY to require the X-Api-Key header on /api/* routes.
var apiKey = Environment.GetEnvironmentVariable("PBS_API_KEY");
if (!string.IsNullOrEmpty(apiKey))
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            var provided = ctx.Request.Headers["X-Api-Key"].ToString();
            if (provided != apiKey)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = "Invalid or missing X-Api-Key." });
                return;
            }
        }
        await next();
    });
}

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "pay-by-square", version = "1.0" }))
   .WithTags("Meta");

var api = app.MapGroup("/api/v1").WithTags("PAY by square");

// --- Generate a QR image from query parameters (great for <img src="..."> embedding). ---
api.MapGet("/qr", ([AsParameters] QrQuery query) =>
{
    var payment = query.ToPayment();
    if (!TryBuildModel(payment, out var model, out var error)) return error;

    string code = PayBySquareCodec.Encode(model);
    var render = QrOptionsMapper.Build(query.Ecc, query.Size, query.Ppm, query.Margin, query.Dark, query.Light,
        query.Logo, query.BrandColor);
    return Render(code, query.Format, render);
})
.WithSummary("Generate a QR code via query string")
.WithDescription("Returns a PNG (default), SVG or the raw encoded string. Example: /api/v1/qr?amount=25.50&iban=SK7283300000009111111118&vs=1234&format=png");

// --- Generate a QR image/string from a JSON body. ---
api.MapPost("/qr", ([FromBody] PaymentDto dto, [AsParameters] RenderQuery query) =>
{
    if (!TryBuildModel(dto, out var model, out var error)) return error;

    string code = PayBySquareCodec.Encode(model);
    var render = QrOptionsMapper.Build(query.Ecc, query.Size, query.Ppm, query.Margin, query.Dark, query.Light,
        query.Logo, query.BrandColor);
    return Render(code, query.Format, render);
})
.WithSummary("Generate a QR code from a JSON payment");

// --- Return just the encoded PAY by square string. ---
api.MapPost("/encode", ([FromBody] PaymentDto dto) =>
{
    if (!TryBuildModel(dto, out var model, out var error)) return error;
    return Results.Ok(new EncodeResponse(PayBySquareCodec.Encode(model)));
})
.WithSummary("Encode a payment into a PAY by square string");

// --- Decode a PAY by square string back into payment fields. ---
api.MapPost("/decode", ([FromBody] DecodeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Code))
        return Problem("Code is required.", "code");
    try
    {
        var result = PayBySquareCodec.Decode(req.Code);
        var dto = ToDto(result.Payment);
        return Results.Ok(new DecodeResponse(result.CrcValid, result.RawData, dto));
    }
    catch (Exception ex)
    {
        return Problem($"Could not decode: {ex.Message}", "code");
    }
})
.WithSummary("Decode a PAY by square string");

// --- Validate an IBAN. ---
api.MapPost("/validate-iban", ([FromBody] ValidateRequest req) =>
{
    bool valid = IbanValidator.Validate(req.Iban, out var error);
    return Results.Ok(new ValidateResponse(valid, IbanValidator.Normalize(req.Iban), error));
})
.WithSummary("Validate an IBAN (mod-97)");

app.Run();
return;

// ---------- helpers ----------

static int? ParseInt(string? s) => int.TryParse(s, out var v) ? v : null;

static IResult Problem(string detail, string? field = null) =>
    Results.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid request",
        extensions: field is null ? null : new Dictionary<string, object?> { ["field"] = field });

static bool TryBuildModel(PaymentDto dto, out PaymentRequest model, out IResult error)
{
    model = dto.ToModel();
    if (!IbanValidator.Validate(model.Iban, out var ibanError))
    {
        error = Problem(ibanError ?? "Invalid IBAN.", "iban");
        return false;
    }
    foreach (var alt in model.AlternativeAccounts)
    {
        if (!IbanValidator.Validate(alt.Iban, out var altError))
        {
            error = Problem(altError ?? "Invalid alternative IBAN.", "alternativeAccounts");
            return false;
        }
    }
    error = Results.Empty;
    return true;
}

static IResult Render(string code, string? format, QrOptions render)
{
    switch ((format ?? "png").Trim().ToLowerInvariant())
    {
        case "string":
        case "text":
            return Results.Text(code, "text/plain");
        case "json":
            return Results.Ok(new EncodeResponse(code));
        case "svg":
            return Results.Text(QrRenderer.RenderSvg(code, render), "image/svg+xml");
        case "png":
        default:
            return Results.Bytes(QrRenderer.RenderPng(code, render), "image/png");
    }
}

static PaymentDto ToDto(PaymentRequest m) => new()
{
    Amount = m.Amount,
    CurrencyCode = m.CurrencyCode,
    DueDate = m.DueDate,
    Iban = m.Iban,
    Bic = m.Bic,
    VariableSymbol = m.VariableSymbol,
    ConstantSymbol = m.ConstantSymbol,
    SpecificSymbol = m.SpecificSymbol,
    OriginatorReference = m.OriginatorReference,
    Note = m.Note,
    BeneficiaryName = m.BeneficiaryName,
    BeneficiaryAddressLine1 = m.BeneficiaryAddressLine1,
    BeneficiaryAddressLine2 = m.BeneficiaryAddressLine2,
    AlternativeAccounts = m.AlternativeAccounts.Count > 0 ? m.AlternativeAccounts : null,
};

/// <summary>Exposes the implicit Program class so integration tests can reference it.</summary>
public partial class Program { }
