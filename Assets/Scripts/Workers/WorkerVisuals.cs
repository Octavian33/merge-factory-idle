using UnityEngine;

public static class WorkerVisuals
{
    public static Color GetColor(int level)
    {
        var t = Mathf.Repeat(level * 0.13f, 1f);
        return Color.HSVToRGB(t, 0.55f, 1f);
    }
}
