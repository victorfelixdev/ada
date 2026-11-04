using LibreHardwareMonitor.Hardware;

namespace Ada.App;

public sealed record SensorReading(string Id, string Device, string Name, string Type, float? Value, DateTimeOffset Timestamp)
{
    public string Unit => Type switch
    {
        "Temperature" => "°C",
        "Load" => "%",
        "Power" => "W",
        "Clock" => "MHz",
        "Voltage" => "V",
        "Fan" => "RPM",
        "Data" => "GB",
        "SmallData" => "MB",
        _ => "",
    };

    public string DisplayValue => Value is float value ? $"{value:F1} {Unit}".TrimEnd() : "indisponível";
}

public sealed class SensorCollector : IDisposable
{
    private readonly Computer computer = new()
    {
        IsMotherboardEnabled = true,
        IsCpuEnabled = true,
        IsMemoryEnabled = true,
        IsGpuEnabled = true,
        IsPsuEnabled = true,
    };
    private bool opened;

    public void Open()
    {
        computer.Open();
        opened = true;
    }

    public List<SensorReading> Read()
    {
        var result = new List<SensorReading>();
        if (opened)
            foreach (var hardware in computer.Hardware)
                ReadHardware(hardware, result);
        return result;
    }

    private static void ReadHardware(IHardware hardware, List<SensorReading> result)
    {
        hardware.Update();
        var now = DateTimeOffset.UtcNow;
        foreach (var sensor in hardware.Sensors)
            result.Add(new SensorReading(sensor.Identifier.ToString(), hardware.Name, sensor.Name, sensor.SensorType.ToString(), sensor.Value, now));
        foreach (var child in hardware.SubHardware)
            ReadHardware(child, result);
    }

    public void Dispose()
    {
        if (opened)
        {
            computer.Close();
            opened = false;
        }
    }
}
