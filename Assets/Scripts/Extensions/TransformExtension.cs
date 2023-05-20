using UnityEngine;

/// <summary>
/// This class provides extension methods for transforms.
/// </summary>
public static class TransformExtension
{
    /// <summary>
    /// This extension method destroys all the children GameObjects.
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public static Transform Clear(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        return transform;
    }
}