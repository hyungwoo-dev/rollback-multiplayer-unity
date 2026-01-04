using System;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ManagedDictionaryAttribute : Attribute
{
    public string BackingListName { get; }
    public string KeyMemberName { get; }

    public ManagedDictionaryAttribute(string backingListName, string keyMemberName)
    {
        BackingListName = backingListName;
        KeyMemberName = keyMemberName;
    }
}
