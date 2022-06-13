using System.Timers;

namespace GameServer.Core.Helpers
{
    public class PerformanceLeakFinder : IDisposable
    {
        private List<TimeSpan> DurationPoints { get; } = new();

        private DateTime _startPoint;
        public TimeSpan Average => TimeSpan.FromTicks((long) Math.Round(DurationPoints.Average(t => t.Ticks)));
        private readonly System.Timers.Timer _myTimer = new System.Timers.Timer();
        private readonly string _name;

        public PerformanceLeakFinder(string name)
        {
            _name = name;
            _myTimer.Elapsed += Export;
            _myTimer.Interval = 3600000; // 1h
            _myTimer.Start();
        }

        public void Start()
        {
            _startPoint = DateTime.Now;
        }

        public void Stop()
        {
            var stopPoint = DateTime.Now;

            DurationPoints.Add(stopPoint - _startPoint);
        }

        public void Export(object? sender, ElapsedEventArgs e)
        {
            using StreamWriter file = new StreamWriter(@$"C:\Users\Daniel\Desktop\perfLog\{_name}_{Guid.NewGuid()}.csv");
            file.Write(string.Join(",", DurationPoints));
        }

        public void Dispose()
        {
            Stop();
            _myTimer.Stop();
            _myTimer.Dispose();
        }
    }
}
