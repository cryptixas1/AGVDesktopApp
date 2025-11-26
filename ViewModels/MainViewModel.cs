using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AGVDesktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Services.SerialService _serial;

        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>(SerialPort.GetPortNames());
        private string? _selectedPort;
        public string? SelectedPort { get => _selectedPort; set { _selectedPort = value; OnPropertyChanged(nameof(SelectedPort)); } }
        public int BaudRate { get; set; } = 115200;

        public ObservableCollection<Models.Telemetry> TelemetryItems { get; } = new ObservableCollection<Models.Telemetry>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        private string _statusMessage = "Ready";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

        private string _outgoingCommand = string.Empty;
        public string OutgoingCommand { get => _outgoingCommand; set { _outgoingCommand = value; OnPropertyChanged(nameof(OutgoingCommand)); } }

        public string MemoryBankText { get; } = System.IO.File.Exists("MemoryBank.md") ? System.IO.File.ReadAllText("MemoryBank.md") : "Memory Bank not found.";

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand SendCommandCommand { get; }
        private bool _minimizeToTray = true;
        public bool MinimizeToTray { get => _minimizeToTray; set { _minimizeToTray = value; OnPropertyChanged(nameof(MinimizeToTray)); } }

        private bool _useSystemMica = true;
        public bool UseSystemMica { get => _useSystemMica; set { _useSystemMica = value; OnPropertyChanged(nameof(UseSystemMica)); } }

        private bool _forceAcrylic = false;
        public bool ForceAcrylic { get => _forceAcrylic; set { _forceAcrylic = value; OnPropertyChanged(nameof(ForceAcrylic)); } }

        private string _backdropStatus = "Unknown";
        public string BackdropStatus { get => _backdropStatus; set { _backdropStatus = value; OnPropertyChanged(nameof(BackdropStatus)); } }

        private string _osVersionInfo = string.Empty;
        public string OSVersionInfo { get => _osVersionInfo; set { _osVersionInfo = value; OnPropertyChanged(nameof(OSVersionInfo)); } }

        private int _micaHr = -999;
        public int MicaHr { get => _micaHr; set { _micaHr = value; OnPropertyChanged(nameof(MicaHr)); } }

        private string _settingsPath = string.Empty;
        public string SettingsPath { get => _settingsPath; set { _settingsPath = value; OnPropertyChanged(nameof(SettingsPath)); } }

        private string _logPath = string.Empty;
        public string LogPath { get => _logPath; set { _logPath = value; OnPropertyChanged(nameof(LogPath)); } }

        private string _reflectionTrace = string.Empty;
        public string ReflectionTrace { get => _reflectionTrace; set { _reflectionTrace = value; OnPropertyChanged(nameof(ReflectionTrace)); } }

        public MainViewModel()
        {
            // Subscribe to file-logged events
            Services.UiLogService.OnLog += (line) => System.Windows.Application.Current.Dispatcher.Invoke(() => Logs.Insert(0, line));

            _serial = new Services.SerialService();
            _serial.TelemetryReceived += (s, t) => System.Windows.Application.Current.Dispatcher.Invoke(() => TelemetryItems.Insert(0, t));
            _serial.LogMessage += (s, l) => System.Windows.Application.Current.Dispatcher.Invoke(() => Logs.Insert(0, l));

            ConnectCommand = new RelayCommand(async _ => await Connect());
            DisconnectCommand = new RelayCommand(_ => Disconnect());
            StartCommand = new RelayCommand(_ => _serial.StartSimulation());
            StopCommand = new RelayCommand(_ => _serial.StopSimulation());
            SendCommandCommand = new RelayCommand(_ => { if (!string.IsNullOrWhiteSpace(OutgoingCommand)) _serial.Send(OutgoingCommand); });
        }

        private async Task Connect()
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                StatusMessage = "Select a port first.";
                return;
            }

            try
            {
                await _serial.ConnectAsync(SelectedPort, BaudRate);
                StatusMessage = $"Connected to {SelectedPort}@{BaudRate}";
            }
            catch (Exception ex)
            {
                StatusMessage = "Connection failed: " + ex.Message;
            }
        }

        private void Disconnect()
        {
            _serial.Disconnect();
            StatusMessage = "Disconnected";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
