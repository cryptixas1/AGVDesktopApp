using System;

namespace AGVDesktop.Models
{
    public class Telemetry
    {
        public DateTime Timestamp { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Battery { get; set; }
        public string? Raw { get; set; }
    }
}
