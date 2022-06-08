using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace GameServer.Core.Helpers
{
    public class PerformanceLeakFinder : IDisposable
    {
        private List<TimeSpan> _durationPoints { get; set; } = new List<TimeSpan>();

        private DateTime _startPoint;
        public TimeSpan Average { get => TimeSpan.FromTicks((long) Math.Round(_durationPoints.Average(t => t.Ticks))); }
        private System.Timers.Timer myTimer = new System.Timers.Timer();
        private string _name;

        public PerformanceLeakFinder(string name)
        {
            _name = name;
            myTimer.Elapsed += Export;
            myTimer.Interval = 3600000; // 1h
            myTimer.Start();
        }

        public void Start()
        {
            _startPoint = DateTime.Now;
        }

        public void Stop()
        {
            var stopPoint = DateTime.Now;

            _durationPoints.Add(stopPoint - _startPoint);
        }

        public void Export(object? sender, ElapsedEventArgs e)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@$"C:\Users\Daniel\Desktop\perfLog\{_name}_{Guid.NewGuid()}.csv"))
            {
                file.Write(string.Join(",", _durationPoints));
            }
        }

        public void Dispose()
        {
            Stop();
            myTimer.Stop();
            myTimer.Dispose();
        }
    }
}
