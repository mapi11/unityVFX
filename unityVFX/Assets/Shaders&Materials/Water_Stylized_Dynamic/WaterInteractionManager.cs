using System.Collections.Generic;
using UnityEngine;

public class WaterInteractionManager : MonoBehaviour
{
    [Header("World mapping (XZ rectangle)")]
    public Vector2 areaMin = new Vector2(-25, -25);
    public Vector2 areaSize = new Vector2(50, 50);

    [Header("RT")]
    public RenderTexture interactionRT;
    public Material decayMaterial; // SG_WaterInteractionDecay material
    public Material stampMaterial; // SG_WaterInteractionStamp material
    [Range(0.90f, 0.999f)] public float decay = 0.985f;

    static readonly int ID_Decay = Shader.PropertyToID("_Decay");
    static readonly int ID_StampPos = Shader.PropertyToID("_StampPos");
    static readonly int ID_StampRadius = Shader.PropertyToID("_StampRadius");
    static readonly int ID_FoamAdd = Shader.PropertyToID("_FoamAdd");
    static readonly int ID_TrailAdd = Shader.PropertyToID("_TrailAdd");

    static readonly int ID_InteractionTex = Shader.PropertyToID("_InteractionTex");
    static readonly int ID_WaterAreaMin = Shader.PropertyToID("_WaterAreaMin");
    static readonly int ID_WaterAreaSize = Shader.PropertyToID("_WaterAreaSize");

    RenderTexture _temp;
    readonly List<WaterInteractor> _interactors = new();

    public void Register(WaterInteractor i) { if (!_interactors.Contains(i)) _interactors.Add(i); }
    public void Unregister(WaterInteractor i) { _interactors.Remove(i); }

    void OnEnable()
    {
        if (interactionRT == null)
        {
            Debug.LogError("Interaction RT is null.");
            enabled = false;
            return;
        }

        _temp = new RenderTexture(interactionRT.descriptor);
        _temp.Create();

        // Expose globals for water shader
        Shader.SetGlobalTexture(ID_InteractionTex, interactionRT);
        Shader.SetGlobalVector(ID_WaterAreaMin, new Vector4(areaMin.x, areaMin.y, 0, 0));
        Shader.SetGlobalVector(ID_WaterAreaSize, new Vector4(areaSize.x, areaSize.y, 0, 0));
    }

    void OnDisable()
    {
        if (_temp != null) _temp.Release();
    }

    void LateUpdate()
    {
        if (decayMaterial == null || stampMaterial == null) return;

        // 1) Decay pass
        decayMaterial.SetFloat(ID_Decay, decay);
        Graphics.Blit(interactionRT, _temp, decayMaterial);
        Graphics.Blit(_temp, interactionRT);

        // 2) Stamp all interactors (full-screen blit per object: ок, если объектов немного)
        for (int i = 0; i < _interactors.Count; i++)
        {
            var it = _interactors[i];
            if (it == null) continue;

            if (!WorldToUV(it.transform.position, out var uv))
                continue;

            stampMaterial.SetVector(ID_StampPos, uv);
            stampMaterial.SetFloat(ID_StampRadius, it.radiusUV);
            stampMaterial.SetFloat(ID_FoamAdd, it.foamAdd);
            stampMaterial.SetFloat(ID_TrailAdd, it.trailAdd);

            Graphics.Blit(interactionRT, _temp, stampMaterial);
            Graphics.Blit(_temp, interactionRT);
        }
    }

    bool WorldToUV(Vector3 worldPos, out Vector2 uv)
    {
        float u = (worldPos.x - areaMin.x) / Mathf.Max(0.0001f, areaSize.x);
        float v = (worldPos.z - areaMin.y) / Mathf.Max(0.0001f, areaSize.y);
        uv = new Vector2(u, v);
        return u >= 0 && u <= 1 && v >= 0 && v <= 1;
    }
}