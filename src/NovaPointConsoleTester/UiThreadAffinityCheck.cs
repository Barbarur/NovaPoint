using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Solutions;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace NovaPointConsoleTester
{
    /// <summary>
    /// Offline regression check: SolutionHandler must raise PropertyChanged on the thread that
    /// constructed it, no matter which thread the solution logs from. Runs with dummy credentials
    /// and never requests a token, so it needs no tenant and no network.
    /// </summary>
    internal class UiThreadAffinityCheck
    {
        internal static async Task<int> RunAsync()
        {
            Console.WriteLine("UI thread affinity check\n");

            PumpingSynchronizationContext pump = new();
            SynchronizationContext.SetSynchronizationContext(pump);

            int mainThreadId = Environment.CurrentManagedThreadId;
            ConcurrentBag<int> notificationThreadIds = [];

            AppClientPublicProperties properties = new()
            {
                TenantId = Guid.NewGuid(),
                ClientId = Guid.NewGuid(),
                CachingToken = false,
            };

            SolutionHandler handler = new(FakeSolution.Create, new FakeSolutionParameters(), properties);
            handler.PropertyChanged += (_, _) => notificationThreadIds.Add(Environment.CurrentManagedThreadId);

            Task run = handler.RunSolution();
            pump.RunUntil(run);

            await run;

            int latchFailures = SecondRunThrows(handler) ? 0 : 1;

            return Report(notificationThreadIds, mainThreadId, pump.SendCount, latchFailures, handler.SolutionFolder);
        }

        private static bool SecondRunThrows(SolutionHandler handler)
        {
            try
            {
                _ = handler.RunSolution();
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static int Report(ConcurrentBag<int> notificationThreadIds, int mainThreadId, int sendCount, int latchFailures, string solutionFolder)
        {
            int total = notificationThreadIds.Count;
            int offContext = notificationThreadIds.Count(id => id != mainThreadId);

            Console.WriteLine($"  notifications recorded : {total}");
            Console.WriteLine($"  off the main thread    : {offContext}");
            Console.WriteLine($"  Send calls             : {sendCount}");

            List<string> failures = [];
            if (total == 0) { failures.Add("no PropertyChanged notifications were recorded; the check would pass vacuously"); }
            if (offContext > 0)
            {
                string threads = string.Join(", ", notificationThreadIds.Where(id => id != mainThreadId).Distinct().Order());
                failures.Add($"{offContext} notification(s) arrived on thread(s) {threads} instead of {mainThreadId}");
            }
            if (sendCount > 0) { failures.Add($"{sendCount} synchronous Send call(s); marshalling must use Post or it can deadlock against the UI log lock"); }
            if (latchFailures > 0) { failures.Add("a second RunSolution() did not throw; the handler must be single-use"); }

            CleanUp(solutionFolder);

            if (failures.Count == 0)
            {
                Console.WriteLine("\nPASS\n");
                return 0;
            }

            Console.WriteLine();
            foreach (string failure in failures) { Console.WriteLine($"FAIL: {failure}"); }
            Console.WriteLine();
            return 1;
        }

        // LoggerSolution creates a real output folder in its constructor; don't leave it behind.
        private static void CleanUp(string solutionFolder)
        {
            try
            {
                if (Directory.Exists(solutionFolder)) { Directory.Delete(solutionFolder, true); }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>
    /// A console app has no message loop, so posted notifications would never be delivered and the
    /// check would pass vacuously. Send is recorded and forwarded rather than thrown from: throwing
    /// tears down whichever worker was logging and buries the real diagnosis.
    /// </summary>
    internal class PumpingSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback callback, object? state)> _queue = [];
        private int _sendCount;

        internal int SendCount => Volatile.Read(ref _sendCount);

        public override void Post(SendOrPostCallback d, object? state)
        {
            try { _queue.Add((d, state)); }
            catch (InvalidOperationException) { }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _sendCount);
            base.Send(d, state);
        }

        /// <summary>Drains the queue on the calling thread until the run completes.</summary>
        internal void RunUntil(Task run)
        {
            while (!run.IsCompleted)
            {
                if (_queue.TryTake(out var work, 50)) { work.callback(work.state); }
            }

            while (_queue.TryTake(out var remaining, 250)) { remaining.callback(remaining.state); }
        }
    }

    internal class FakeSolutionParameters : ISolutionParameters { }

    /// <summary>
    /// Logs from parallel workers through a sub-thread logger, reproducing the real
    /// LoggerThread.UI -> UiAddLog path without touching SharePoint. Never requests a token.
    /// </summary>
    internal class FakeSolution : ISolution
    {
        private readonly ContextSolution _ctx;

        private FakeSolution(ContextSolution ctx, FakeSolutionParameters _)
        {
            _ctx = ctx;
        }

        internal static ISolution Create(ContextSolution ctx, ISolutionParameters parameters)
        {
            return new FakeSolution(ctx, (FakeSolutionParameters)parameters);
        }

        public async Task RunAsync()
        {
            int[] batches = Enumerable.Range(0, 8).ToArray();

            await Parallel.ForEachAsync(
                batches,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (batch, _) =>
                {
                    ILogger threadLogger = await _ctx.Logger.GetSubThreadLogger();

                    for (int i = 0; i < 25; i++)
                    {
                        threadLogger.UI(GetType().Name, $"Batch {batch}, line {i}");
                        threadLogger.Progress((batch * 25 + i) / 2.0);
                    }
                });
        }
    }
}
