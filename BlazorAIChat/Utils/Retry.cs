using Polly;
using Polly.Extensions.Http;
using System.Net.Http;
using System;
using System.Linq;

namespace BlazorAIChat.Utils
{
    public static class RetryHelper
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.Headers.RetryAfter != null) // Check if the Retry-After header is present
                .WaitAndRetryAsync(5, (retryAttempt, response, context) =>
                {
                    if (response.Result != null && response.Result.Headers.TryGetValues("Retry-After", out var values))
                    {
                        var retryAfter = values.First();
                        Console.WriteLine($"URL: {response.Result.RequestMessage?.RequestUri}, Retry-After header value: {retryAfter}");
                        if (int.TryParse(retryAfter, out var seconds))
                        {
                            return TimeSpan.FromSeconds(seconds); // Directly return TimeSpan
                        }
                        else if (DateTimeOffset.TryParse(retryAfter, out var dateTime))
                        {
                            var delay = dateTime - DateTimeOffset.UtcNow;
                            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero; // Directly return TimeSpan
                        }
                    }
                    var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                    var jitter = TimeSpan.FromMilliseconds(new Random().Next(-500, 500));
                    return baseDelay + jitter; // Directly return TimeSpan
                }, onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                {
                    // write to console the retry attempt
                    Console.WriteLine($"Retry {retryAttempt} scheduled in {timespan.TotalSeconds} seconds due to {outcome.Exception?.Message ?? "an error"}");
                    return Task.CompletedTask; // This is correct for an async callback
                });
        }



    }
}
