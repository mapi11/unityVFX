using UnityEngine;

public class WaterInteractor : MonoBehaviour
{
    public WaterInteractionManager manager;

    [Header("Stamp in UV space")]
    [Range(0.001f, 0.2f)] public float radiusUV = 0.03f;

    [Header("Adds")]
    [Range(0f, 2f)] public float foamAdd = 0.8f;
    [Range(0f, 2f)] public float trailAdd = 0.6f;

    void OnEnable()
    {
        if (manager != null) manager.Register(this);
    }

    void OnDisable()
    {
        if (manager != null) manager.Unregister(this);
    }
}