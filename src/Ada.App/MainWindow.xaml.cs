using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Ada.App;

public partial class MainWindow : Window
{
    private readonly SensorCollector sensors = new();
    private readonly object sensorGate = new();
    private readonly object frameGate = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private PresentMonCollector? frames;
    private OverlayWindow? overlay;
    private bool polling;
    private bool sensorReady;
    private volatile bool closed;

    public ObservableCollection<SensorReading> SensorReadings { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += async (_, _) =>
        {
            try
            {
                sensorReady = await Task.Run(() =>
                {
                    lock (sensorGate)
                    {
                        if (closed) return false;
                        sensors.Open();
                        return true;
                    }
                });
                SensorStatus.Text = "Sensores: coleta iniciada.";
            }
            catch (Exception error)
            {
                SensorStatus.Text = $"Sensores: {error.Message}";
            }

            if (closed) return;
            timer.Tick += Poll;
            timer.Start();
            await Refresh();
        };
        Closed += (_, _) =>
        {
            closed = true;
            timer.Stop();
            lock (frameGate) frames?.Dispose();
            lock (sensorGate) sensors.Dispose();
            overlay?.Close();
        };
    }

    private void TrackButton_Click(object sender, RoutedEventArgs e)
    {
        lock (frameGate)
        {
            frames?.Dispose();
            frames = null;
        }
        FrameValues.Text = "FPS — · frametime — · CPU Busy — · GPU Busy —";
        overlay?.SetText(FrameValues.Text);
        if (!int.TryParse(ProcessIdBox.Text, out int pid) || pid <= 0)
        {
            FrameStatus.Text = "PresentMon: informe um PID válido.";
            return;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            lock (frameGate) frames = new PresentMonCollector(pid);
            FrameStatus.Text = $"PresentMon: monitorando {process.ProcessName} ({pid}).";
        }
        catch (Exception error)
        {
            FrameStatus.Text = $"PresentMon: {error.Message}";
        }
    }

    private void OverlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (overlay is null)
        {
            overlay = new OverlayWindow();
            overlay.Closed += (_, _) => overlay = null;
            overlay.Show();
            OverlayButton.Content = "Ocultar overlay";
        }
        else
        {
            overlay.Close();
            OverlayButton.Content = "Mostrar overlay";
        }
    }

    private async void Poll(object? sender, EventArgs e) => await Refresh();

    private async Task Refresh()
    {
        if (polling) return;
        polling = true;
        try
        {
            if (sensorReady)
            {
                try
                {
                    var readings = await Task.Run(() =>
                    {
                        lock (sensorGate) return sensors.Read();
                    });
                    SensorReadings.Clear();
                    foreach (var reading in readings) SensorReadings.Add(reading);
                    SensorStatus.Text = $"Sensores: {readings.Count(r => r.Value is not null)} de {readings.Count} leituras disponíveis.";
                }
                catch (Exception error)
                {
                    SensorStatus.Text = $"Sensores: {error.Message}";
                }
            }

            var target = frames;
            if (target is not null)
            {
                try
                {
                    var sample = await Task.Run(() =>
                    {
                        lock (frameGate) return target.Read();
                    });
                    if (!ReferenceEquals(frames, target)) return;
                    FrameValues.Text = sample is null
                        ? "FPS — · frametime — · CPU Busy — · GPU Busy —"
                        : $"FPS {sample.Fps:F1} · frametime {sample.FrameTimeMs:F2} ms · CPU Busy {sample.CpuBusyMs:F2} ms · GPU Busy {sample.GpuBusyMs:F2} ms";
                    FrameStatus.Text = sample is null ? "PresentMon: aguardando frames recentes do processo." : "PresentMon: recebendo frames recentes.";
                    overlay?.SetText(FrameValues.Text);
                }
                catch (Exception error)
                {
                    if (!ReferenceEquals(frames, target)) return;
                    lock (frameGate)
                    {
                        target.Dispose();
                        frames = null;
                    }
                    FrameStatus.Text = $"PresentMon: {error.Message}";
                    FrameValues.Text = "FPS — · frametime — · CPU Busy — · GPU Busy —";
                    overlay?.SetText(FrameValues.Text);
                }
            }
        }
        finally
        {
            polling = false;
        }
    }
}
