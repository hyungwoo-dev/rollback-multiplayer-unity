public class BattleWorldResource
{
    public string ResourcePath;
    public string NoGraphicsResourcePath;

    public BattleWorldResource(string resourceName) : this(resourceName, resourceName)
    {

    }

    public BattleWorldResource(string resourceName, string noGraphicsResourceName)
    {
        ResourcePath = resourceName;
        NoGraphicsResourcePath = noGraphicsResourceName;
    }
}
