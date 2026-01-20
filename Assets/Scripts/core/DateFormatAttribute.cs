using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Put this on a string field to show a date-format dropdown in the Inspector.
/// Works with System.DateTime.ToString(format).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DateFormatAttribute : PropertyAttribute
{
    public readonly bool showPreview;
    public readonly string label;

    public DateFormatAttribute(bool showPreview = true, string label = "Date Format")
    {
        this.showPreview = showPreview;
        this.label = label;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DateFormatAttribute))]
public class DateFormatAttributeDrawer : PropertyDrawer
{
    private static readonly string[] LabelStrings =
    {
        "dd MMM yyyy   (20 Jan 2026)",
        "dd/MM/yyyy    (20/01/2026)",
        "MM/dd/yyyy    (01/20/2026)",
        "yyyy-MM-dd    (2026-01-20)",
        "HH:mm         (14:05)",
        "dd MMM yyyy HH:mm (20 Jan 2026 14:05)",
        "Custom..."
    };

    private static readonly string[] Formats =
    {
        "dd MMM yyyy",
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd",
        "HH:mm",
        "dd MMM yyyy HH:mm",
        "" // Custom...
    };

    private static readonly GUIContent[] LabelContents = BuildLabelContents();

    private static GUIContent[] BuildLabelContents()
    {
        var arr = new GUIContent[LabelStrings.Length];
        for (int i = 0; i < LabelStrings.Length; i++)
            arr[i] = new GUIContent(LabelStrings[i]);
        return arr;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (DateFormatAttribute)attribute;

        float h = EditorGUIUtility.singleLineHeight;

        bool isCustom = IsCustomFormat(property.stringValue);
        if (isCustom)
            h += 4 + EditorGUIUtility.singleLineHeight;

        if (attr.showPreview)
            h += 6 + (EditorGUIUtility.singleLineHeight * 1.6f);

        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [DateFormat] on a string field.");
            return;
        }

        var attr = (DateFormatAttribute)attribute;
        var displayLabel = new GUIContent(string.IsNullOrEmpty(attr.label) ? label.text : attr.label);

        float line = EditorGUIUtility.singleLineHeight;

        // Dropdown (use GUIContent[] overload for widest compatibility)
        Rect rDrop = new Rect(position.x, position.y, position.width, line);
        int selectedIndex = FindFormatIndex(property.stringValue);
        int newIndex = EditorGUI.Popup(rDrop, displayLabel, selectedIndex, LabelContents);

        if (newIndex != selectedIndex)
        {
            if (newIndex >= 0 && newIndex < Formats.Length)
            {
                if (newIndex == Formats.Length - 1)
                {
                    // Custom... keep existing; seed if empty
                    if (string.IsNullOrEmpty(property.stringValue))
                        property.stringValue = "dd MMM yyyy";
                }
                else
                {
                    property.stringValue = Formats[newIndex];
                }
            }
        }

        float y = position.y + line + 4;

        // Custom textbox if not one of presets
        bool isCustom = IsCustomFormat(property.stringValue);
        if (isCustom)
        {
            Rect rCustom = new Rect(position.x, y, position.width, line);
            property.stringValue = EditorGUI.TextField(rCustom, "Custom Format", property.stringValue);
            y += line + 6;
        }
        else
        {
            y += 2;
        }

        // Preview box
        if (attr.showPreview)
        {
            Rect rPrev = new Rect(position.x, y, position.width, line * 1.6f);

            string preview;
            try
            {
                string fmt = string.IsNullOrEmpty(property.stringValue) ? "dd MMM yyyy" : property.stringValue;
                preview = DateTime.Now.ToString(fmt);
            }
            catch
            {
                preview = "Invalid format string";
            }

            EditorGUI.HelpBox(rPrev, $"Preview: {preview}", MessageType.None);
        }
    }

    private static int FindFormatIndex(string current)
    {
        for (int i = 0; i < Formats.Length - 1; i++)
        {
            if (string.Equals(current, Formats[i], StringComparison.Ordinal))
                return i;
        }
        return Formats.Length - 1; // Custom...
    }

    private static bool IsCustomFormat(string current)
    {
        for (int i = 0; i < Formats.Length - 1; i++)
        {
            if (string.Equals(current, Formats[i], StringComparison.Ordinal))
                return false;
        }
        return true;
    }
}
#endif
