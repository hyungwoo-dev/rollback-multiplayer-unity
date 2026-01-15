using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ManagedStateIgnoreAttribute : Attribute
{
}
