using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// This class allows to show range with steps onGUI in Unity Editor
/// </summary>
[CustomPropertyDrawer(typeof(RangeExtensionAttribute))]
internal sealed class RangeExDrawer : PropertyDrawer
{

    /**
     * Return exact precision of reel decimal
     * ex :
     * 0.01     = 2 digits
     * 0.02001  = 5 digits
     * 0.02000  = 2 digits
     */
    private int Precision(float value)
    {
        int _precision;
        if (value == .0f) return 0;
        _precision = value.ToString().Length - (((int)value).ToString().Length + 1);
        // Math.Round function get only precision between 0 to 15
        return Mathf.Clamp(_precision, 0, 15);
    }

    /**
     * Return new float value with step calcul (and step decimal precision)
     */
    private float Step(float value, float min, float step)
    {
        if (step == 0) return value;
        float newValue = min + Mathf.Round((value - min) / step) * step;
        return (float)Math.Round(newValue, Precision(step));
    }

    /**
     * Return new integer value with step calcul
     * (It's more simple ^^)
     */
    private int Step(int value, float step)
    {
        if (step == 0) return value;
        value -= (value % (int)Math.Round(step));
        return value;
    }

    //
    // Methods
    //
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var rangeAttribute = (RangeExtensionAttribute)base.attribute;

        switch (property.propertyType)
        {
            case SerializedPropertyType.Float:
                float _floatValue = EditorGUI.Slider(position, label, property.floatValue, rangeAttribute.min, rangeAttribute.max);
                property.floatValue = Step(_floatValue, rangeAttribute.min, rangeAttribute.step);
                break;
            case SerializedPropertyType.Integer:
                int _intValue = EditorGUI.IntSlider(position, label, property.intValue, (int)rangeAttribute.min, (int)rangeAttribute.max);
                property.intValue = Step(_intValue, rangeAttribute.step);
                break;
            default:
                EditorGUI.LabelField(position, label.text, "Use Range with float or int.");
                break;
        }
    }
}