using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Dynatrace.OneAgent.Sdk.Api;

var builder = WebApplication.CreateBuilder(args);

// Create OneAgent SDK instance
IOneAgentSdk oneAgentSdk = OneAgentSdkFactory.CreateInstance();

// Add services to the container.
builder.Services.AddSingleton<IOneAgentSdk>(oneAgentSdk);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapGet("/", async (HttpContext context, ILogger<Program> logger, IOneAgentSdk sdk) =>
{
    var traceContextInfo = sdk.TraceContextInfo;
    var traceId = traceContextInfo.TraceId;
    var spanId = traceContextInfo.SpanId;

    logger.LogInformation($"[!dt dt.trace_id={traceId},dt.span_id={spanId}] Processing request");

    sdk.AddCustomRequestAttribute("exampleAttribute", "exampleValue");

    // Simulate some work
    await Task.Delay(100);

    await context.Response.WriteAsync("Hello from Dynatrace OneAgent SDK Demo!");

    logger.LogInformation($"[!dt dt.trace_id={traceId},dt.span_id={spanId}] Request processed");
});

app.Run();