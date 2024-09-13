using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dynatrace.OneAgent.Sdk.Api;
using Serilog;
using Newtonsoft.Json.Linq;

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
                Log.Information("Web host configured with Startup class");
            });
}

public class Startup
{
    private readonly IOneAgentSdk oneAgentSdk;
    private readonly string processGroupInstanceId;

    public Startup()
    {
        oneAgentSdk = OneAgentSdkFactory.CreateInstance() ?? throw new InvalidOperationException("Failed to initialize OneAgent SDK.");
        Log.Information("OneAgent SDK initialized successfully.");

        // Load metadata
        processGroupInstanceId = LoadProcessGroupInstanceId();
        if (!string.IsNullOrEmpty(processGroupInstanceId))
        {
            Log.Information("Process Group Instance ID: {ProcessGroupInstanceId}", processGroupInstanceId);
        }
        else
        {
            Log.Warning("Process Group Instance ID not found.");
        }
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

                if (string.IsNullOrEmpty(traceId) || string.IsNullOrEmpty(spanId))
                {
                    Log.Warning("TraceId or SpanId is null or empty.");
                }
                else
                {
                    Log.Information("[!dt dt.trace_id={TraceId},dt.span_id={SpanId},dt.process_group_instance={ProcessGroupInstanceId}] Processing request", traceId, spanId, processGroupInstanceId);
                }

                oneAgentSdk.AddCustomRequestAttribute("exampleAttribute", "exampleValue");

                // Simulate some work
                await Task.Delay(100);

                await context.Response.WriteAsync("Hello from Dynatrace OneAgent SDK Demo!");

                Log.Information("[!dt dt.trace_id={TraceId},dt.span_id={SpanId},dt.process_group_instance={ProcessGroupInstanceId}] Request processed", traceId, spanId, processGroupInstanceId);
            });
        });
    }

    private string? LoadProcessGroupInstanceId()
    {
        string[] metadataFiles = { "dt_metadata_e617c525669e072eebe3d0f08212e8f2.json", "/var/lib/dynatrace/enrichment/dt_metadata.json" };
        foreach (var file in metadataFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var jsonObject = JObject.Parse(json);
                if (jsonObject.TryGetValue("dt.entity.process_group_instance", out var processGroupInstanceId))
                {
                    return processGroupInstanceId.ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load metadata from {File}: {Exception}", file, ex);
            }
        }
        return null;
    }

}
