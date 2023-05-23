using System;
using UnityEngine;

/// <summary>
/// This class allows to show range with steps onGUI in Unity Editor
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class RangeExtensionAttribute : PropertyAttribute
{
    public readonly float min;
    public readonly float max;
    public readonly float step;

    public RangeExtensionAttribute(float min, float max, float step)
    {
        this.min = min;
        this.max = max;
        this.step = step;
    }
}