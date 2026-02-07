using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ManagedStateAttribute : Attribute
{
    public Type Owner { get; }

    public ManagedStateAttribute(Type owner)
    {
        Owner = owner;
    }
}
