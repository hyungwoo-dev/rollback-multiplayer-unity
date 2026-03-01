[ManagedStateIgnore]
public class Debug
{
    public static Debug Shared { get; } = new(string.Empty);

    private readonly string _tag;

    public Debug(string tag)
    {
        _tag = tag;
    }

    public void Log(object message)
    {
        UnityEngine.Debug.Log(GetTagString() + message);
    }

    public void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(GetTagString() + message);
    }

    public void LogError(object message)
    {
        UnityEngine.Debug.LogError(GetTagString() + message);
    }

    public void LogException(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    private string GetTagString()
    {
        return string.IsNullOrWhiteSpace(_tag) ? string.Empty : $"[{_tag}] ";
    }
}
