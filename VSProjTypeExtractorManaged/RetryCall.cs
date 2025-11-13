//
// for the initial variant, credits to Leo Bushkin https://stackoverflow.com/users/91671/lbushkin for his poroposal at:
// https://stackoverflow.com/questions/1563191/cleanest-way-to-write-retry-logic

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace VSProjTypeExtractorManaged
{
    public static class RetryCall
    {
        private static readonly Random _rng = new Random();

        // Generic version for any Func<T>
        public static T Do<T>(
            Func<T> action,
            TimeSpan baseRetryInterval,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();
            T result = default!;
            int attempt;

            for (attempt = 1; attempt <= maxAttemptCount; attempt++)
            {
                try
                {
                    if (attempt > 1)
                        SleepWithBackoff(baseRetryInterval, attempt);

                    result = action();
                    break;
                }
                catch (COMException ex) when (IsRetryableComException(ex))
                {
                    exceptions.Add(ex);
                    // transient, will retry silently
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    break; // non-transient, stop retrying
                }
            }

            var conlog = VSProjTypeExtractorManaged.ConAndLog.Instance;

            if (attempt <= maxAttemptCount && exceptions.Count > 0)
            {
                var lastEx = exceptions[exceptions.Count - 1];
                conlog.WriteLineDebug(
                    "Recovered call after {0} attempt(s). Last transient error: {1}: {2}",
                    attempt,
                    lastEx.GetType().Name,
                    lastEx.Message);
            }

            if (attempt > maxAttemptCount)
            {
                var lastEx = exceptions.Count > 0 ? exceptions[exceptions.Count - 1] : null;
                conlog.WriteLineWarn(
                    "Operation failed after {0} attempts. Last error: {1}: {2}",
                    maxAttemptCount,
                    lastEx?.GetType().Name ?? "Unknown",
                    lastEx?.Message ?? "no details");
                throw new AggregateException(exceptions);
            }

            return result;
        }

        // Specialized helper for DTE project loading
        public static EnvDTE.Project Do<stringT>(
            Func<stringT, EnvDTE.Project> action,
            stringT param,
            TimeSpan baseRetryInterval,
            int maxAttemptCount = 3)
        {
            return Do(() => action(param), baseRetryInterval, maxAttemptCount);
        }

        // --- helpers ---
        private static bool IsRetryableComException(COMException ex)
        {
            uint code = (uint)ex.ErrorCode;
            return code == 0x8001010A   // RPC_E_SERVERCALL_RETRYLATER
                || code == 0x80010001;  // RPC_E_CALL_REJECTED
        }

        private static void SleepWithBackoff(TimeSpan baseInterval, int attempt)
        {
            double baseMs = baseInterval.TotalMilliseconds;
            double backoff = baseMs * Math.Pow(2, attempt - 2);
            backoff = Math.Min(backoff, 8000); // cap to 8s
            double jitterFactor = 0.8 + (_rng.NextDouble() * 0.4); // ±20%
            int delayMs = (int)(backoff * jitterFactor);
            Thread.Sleep(delayMs);
        }
    }
}
