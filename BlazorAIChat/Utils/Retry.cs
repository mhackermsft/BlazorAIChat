using Polly;

namespace BlazorAIChat.Utils
{
    public static class RetryHelper
    {
        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, TimeSpan initialWait, int retryCount = 0)
        {
            PolicyResult<T> policyResult = await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retryCount, retryAttempt =>
                    TimeSpan.FromMilliseconds(initialWait.TotalMilliseconds * Math.Pow(2, retryAttempt - 1)))
                .ExecuteAndCaptureAsync(action);

            if (policyResult.Outcome == OutcomeType.Failure)
            {
                throw policyResult.FinalException;
            }

            return policyResult.Result;
        }
    }
}
