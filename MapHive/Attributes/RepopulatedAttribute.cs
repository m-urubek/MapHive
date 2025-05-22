namespace MapHive.Attributes;

using System;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RepopulatedAttribute(string viewPath) : Attribute
{
    /// <summary>
    /// The relative path of the Razor view (e.g. "Views/Review/Edit") where this property must be repopulated via a hidden field
    /// </summary>
    public string ViewPath { get; } = viewPath;
}