using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Ada.App;

public sealed record FrameReading(double Fps, double FrameTimeMs, double CpuBusyMs, double GpuBusyMs, DateTimeOffset PolledAt);

public sealed class PresentMonCollector : IDisposable
{
    // shortcut: this phase-1 probe uses the SDK installed at its default path; package the loader during the installer stage.
    private const string Loader = @"C:\Program Files\Intel\PresentMon\SDK\PresentMonAPI2Loader.dll";
    private readonly uint processId;
    private IntPtr session;
    private IntPtr query;
    private IntPtr frameQuery;
    private uint frameBlobSize;
    private ulong lastPresentQpc;

    [StructLayout(LayoutKind.Sequential)]
    private struct QueryElement
    {
        public int Metric;
        public int Stat;
        public uint DeviceId;
        public uint ArrayIndex;
        public ulong DataOffset;
        public ulong DataSize;
    }

    private readonly QueryElement[] elements =
    [
        new() { Metric = 12, Stat = 1 }, // presented FPS, average
        new() { Metric = 87, Stat = 1 }, // presented frame time, average
        new() { Metric = 9, Stat = 1 },  // CPU Busy, average
        new() { Metric = 14, Stat = 1 }, // GPU Busy, average
    ];
    private readonly QueryElement[] frameElements = [new() { Metric = 77, Stat = 0 }]; // present QPC per frame

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmOpenSession(out IntPtr handle);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmCloseSession(IntPtr handle);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmStartTrackingProcess(IntPtr handle, uint processId);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmStopTrackingProcess(IntPtr handle, uint processId);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmRegisterDynamicQuery(IntPtr session, out IntPtr query, [In, Out] QueryElement[] elements, ulong count, double windowMs, double offsetMs);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmFreeDynamicQuery(IntPtr query);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmPollDynamicQuery(IntPtr query, uint processId, [Out] byte[] blob, ref uint swapChains);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmRegisterFrameQuery(IntPtr session, out IntPtr query, [In, Out] QueryElement[] elements, ulong count, out uint blobSize);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmConsumeFrames(IntPtr query, uint processId, [Out] byte[] blob, ref uint frameCount);

    [DllImport(Loader, CallingConvention = CallingConvention.Cdecl)]
    private static extern int pmFreeFrameQuery(IntPtr query);

    public PresentMonCollector(int pid)
    {
        if (Marshal.SizeOf<QueryElement>() != 32)
            throw new InvalidOperationException("Layout nativo de consulta PresentMon incompatível.");
        processId = (uint)pid;
        try
        {
            Check(pmOpenSession(out session), "abrir sessão");
            Check(pmStartTrackingProcess(session, processId), "acompanhar processo");
            Check(pmRegisterDynamicQuery(session, out query, elements, (ulong)elements.Length, 1000, 0), "registrar métricas");
            Check(pmRegisterFrameQuery(session, out frameQuery, frameElements, 1, out frameBlobSize), "registrar marcador de frames");
            if (frameBlobSize < sizeof(ulong) || frameBlobSize > 4096)
                throw new InvalidDataException("Tamanho inesperado do frame PresentMon.");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public FrameReading? Read()
    {
        if (query == IntPtr.Zero) return null;
        using var process = Process.GetProcessById((int)processId);
        if (process.HasExited) return null;
        uint frameCount = 1024;
        var frameBlob = new byte[checked((int)(frameBlobSize * frameCount))];
        Check(pmConsumeFrames(frameQuery, processId, frameBlob, ref frameCount), "consumir frames");
        if (frameCount > 0)
        {
            var frameElement = frameElements[0];
            if (frameElement.DataSize != sizeof(ulong) || frameElement.DataOffset > frameBlobSize - sizeof(ulong))
                throw new InvalidDataException("Layout do marcador de frame inesperado do PresentMon.");
            var offset = checked((int)((frameCount - 1) * frameBlobSize + frameElement.DataOffset));
            var presentQpc = BitConverter.ToUInt64(frameBlob, offset);
            if (presentQpc > lastPresentQpc)
                lastPresentQpc = presentQpc;
        }
        var ageTicks = Stopwatch.GetTimestamp() - (long)lastPresentQpc;
        if (lastPresentQpc == 0 || ageTicks < 0 || ageTicks > 3 * Stopwatch.Frequency) return null;

        uint swapChains = 1;
        var blob = new byte[4096];
        Check(pmPollDynamicQuery(query, processId, blob, ref swapChains), "ler métricas");
        if (swapChains == 0) return null;

        double Value(int index)
        {
            var element = elements[index];
            if (element.DataSize != sizeof(double) || element.DataOffset > (ulong)(blob.Length - sizeof(double)))
                throw new InvalidDataException("Layout de métrica inesperado do PresentMon.");
            return BitConverter.ToDouble(blob, (int)element.DataOffset);
        }

        var fps = Value(0);
        var frameTime = Value(1);
        if (fps <= 0 || frameTime <= 0 || !double.IsFinite(fps) || !double.IsFinite(frameTime)) return null;
        return new FrameReading(fps, frameTime, Value(2), Value(3), DateTimeOffset.UtcNow);
    }

    public void Dispose()
    {
        if (frameQuery != IntPtr.Zero)
        {
            pmFreeFrameQuery(frameQuery);
            frameQuery = IntPtr.Zero;
        }
        if (query != IntPtr.Zero)
        {
            pmFreeDynamicQuery(query);
            query = IntPtr.Zero;
        }
        if (session != IntPtr.Zero)
        {
            pmStopTrackingProcess(session, processId);
            pmCloseSession(session);
            session = IntPtr.Zero;
        }
    }

    private static void Check(int status, string action)
    {
        if (status != 0) throw new InvalidOperationException($"PresentMon não conseguiu {action} (status {status}).");
    }
}
