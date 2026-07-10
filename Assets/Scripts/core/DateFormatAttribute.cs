using System; // Imports DateTime, StringComparison, and AttributeUsageAttribute.
using UnityEngine; // Imports Unity runtime types such as PropertyAttribute.

#if UNITY_EDITOR // Includes the drawer code only while running inside the Unity Editor.
using UnityEditor; // Imports Unity editor types such as PropertyDrawer and EditorGUI.
#endif // Ends the editor-only using block.

// Starts the XML summary shown by IDE tooltips.
/// <summary>
// Explains where this attribute should be used.
/// Put this on a string field to show a date-format dropdown in the Inspector.
// Notes that each stored string is a DateTime format pattern.
/// Works with System.DateTime.ToString(format).
// Ends the XML summary.
/// </summary>
[AttributeUsage(AttributeTargets.Field)] // Allows this attribute to be placed on fields only.
public class DateFormatAttribute : PropertyAttribute // Declares a Unity property attribute that can customize Inspector drawing.
{ // Opens the DateFormatAttribute class body.
    public readonly bool showPreview; // Stores whether the Inspector should show a live date preview.
    public readonly string label; // Stores the label to show beside the date-format dropdown.

    public DateFormatAttribute(bool showPreview = true, string label = "Date Format") // Defines the attribute constructor with default Inspector options.
    { // Opens the constructor body.
        this.showPreview = showPreview; // Saves the preview visibility setting on the attribute instance.
        this.label = label; // Saves the custom label text on the attribute instance.
    } // Closes the constructor body.
} // Closes the DateFormatAttribute class body.

#if UNITY_EDITOR // Compiles the custom drawer only for the Unity Editor.
[CustomPropertyDrawer(typeof(DateFormatAttribute))] // Connects this drawer to fields marked with DateFormatAttribute.
public class DateFormatAttributeDrawer : PropertyDrawer // Declares the Inspector drawer for the attribute.
{ // Opens the DateFormatAttributeDrawer class body.
    private static readonly string[] LabelStrings = // Declares the dropdown text shown to users.
    { // Opens the dropdown label array.
        "dd MMM yyyy   (20 Jan 2026)", // Shows a day-month-name-year preset with an example.
        "dd/MM/yyyy    (20/01/2026)", // Shows a day/month/year preset with an example.
        "MM/dd/yyyy    (01/20/2026)", // Shows a month/day/year preset with an example.
        "yyyy-MM-dd    (2026-01-20)", // Shows an ISO-like year-month-day preset with an example.
        "HH:mm         (14:05)", // Shows a 24-hour time preset with an example.
        "dd MMM yyyy HH:mm (20 Jan 2026 14:05)", // Shows a combined date and time preset with an example.
        "Custom..." // Shows the option that lets users type their own format.
    }; // Closes the dropdown label array.

    private static readonly string[] Formats = // Declares the actual DateTime format strings saved to the serialized field.
    { // Opens the format value array.
        "dd MMM yyyy", // Stores the day-month-name-year format.
        "dd/MM/yyyy", // Stores the day/month/year format.
        "MM/dd/yyyy", // Stores the month/day/year format.
        "yyyy-MM-dd", // Stores the ISO-like year-month-day format.
        "HH:mm", // Stores the 24-hour time format.
        "dd MMM yyyy HH:mm", // Stores the combined date and time format.
        "" // Stores a placeholder value for the Custom option.
    }; // Closes the format value array.

    private static readonly GUIContent[] LabelContents = BuildLabelContents(); // Caches GUIContent wrappers for the dropdown labels.

    private static GUIContent[] BuildLabelContents() // Creates GUIContent objects from the plain label strings.
    { // Opens the label-content helper body.
        var arr = new GUIContent[LabelStrings.Length]; // Allocates one GUIContent slot for each dropdown label.
        for (int i = 0; i < LabelStrings.Length; i++) // Loops through every dropdown label.
            arr[i] = new GUIContent(LabelStrings[i]); // Wraps the current label string in GUIContent.
        return arr; // Returns the completed GUIContent array.
    } // Closes the label-content helper body.

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) // Calculates how much vertical Inspector space this property needs.
    { // Opens the height calculation method body.
        var attr = (DateFormatAttribute)attribute; // Reads the DateFormatAttribute attached to the field.

        float h = EditorGUIUtility.singleLineHeight; // Starts with enough height for the dropdown line.

        bool isCustom = IsCustomFormat(property.stringValue); // Checks whether the current string is outside the preset list.
        if (isCustom) // Adds extra space when a custom text field must be shown.
            h += 4 + EditorGUIUtility.singleLineHeight; // Adds padding and one line for the custom format field.

        if (attr.showPreview) // Adds extra space when the preview box is enabled.
            h += 6 + (EditorGUIUtility.singleLineHeight * 1.6f); // Adds padding and a taller line for the preview help box.

        return h; // Returns the total height Unity should reserve.
    } // Closes the height calculation method body.

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) // Draws the custom Inspector UI for the field.
    { // Opens the GUI drawing method body.
        if (property.propertyType != SerializedPropertyType.String) // Validates that the attribute is being used on a string property.
        { // Opens the invalid-property warning block.
            EditorGUI.LabelField(position, label.text, "Use [DateFormat] on a string field."); // Shows a warning when the attribute is on the wrong type.
            return; // Stops drawing because non-string properties cannot store date format text.
        } // Closes the invalid-property warning block.

        var attr = (DateFormatAttribute)attribute; // Reads the attribute settings for this field.
        var displayLabel = new GUIContent(string.IsNullOrEmpty(attr.label) ? label.text : attr.label); // Uses the custom label unless it is empty.

        float line = EditorGUIUtility.singleLineHeight; // Stores Unity's standard single-line control height.

        // Dropdown (use GUIContent[] overload for widest compatibility)
        Rect rDrop = new Rect(position.x, position.y, position.width, line); // Defines the rectangle used by the dropdown.
        int selectedIndex = FindFormatIndex(property.stringValue); // Finds which preset matches the current saved string.
        int newIndex = EditorGUI.Popup(rDrop, displayLabel, selectedIndex, LabelContents); // Draws the dropdown and reads the user's selection.

        if (newIndex != selectedIndex) // Responds only when the dropdown selection changed.
        { // Opens the selection-changed block.
            if (newIndex >= 0 && newIndex < Formats.Length) // Ensures the selected index is inside the format array.
            { // Opens the valid-selection block.
                if (newIndex == Formats.Length - 1) // Checks whether the user selected Custom.
                { // Opens the custom-selection block.
                    // Custom... keep existing; seed if empty
                    if (string.IsNullOrEmpty(property.stringValue)) // Checks whether there is no existing custom format to preserve.
                        property.stringValue = "dd MMM yyyy"; // Seeds the custom field with a useful default format.
                } // Closes the custom-selection block.
                else // Handles a preset selection.
                { // Opens the preset-selection block.
                    property.stringValue = Formats[newIndex]; // Saves the selected preset format string to the property.
                } // Closes the preset-selection block.
            } // Closes the valid-selection block.
        } // Closes the selection-changed block.

        float y = position.y + line + 4; // Starts the next control below the dropdown with a small gap.

        // Custom textbox if not one of presets
        bool isCustom = IsCustomFormat(property.stringValue); // Rechecks whether the current value is a custom format.
        if (isCustom) // Shows a text field when the value is not one of the presets.
        { // Opens the custom-textbox block.
            Rect rCustom = new Rect(position.x, y, position.width, line); // Defines the rectangle used by the custom format text field.
            property.stringValue = EditorGUI.TextField(rCustom, "Custom Format", property.stringValue); // Draws the text field and saves the edited custom format.
            y += line + 6; // Moves the next control below the text field with padding.
        } // Closes the custom-textbox block.
        else // Handles preset values that do not need a custom text field.
        { // Opens the preset-spacing block.
            y += 2; // Adds a small visual gap before the preview.
        } // Closes the preset-spacing block.

        // Preview box
        if (attr.showPreview) // Shows a preview when enabled by the attribute.
        { // Opens the preview block.
            Rect rPrev = new Rect(position.x, y, position.width, line * 1.6f); // Defines the rectangle used by the preview help box.

            string preview; // Declares the preview text that will be displayed.
            try // Attempts to format the current date using the chosen format string.
            { // Opens the preview formatting try block.
                string fmt = string.IsNullOrEmpty(property.stringValue) ? "dd MMM yyyy" : property.stringValue; // Uses a default format when the property is empty.
                preview = DateTime.Now.ToString(fmt); // Formats the current date and time for the preview.
            } // Closes the preview formatting try block.
            catch // Handles invalid date format strings.
            { // Opens the invalid-format fallback block.
                preview = "Invalid format string"; // Shows a friendly message instead of throwing an Inspector error.
            } // Closes the invalid-format fallback block.

            EditorGUI.HelpBox(rPrev, $"Preview: {preview}", MessageType.None); // Draws the final preview message in the Inspector.
        } // Closes the preview block.
    } // Closes the GUI drawing method body.

    private static int FindFormatIndex(string current) // Finds the dropdown index that matches the current format string.
    { // Opens the format-index helper body.
        for (int i = 0; i < Formats.Length - 1; i++) // Loops through preset formats while skipping the Custom placeholder.
        { // Opens the preset-search loop body.
            if (string.Equals(current, Formats[i], StringComparison.Ordinal)) // Compares the current value with this preset using an exact ordinal match.
                return i; // Returns the matching preset index.
        } // Closes the preset-search loop body.
        return Formats.Length - 1; // Returns the Custom index when no preset matches.
    } // Closes the format-index helper body.

    private static bool IsCustomFormat(string current) // Determines whether the current value is outside the preset formats.
    { // Opens the custom-format helper body.
        for (int i = 0; i < Formats.Length - 1; i++) // Loops through preset formats while skipping the Custom placeholder.
        { // Opens the preset-check loop body.
            if (string.Equals(current, Formats[i], StringComparison.Ordinal)) // Checks for an exact match with the current preset.
                return false; // Reports that the value is not custom because it matches a preset.
        } // Closes the preset-check loop body.
        return true; // Reports that the value is custom because no preset matched.
    } // Closes the custom-format helper body.
} // Closes the DateFormatAttributeDrawer class body.
#endif // Ends the editor-only drawer code.
