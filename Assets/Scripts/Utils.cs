using UnityEngine;

public class Utils
{
    /// <summary>
    /// cannot be instantiated
    /// </summary>
    private Utils() { }

    /// <summary> Returns true if the given LayerMask contains the given layer </summary>
    /// <param name="layerMask"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public static bool IsInLayerMask(LayerMask layerMask, int layer) {
        return (layerMask.value & 1 << layer) != 0;
    }
}
