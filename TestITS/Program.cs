using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dynatrace.OneAgent.Sdk.Api;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting web host");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    private readonly IOneAgentSdk oneAgentSdk;

    public Startup()
    {
        oneAgentSdk = OneAgentSdkFactory.CreateInstance();
    }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                var traceContextInfo = oneAgentSdk.TraceContextInfo;
                var traceId = traceContextInfo.TraceId;
                var spanId = traceContextInfo.SpanId;

                Log.Information("[!dt dt.trace_id={TraceId},dt.span_id={SpanId}] Processing request", traceId, spanId);

                oneAgentSdk.AddCustomRequestAttribute("exampleAttribute", "exampleValue");

                // Simulate some work
                await Task.Delay(100);

                await context.Response.WriteAsync("Hello from Dynatrace OneAgent SDK Demo!");

                Log.Information("[!dt dt.trace_id={TraceId},dt.span_id={SpanId}] Request processed", traceId, spanId);
            });
        });
    }
}