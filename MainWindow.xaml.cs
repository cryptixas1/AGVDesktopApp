using System.Windows;
using System.Windows.Input;
using AGVDesktop.ViewModels;
using WinForms = System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace AGVDesktop
{
    public partial class MainWindow : Window
    {
        private WinForms.NotifyIcon _trayIcon;
        private System.Drawing.Icon? _generatedIcon;
        private System.Windows.Media.Brush? _originalAccentBrush;
        private int _lastMicaHr = -999;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // Try to enable Windows 11 Mica backdrop when available
            try
            {
                bool micaApplied = EnableMica();
                var initialVm = DataContext as ViewModels.MainViewModel;
                if (initialVm != null)
                {
                    initialVm.BackdropStatus = micaApplied ? "Mica applied" : "Acrylic (fallback)";
                    initialVm.MicaHr = _lastMicaHr;
                    initialVm.OSVersionInfo = Environment.OSVersion.ToString();
                    initialVm.SettingsPath = Services.SettingsService.SettingsFilePath;
                    initialVm.LogPath = Services.UiLogService.LogFilePath;
                    if (micaApplied)
                    {
                        // If Mica was applied, note that we didn't attempt FluentWPF reflection fallback
                        initialVm.ReflectionTrace = "Mica applied; FluentWPF fallback not attempted.";
                    }
                }

                // accent tuning: if Mica applied, increase accent tint for stronger effect; otherwise restore original
                try
                {
                    if (_originalAccentBrush == null && System.Windows.Application.Current.Resources.Contains("AccentBrush"))
                        _originalAccentBrush = System.Windows.Application.Current.Resources["AccentBrush"] as System.Windows.Media.Brush;

                    if (micaApplied)
                    {
                        if (System.Windows.Application.Current.Resources.Contains("AccentColor"))
                        {
                            var ac = (System.Windows.Media.Color)System.Windows.Application.Current.Resources["AccentColor"];
                            var tuned = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x33, ac.R, ac.G, ac.B));
                            System.Windows.Application.Current.Resources["AccentBrush"] = tuned;
                            Services.UiLogService.Log("AccentBrush tuned for Mica.");
                        }
                    }
                    else
                    {
                        if (_originalAccentBrush != null)
                        {
                            System.Windows.Application.Current.Resources["AccentBrush"] = _originalAccentBrush;
                            Services.UiLogService.Log("AccentBrush restored to original.");
                        }
                    }
                }
                catch { }

                // If DWM Mica was not applied, attempt FluentWPF HostBackdrop fallback via reflection
                if (!micaApplied)
                {
                    var attempts = new System.Collections.Generic.List<string>();
                    bool fwkb = TryEnableFluentHostBackdrop(attempts);
                    var vm2 = DataContext as ViewModels.MainViewModel;
                    if (vm2 != null)
                    {
                        vm2.ReflectionTrace = string.Join("\n", attempts);
                        if (fwkb)
                        {
                            vm2.BackdropStatus = "FluentWPF HostBackdrop applied";
                            Services.UiLogService.Log("FluentWPF HostBackdrop fallback applied.");
                        }
                        else
                        {
                            Services.UiLogService.Log("FluentWPF HostBackdrop fallback not applied.");
                        }
                    }
                }
            }
            catch
            {
                // If it fails, fall back to AcrylicPanel visual (already present in XAML)
                var fallbackVm = DataContext as ViewModels.MainViewModel;
                if (fallbackVm != null)
                {
                    fallbackVm.BackdropStatus = "Acrylic (fallback)";
                    fallbackVm.MicaHr = _lastMicaHr;
                    fallbackVm.OSVersionInfo = Environment.OSVersion.ToString();
                    fallbackVm.SettingsPath = Services.SettingsService.SettingsFilePath;
                    fallbackVm.LogPath = Services.UiLogService.LogFilePath;
                }
            }

            // Initialize logging and settings
            Services.UiLogService.Init();
            Services.SettingsService.Load();
            // Apply loaded settings to viewmodel
            var settingsVm = DataContext as ViewModels.MainViewModel;
            if (settingsVm != null)
            {
                settingsVm.MinimizeToTray = Services.SettingsService.Settings.MinimizeToTray;
                settingsVm.UseSystemMica = Services.SettingsService.Settings.UseSystemMica;
                settingsVm.ForceAcrylic = Services.SettingsService.Settings.ForceAcrylic;
            }
            Services.UiLogService.Log("Application started. Settings loaded.");

            // Initialize tray icon
            _trayIcon = new WinForms.NotifyIcon();
            // generate application icon at runtime and set both tray and window icon
            try
            {
                _generatedIcon = CreateAppIcon();
                if (_generatedIcon != null)
                {
                    _trayIcon.Icon = _generatedIcon;
                    // set window icon from generated icon
                    var hBitmap = _generatedIcon.ToBitmap().GetHbitmap();
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    this.Icon = bitmapSource;
                    NativeMethods.DeleteObject(hBitmap);
                }
                else
                {
                    _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            _trayIcon.Text = "IAOSB NUMTAL";
            var cms = new WinForms.ContextMenuStrip();
            cms.Items.Add("Open").Click += (s, e) => RestoreFromTray();
            cms.Items.Add("Exit").Click += (s, e) => { _trayIcon.Visible = false; System.Windows.Application.Current.Shutdown(); };
            _trayIcon.ContextMenuStrip = cms;
            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            this.StateChanged += MainWindow_StateChanged;
        }

        private bool EnableMica()
        {
            // DWMWA_SYSTEMBACKDROP_TYPE = 38 (Windows 11 22H2+)
            const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
            // DWMSBT_MAINWINDOW = 2
            int backdropType = 2;

            // Only attempt on Windows 11 build 22000+
            try
            {
                var v = Environment.OSVersion.Version;
                if (v.Major < 10 || (v.Major == 10 && v.Build < 22000))
                    return false;
            }
            catch
            {
                // if we can't determine, try anyway
            }

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero)
                return false;

            try
            {
                int hr = NativeMethods.DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, Marshal.SizeOf<int>());
                _lastMicaHr = hr;
                return hr == 0;
            }
            catch
            {
                // ignore failures - not all Windows versions support this attribute
                _lastMicaHr = -1;
                return false;
            }
        }

        private bool TryEnableFluentHostBackdrop(System.Collections.Generic.List<string> attempts)
        {
            try
            {
                // Respect settings
                if (!Services.SettingsService.Settings.UseSystemMica || Services.SettingsService.Settings.ForceAcrylic)
                {
                    attempts.Add("Skipping FluentWPF fallback due to settings (ForceAcrylic or UseSystemMica=false).");
                    return false;
                }

                var asm = System.AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "FluentWPF", StringComparison.OrdinalIgnoreCase));
                if (asm == null)
                {
                    attempts.Add("FluentWPF assembly not loaded, attempting to load 'FluentWPF'.");
                    try { asm = System.Reflection.Assembly.Load("FluentWPF"); attempts.Add("Loaded FluentWPF assembly."); } catch (Exception ex) { attempts.Add("Failed to load FluentWPF: " + ex.Message); asm = null; }
                }
                if (asm == null) { attempts.Add("No FluentWPF assembly available."); return false; }

                // Known candidate types and method name fragments used by FluentWPF/other libs
                string[] candidateTypeFragments = new[] { "Backdrop", "HostBackdrop", "SystemBackdrop", "Mica", "WindowBackdrop", "BackdropMaterial" };
                string[] candidateMethodFragments = new[] { "Enable", "SetIsHost", "SetIsSystemBackdrop", "SetBackdrop", "Initialize", "Attach", "EnableHost" };

                foreach (var t in asm.GetTypes())
                {
                    var fullName = t.FullName ?? t.Name;
                    bool typeMatches = false;
                    foreach (var frag in candidateTypeFragments)
                    {
                        if ((t.Name ?? string.Empty).IndexOf(frag, StringComparison.OrdinalIgnoreCase) >= 0 || (fullName ?? string.Empty).IndexOf(frag, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            typeMatches = true; break;
                        }
                    }
                    if (!typeMatches) continue;

                    attempts.Add("Considering type: " + fullName);
                    var methods = t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance);
                    foreach (var mi in methods)
                    {
                        var name = mi.Name ?? string.Empty;
                        bool nameMatches = false;
                        foreach (var frag in candidateMethodFragments)
                        {
                            if (name.IndexOf(frag, StringComparison.OrdinalIgnoreCase) >= 0) { nameMatches = true; break; }
                        }
                        if (!nameMatches) continue;

                        attempts.Add($"Trying method: {t.FullName}.{mi.Name} (static={mi.IsStatic})");

                        object? instance = null;
                        if (!mi.IsStatic)
                        {
                            try { instance = Activator.CreateInstance(t); attempts.Add("Created instance of " + t.FullName); } catch (Exception ex) { attempts.Add("Failed to create instance: " + ex.Message); instance = null; }
                        }

                        var pars = mi.GetParameters();
                        object?[]? args = null;
                        if (pars.Length == 0) args = new object[0];
                        else if (pars.Length == 1 && pars[0].ParameterType == typeof(System.Windows.Window)) args = new object[] { this };
                        else if (pars.Length == 1 && pars[0].ParameterType == typeof(IntPtr)) args = new object[] { new WindowInteropHelper(this).Handle };
                        else if (pars.Length == 2 && pars[0].ParameterType == typeof(IntPtr) && pars[1].ParameterType == typeof(int)) args = new object[] { new WindowInteropHelper(this).Handle, 2 };
                        else { attempts.Add("Skipped method due to unsupported signature: (" + string.Join(",", Array.ConvertAll(pars, p => p.ParameterType.Name)) + ")"); continue; }

                        try
                        {
                            var res = mi.Invoke(instance, args);
                            attempts.Add("Invocation succeeded, result: " + (res?.ToString() ?? "<null>"));
                            return true;
                        }
                        catch (System.Reflection.TargetInvocationException tie)
                        {
                            attempts.Add("Invocation TargetInvocationException: " + tie.InnerException?.Message);
                        }
                        catch (Exception ex)
                        {
                            attempts.Add("Invocation exception: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                attempts.Add("Reflection overall failure: " + ex.Message);
            }
            return false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
            }
            else
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            var vm = DataContext as MainViewModel;
            bool minimizeToTray = vm?.MinimizeToTray ?? true;
            if (WindowState == WindowState.Minimized)
            {
                if (minimizeToTray)
                {
                    // Hide window and show tray icon
                    Hide();
                    _trayIcon.Visible = true;
                    _trayIcon.ShowBalloonTip(700, "IAOSB NUMTAL", "Uygulama tepsiye küçültüldü.", WinForms.ToolTipIcon.Info);
                }
                else
                {
                    // Let the window minimize to taskbar normally; ensure tray icon hidden
                    _trayIcon.Visible = false;
                }
            }
            else
            {
                // Window is restored or maximized - hide tray and ensure window shown
                _trayIcon.Visible = false;
                Show();
                Activate();
            }
        }

        private void RestoreFromTray()
        {
            _trayIcon.Visible = false;
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void SystemMica_Checked(object sender, RoutedEventArgs e)
        {
            // save setting and attempt to apply
            if (DataContext is ViewModels.MainViewModel vm)
            {
                Services.SettingsService.Settings.UseSystemMica = vm.UseSystemMica;
                Services.SettingsService.Save();
                Services.UiLogService.Log($"UseSystemMica set to {vm.UseSystemMica}");
                if (vm.UseSystemMica && !vm.ForceAcrylic)
                {
                    try { bool ok = EnableMica(); Services.UiLogService.Log("Attempted to enable system Mica. Result: " + ok); vm.BackdropStatus = ok ? "Mica applied" : "Acrylic (fallback)"; } catch { Services.UiLogService.Log("EnableMica failed."); vm.BackdropStatus = "Acrylic (fallback)"; }
                }
            }
        }

        private void ForceAcrylic_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel vm)
            {
                Services.SettingsService.Settings.ForceAcrylic = vm.ForceAcrylic;
                Services.SettingsService.Save();
                Services.UiLogService.Log($"ForceAcrylic set to {vm.ForceAcrylic}");
                if (vm.ForceAcrylic)
                {
                    Services.UiLogService.Log("Force Acrylic enabled. Skipping system Mica attempts.");
                    vm.BackdropStatus = "Acrylic (forced)";
                }
                else
                {
                    if (vm.UseSystemMica) { try { bool ok = EnableMica(); Services.UiLogService.Log("Attempted to enable system Mica. Result: " + ok); vm.BackdropStatus = ok ? "Mica applied" : "Acrylic (fallback)"; } catch { Services.UiLogService.Log("EnableMica failed."); vm.BackdropStatus = "Acrylic (fallback)"; } }
                }
            }
        }

        private System.Drawing.Icon? CreateAppIcon()
        {
            try
            {
                int size = 64;
                using (var bmp = new Bitmap(size, size))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    // Background gradient
                    var rect = new Rectangle(0, 0, size, size);
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, System.Drawing.Color.FromArgb(10, 10, 10), System.Drawing.Color.FromArgb(10, 120, 255), 45f))
                    {
                        g.FillRectangle(brush, rect);
                    }
                    // Accent circle
                    using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(10, 132, 255)))
                    {
                        g.FillEllipse(brush, 6, 6, size - 12, size - 12);
                    }
                    // Draw initials 'IN'
                    using (var font = new Font("Segoe UI", 22, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                    using (var brush = new SolidBrush(System.Drawing.Color.White))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString("IN", font, brush, rect, sf);
                    }

                    // Create icon from bitmap
                    IntPtr hIcon = bmp.GetHicon();
                    var icon = System.Drawing.Icon.FromHandle(hIcon);
                    // Clone the icon so we own a separate managed copy, then destroy the native handle
                    var cloned = (System.Drawing.Icon)icon.Clone();
                    // destroy native hIcon now that we've cloned
                    NativeMethods.DestroyIcon(hIcon);
                    icon.Dispose();
                    return cloned;
                }
            }
            catch
            {
                return null;
            }
        }

        private static class NativeMethods
        {
            [DllImport("gdi32.dll")]
            internal static extern bool DeleteObject(IntPtr hObject);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool DestroyIcon(IntPtr hIcon);
            [DllImport("dwmapi.dll", PreserveSig = true)]
            internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void ToggleMaximizeRestore()
        {
            if (WindowState == WindowState.Normal)
            {
                // Use work area height so the window doesn't cover the taskbar
                MaxHeight = SystemParameters.WorkArea.Height;
                MaxWidth = SystemParameters.WorkArea.Width;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Dispose tray icon and close
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
        }
    }
}
