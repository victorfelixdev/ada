using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ada.App;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExToolwindow = 0x80;
    private const long WsExNoactivate = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    public OverlayWindow()
    {
        InitializeComponent();
        Left = SystemParameters.WorkArea.Right - Width - 20;
        Top = SystemParameters.WorkArea.Top + 20;
        SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(this).Handle;
            long style = GetWindowLongPtr(handle, GwlExstyle).ToInt64();
            SetWindowLongPtr(handle, GwlExstyle, new IntPtr(style | WsExTransparent | WsExToolwindow | WsExNoactivate));
        };
    }

    public void SetText(string text) => OverlayText.Text = text;
}
