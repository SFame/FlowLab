using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ResourceGetterAttribute : Attribute
{
    public string Path { get; }
    public Color Color { get; }

    public ResourceGetterAttribute() : this(string.Empty) { }

    public ResourceGetterAttribute(string path) : this(path, "#FFFFFF") { }

    public ResourceGetterAttribute(string path, string colorHex)
    {
        Path = path;

        if (string.IsNullOrEmpty(colorHex) || !ColorUtility.TryParseHtmlString(colorHex, out Color parsedColor))
        {
            parsedColor = Color.white;
        }

        Color = parsedColor;
    }
}