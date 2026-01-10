using System.Diagnostics;

[ManagedStateIgnore]
public class Debug
{
    private const string CONDITION_STRING = "UNITY_EDITOR";

    public static Debug Shared { get; } = new(string.Empty);

    private readonly string _tag;

    public Debug(string tag)
    {
        _tag = tag;
    }

    [Conditional(CONDITION_STRING)]
    public void Log(object message)
    {
        UnityEngine.Debug.Log(GetTagString() + message);
    }

    [Conditional(CONDITION_STRING)]
    public void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(GetTagString() + message);
    }

    [Conditional(CONDITION_STRING)]
    public void LogError(object message)
    {
        UnityEngine.Debug.LogError(GetTagString() + message);
    }

    [Conditional(CONDITION_STRING)]
    public void LogException(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    private string GetTagString()
    {
        return string.IsNullOrWhiteSpace(_tag) ? string.Empty : $"[{_tag}] ";
    }
}
