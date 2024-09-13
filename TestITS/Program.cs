using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Dynatrace.OneAgent.Sdk.Api;

class Program
{
    static async Task Main(string[] args)
    {
        // Create OneAgent SDK instance
        IOneAgentSdk oneAgentSdk = OneAgentSdkFactory.CreateInstance();

        // Set up a simple HTTP server
        string url = "http://*:80/";
        using (var listener = new HttpListener())
        {
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine($"Listening on {url}");

            while (true)
            {
                var context = await listener.GetContextAsync();
                var response = context.Response;

                var traceContextInfo = oneAgentSdk.TraceContextInfo;
                var traceId = traceContextInfo.TraceId;
                var spanId = traceContextInfo.SpanId;

                Console.WriteLine($"[!dt dt.trace_id={traceId},dt.span_id={spanId}] Processing request");

                oneAgentSdk.AddCustomRequestAttribute("exampleAttribute", "exampleValue");

                // Simulate some work
                await Task.Delay(100);

                string responseString = "Hello from Dynatrace OneAgent SDK Demo!";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();

                Console.WriteLine($"[!dt dt.trace_id={traceId},dt.span_id={spanId}] Request processed");
            }
        }
    }
}