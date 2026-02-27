using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class NativeBackgroundRawInput
{
    private static object _lock = new();
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
    private static ulong? _handle;
    private static HashSet<RawKey> _keyDownRawKeys;

    private static Action<RawKey> _onKeyDown;
    public static event Action<RawKey> OnKeyDown
    {
        add
        {
            lock (_lock)
            {
                _onKeyDown += value;
            }
        }
        remove
        {
            lock (_lock)
            {
                _onKeyDown -= value;
            }
        }
    }

    public static event Action<RawKey> _onKeyUp;
    public static event Action<RawKey> OnKeyUp
    {
        add
        {
            lock (_lock)
            {
                _onKeyUp += value;
            }
        }
        remove
        {
            lock (_lock)
            {
                _onKeyUp -= value;
            }
        }
    }

    public static void Initialize()
    {
#if UNITY_STANDALONE_WIN
        if (_handle == null)
        {
            _keyDelegate = OnNativeEvent;
            _handle = _InitializeRawInput(_keyDelegate);
            _keyDownRawKeys = new();
            Debug.Log("[NativeBackgroundRawInput::Initialize]");
        }
#endif
    }

    public static void Shutdown()
    {
#if UNITY_STANDALONE_WIN
        if (_handle != null)
        {
            _StopRawInput(_handle.Value);
            _handle = null;
            Debug.Log("[NativeBackgroundRawInput::Shutdown]");
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

        lock (_lock)
        {
            switch (type)
            {
                case KeyEventType.Down:
                {
                    if (_keyDownRawKeys.Add(key))
                    {
                        Debug.Log($"KeyDown: {key}");
                        _onKeyDown?.Invoke(key);
                    }
                    break;
                }
                case KeyEventType.Up:
                {
                    if (_keyDownRawKeys.Remove(key))
                    {
                        Debug.Log($"KeyUp: {key}");
                        _onKeyUp?.Invoke(key);
                    }
                    break;
                }
            }
        }
    }
}