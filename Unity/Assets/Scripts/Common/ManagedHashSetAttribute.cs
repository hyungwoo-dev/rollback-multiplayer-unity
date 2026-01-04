using System;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ManagedHashSetAttribute : Attribute
{
    public string BackingListName { get; }

    public ManagedHashSetAttribute(string backingListName)
    {
        BackingListName = backingListName;
    }
}