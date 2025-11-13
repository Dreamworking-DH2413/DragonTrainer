using UnityEngine;

[ExecuteAlways]
public class DistanceFogController : MonoBehaviour
{
    [Header("Distance (world units/meters)")]
    public float startDistance = 1000f; // 1 km
    public float endDistance = 1500f; // 1.5 km

    [Header("Fog Color")]
    public Color fogColor = new Color(0.75f, 0.8f, 0.85f);

    void OnEnable()
    {
        ApplyFogSettings();
    }

    void OnValidate()
    {
        if (endDistance < startDistance)
            endDistance = startDistance + 1f;

        ApplyFogSettings();
    }

    void ApplyFogSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = startDistance;
        RenderSettings.fogEndDistance = endDistance;
        RenderSettings.fogColor = fogColor;
    }

    void OnDisable()
    {
        // Turn fog off when this object is disabled
        // Comment this out if you want fog to stay on
        RenderSettings.fog = false;
    }
}
