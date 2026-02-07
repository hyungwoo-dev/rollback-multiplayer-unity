public class BattleWorldResource
{
    public string ResourcePath;
    public string ViewResourcePath;

    public BattleWorldResource(string resourceName) : this(resourceName, resourceName)
    {

    }

    public BattleWorldResource(string resourceName, string viewResourceName)
    {
        ResourcePath = resourceName;
        ViewResourcePath = viewResourceName;
    }
}
