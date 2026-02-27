using System.Threading;
using UnityEngine;

public class NativeBackgroundRawInputFocusChecker : MonoBehaviour
{
    private static NativeBackgroundRawInputFocusChecker Instance { get; set; }
    private static long _focusFlag = 0;

    public static bool IsFocused => Interlocked.Read(ref _focusFlag) > 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        if (Instance == this && Application.isFocused)
        {
            if (!IsFocused)
            {
                Interlocked.Increment(ref _focusFlag);
            }
            NativeBackgroundRawInput.Initialize();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (!IsFocused)
            {
                Interlocked.Increment(ref _focusFlag);
                NativeBackgroundRawInput.Initialize();
            }
        }
        else
        {
            NativeBackgroundRawInput.Shutdown();
            Interlocked.Decrement(ref _focusFlag);
        }
        Debug.Log($"OnApplicationFocus: {focus}");
    }

    private void OnApplicationQuit()
    {
        NativeBackgroundRawInput.Shutdown();
    }
}
