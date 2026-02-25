using AOT;
using System.Runtime.InteropServices;

public static class NativeBackgroundRawInput
{
#if UNITY_STANDALONE_WIN
    const string DLL = "BackgroundRawInput";

    public delegate void KeyDelegate(
        RawKey key,
        KeyEventType type,
        ulong timestamp);

    [DllImport(DLL)]
    private static extern ulong _InitializeRawInput(
        KeyDelegate cb);

    [DllImport(DLL)]
    private static extern void _StopRawInput(
        ulong handle);
#endif

    static KeyDelegate cached;
    static ulong handle;

    public static void Initialize()
    {
#if UNITY_STANDALONE_WIN
        cached = OnNativeEvent;
        handle = _InitializeRawInput(cached);
#endif
    }

    public static void Shutdown()
    {
#if UNITY_STANDALONE_WIN
        if (handle != 0)
        {
            _StopRawInput(handle);
            handle = 0;
        }
#endif
    }

    [MonoPInvokeCallback(typeof(KeyDelegate))]
    private static void OnNativeEvent(
        RawKey key,
        KeyEventType type,
        ulong timestamp)
    {
        UnityEngine.Debug.Log($"[OnNativeEvent] Key: {key} Event: {type} Timestamp: {timestamp}");
    }
}