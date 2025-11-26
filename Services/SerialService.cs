using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace AGVDesktop.Services
{
    public class SerialService
    {
        private SerialPort? _port;
        private System.Threading.Timer? _simTimer;
        private readonly Random _rnd = new Random();

        public event EventHandler<Models.Telemetry>? TelemetryReceived;
        public event EventHandler<string>? LogMessage;

        public async System.Threading.Tasks.Task ConnectAsync(string portName, int baudRate)
        {
            Disconnect();
            _port = new SerialPort(portName, baudRate) { Encoding = Encoding.ASCII, ReadTimeout = 500, WriteTimeout = 500 };
            _port.DataReceived += Port_DataReceived;
            _port.Open();
            LogMessage?.Invoke(this, $"Opened {_port.PortName} @ {baudRate}");
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void Port_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = sender as SerialPort;
                if (sp == null) return;
                var line = sp.ReadLine();
                LogMessage?.Invoke(this, "RX: " + line.Trim());
                // parse simple telemetry "X,Y,Speed,Battery"
                var parts = line.Split(',');
                if (parts.Length >= 4 && double.TryParse(parts[0], out var x) && double.TryParse(parts[1], out var y))
                {
                    var t = new Models.Telemetry { Timestamp = DateTime.Now, X = x, Y = y, Raw = line.Trim() };
                    TelemetryReceived?.Invoke(this, t);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, "RX error: " + ex.Message);
            }
        }

        public void Disconnect()
        {
            try
            {
                _port?.Close();
                _port = null;
                LogMessage?.Invoke(this, "Port closed");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, "Disconnect error: " + ex.Message);
            }
        }

        public void Send(string text)
        {
            try
            {
                if (_port != null && _port.IsOpen)
                {
                    _port.WriteLine(text);
                    LogMessage?.Invoke(this, "TX: " + text);
                }
                else
                {
                    LogMessage?.Invoke(this, "Port not open, cannot send");
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, "TX error: " + ex.Message);
            }
        }

        // Simple simulator that emits telemetry periodically
        public void StartSimulation(int intervalMs = 500)
        {
            StopSimulation();
            _simTimer = new System.Threading.Timer(_ =>
            {
                var t = new Models.Telemetry
                {
                    Timestamp = DateTime.Now,
                    X = _rnd.NextDouble() * 10,
                    Y = _rnd.NextDouble() * 10,
                    Speed = _rnd.NextDouble() * 2,
                    Battery = 80 + _rnd.NextDouble() * 20,
                    Raw = "SIM"
                };
                TelemetryReceived?.Invoke(this, t);
                LogMessage?.Invoke(this, "SIM: telemetry emitted");
            }, null, 0, intervalMs);
            LogMessage?.Invoke(this, "Simulation started");
        }

        public void StopSimulation()
        {
            _simTimer?.Dispose();
            _simTimer = null;
            LogMessage?.Invoke(this, "Simulation stopped");
        }

        public void Stop()
        {
            StopSimulation();
            Disconnect();
        }
    }
}
