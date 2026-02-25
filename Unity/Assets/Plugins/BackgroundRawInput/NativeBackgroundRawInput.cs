using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

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

    private static KeyDelegate _keyDelegate;
    private static ulong _handle;
    private static HashSet<RawKey> _keyDownRawKeys;

    public static event Action<RawKey> OnKeyDown;
    public static event Action<RawKey> OnKeyUp;

    public static void Initialize()
    {
#if UNITY_STANDALONE_WIN
        _keyDelegate = OnNativeEvent;
        _handle = _InitializeRawInput(_keyDelegate);
        _keyDownRawKeys = new();
#endif
    }

    public static void Shutdown()
    {
#if UNITY_STANDALONE_WIN
        if (_handle != 0)
        {
            _StopRawInput(_handle);
            _handle = 0;
        }
#endif
    }

    [MonoPInvokeCallback(typeof(KeyDelegate))]
    private static void OnNativeEvent(
        RawKey key,
        KeyEventType type,
        ulong timestamp)
    {
        if (!NativeBackgroundRawInputFocusChecker.IsFocused)
        {
            return;
        }

        switch (type)
        {
            case KeyEventType.Down:
            {
                if (_keyDownRawKeys.Add(key))
                {
                    Debug.Log($"KeyDown: {key}");
                    OnKeyDown?.Invoke(key);
                }
                break;
            }
            case KeyEventType.Up:
            {
                if (_keyDownRawKeys.Remove(key))
                {
                    Debug.Log($"KeyUp: {key}");
                    OnKeyUp?.Invoke(key);
                }
                break;
            }
        }
    }
}