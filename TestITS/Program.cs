using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dynatrace.OneAgent.Sdk.Api;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
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

                System.Console.WriteLine($"[!dt dt.trace_id={traceId},dt.span_id={spanId}] Processing request");

                oneAgentSdk.AddCustomRequestAttribute("exampleAttribute", "exampleValue");

                // Simulate some work
                await Task.Delay(100);

                await context.Response.WriteAsync("Hello from Dynatrace OneAgent SDK Demo!");

                System.Console.WriteLine($"[!dt dt.trace_id={traceId},dt.span_id={spanId}] Request processed");
            });
        });
    }
}