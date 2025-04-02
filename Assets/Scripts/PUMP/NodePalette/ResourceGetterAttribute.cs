using System;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ResourceGetterAttribute : Attribute
{
    public string Path { get; }

    public ResourceGetterAttribute(string path)
    {
        Path = path;
    }
}