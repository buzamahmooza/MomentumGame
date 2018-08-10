using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Utils
{
    /// <summary>
    /// cannot be instantiated
    /// </summary>
    private Utils()
    {
    }

    /// <summary> Returns true if the given LayerMask contains the given layer </summary>
    /// <param name="layerMask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool IsInLayerMask(LayerMask layerMask, int layer)
    {
        return (layerMask.value & 1 << layer) != 0;
    }

    /// <summary>
    /// returns a number that is safe to multiply with.
    /// Can be capped to an upper limit and is already capped to a minimum of 1.
    /// If value isNan, a default value of 1 is used.
    /// </summary>
    /// <param name="value">The unfiltered multiplier</param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static float FilterMultiplier(float value, float maxValue = float.MaxValue)
    {
        if (float.IsNaN(value)) 
            value = 1;
        
        return Mathf.Clamp(1 + Mathf.Abs(value), 1, maxValue);
    }

    public static T GetRandomElement<T>(T[] array)
    {
        if (array == null || array.Length == 0)
            throw new Exception("The array doesn't contain any elements.");
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    // TODO: finish this guy (not important)
    public static void DrawArrow(Vector3 from, Vector3 to, Color color)
    {
        Vector3 offset = to - from;
        Debug.DrawLine(from, to, color);
        Debug.DrawLine(to, to, color);
        Debug.DrawLine(to, to, color);
    }
}