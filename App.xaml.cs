using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Pet.TaskDevourer.Helpers;

namespace Pet.TaskDevourer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Diagnostics.Process? _apiProcess;

        private bool IsPortOpen(string host, int port, int timeoutMs = 400)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var task = client.ConnectAsync(host, port);
                return task.Wait(timeoutMs) && client.Connected;
            }
            catch { return false; }
        }

        private void EnsureApiStarted()
        {
            const int port = 5005;
            if (IsPortOpen("localhost", port))
            {
                DiagnosticsLogger.Log($"API already listening on port {port}");
                return;
            }

            try
            {
                var baseDir = AppContext.BaseDirectory; 

                var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", ".."));
                string[] candidateConfigs = new[] { "Debug", "Release" };
                string? foundExe = null;
                foreach (var cfg in candidateConfigs)
                {
                    var p = System.IO.Path.Combine(root, "Server", "Pet.TaskDevourer.Api", "bin", cfg, "net8.0", "Pet.TaskDevourer.Api.exe");
                    if (System.IO.File.Exists(p)) { foundExe = p; break; }
                }
                if (foundExe == null)
                {
                    DiagnosticsLogger.Log("API exe not found, skipping autostart.");
                    return;
                }
                _apiProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(foundExe)
                {
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(foundExe)
                });
                DiagnosticsLogger.Log("Started API process: " + _apiProcess?.Id);
            }
            catch (Exception ex)
            {
                DiagnosticsLogger.Log("Failed to start API process: " + ex.Message);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += (s, args) =>
            {
                DiagnosticsLogger.Log("DispatcherUnhandledException: " + args.Exception);
                MessageBox.Show("UI exception: " + args.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true; 
            };
            AppDomain.CurrentDomain.UnhandledException += (s, args2) =>
            {
                DiagnosticsLogger.Log("DomainUnhandledException: " + args2.ExceptionObject);
                try { MessageBox.Show("Fatal exception: " + args2.ExceptionObject, "Fatal", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
            };
            TaskScheduler.UnobservedTaskException += (s, args3) =>
            {
                DiagnosticsLogger.Log("UnobservedTaskException: " + args3.Exception);
                args3.SetObserved();
            };

            DiagnosticsLogger.Log("App startup begin");
            EnsureApiStarted();
            base.OnStartup(e);
            DiagnosticsLogger.Log("App startup end");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            try
            {
                if (_apiProcess != null && !_apiProcess.HasExited)
                {
                    _apiProcess.CloseMainWindow();
                    if (!_apiProcess.WaitForExit(500))
                    {
                        _apiProcess.Kill(true);
                    }
                    DiagnosticsLogger.Log("API process terminated on exit.");
                }
            }
            catch (Exception ex)
            {
                DiagnosticsLogger.Log("Error terminating API process: " + ex.Message);
            }
        }
    }

}
