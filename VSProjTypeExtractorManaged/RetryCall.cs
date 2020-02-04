//
// credits to Leo Bushkin https://stackoverflow.com/users/91671/lbushkin for his poroposal at:
// https://stackoverflow.com/questions/1563191/cleanest-way-to-write-retry-logic

using System;
using System.Collections.Generic;
using System.Threading;

namespace VSProjTypeExtractorManaged
{
    public static class RetryCall
    {
        // call a delegate whith a parameter of type T and return of type T
        public static EnvDTE.Project Do<String>(
            Func<String, EnvDTE.Project> action,
            String param,
            TimeSpan retryInterval,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }
                    return action(param);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
