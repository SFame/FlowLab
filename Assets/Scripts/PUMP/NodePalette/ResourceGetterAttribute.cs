using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class ResourceGetterAttribute : Attribute
{
    public string Path { get; }
    public Color BackgroundColor { get; }
    public Color TextColor { get; }

    public ResourceGetterAttribute() : this(string.Empty) { }

    public ResourceGetterAttribute(string path) : this(path, "#FFFFFF", "#001A2F") { }

    public ResourceGetterAttribute(string path, string backgroundColorHex, string textColorHex)
    {
        Path = path;

        if (string.IsNullOrEmpty(backgroundColorHex) || !ColorUtility.TryParseHtmlString(backgroundColorHex, out Color backgroundParsedColor))
        {
            backgroundParsedColor = Color.white;
        }

        if (string.IsNullOrEmpty(textColorHex) || !ColorUtility.TryParseHtmlString(textColorHex, out Color textParsedColor))
        {
            textParsedColor = Color.black;
        }

        BackgroundColor = backgroundParsedColor;
        TextColor = textParsedColor;
    }
}