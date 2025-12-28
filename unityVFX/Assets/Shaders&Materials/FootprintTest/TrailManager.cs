using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-10000)] // чтобы TrailManager.Update шёл раньше эмиттеров
public class TrailManager : MonoBehaviour
{
    [Header("Plane / Material target")]
    public Renderer targetRenderer;
    public int materialIndex = 0;

    [Header("World mapping (XZ -> UV)")]
    public bool autoFromRendererBounds = true;
    public Vector2 worldSize = new Vector2(10f, 10f);
    public Vector2 worldCenterXZ = Vector2.zero;

    [Header("Mapping fixes (Final sampling in Footprint shader)")]
    public bool flipV = true; // управляет _TrailFlipV в основном ShaderGraph (FootprintTest)
    public bool showRTInMaterialInspector = false; // только для дебага

    [Header("Blit fixes (Decay/Splat blit shaders)")]
    public bool blitFlipY = true; // управляет _BlitFlipY в TrailRT_Decay_URP и TrailRT_Splat_URP

    [Header("RenderTexture")]
    [Range(64, 2048)] public int resolution = 512;
    public RenderTextureFormat format = RenderTextureFormat.ARGB32;
    public FilterMode filterMode = FilterMode.Bilinear;

    [Header("Blit materials")]
    public Material decayMaterial;   // TrailRT_Decay_URP
    public Material splatMaterial;   // TrailRT_Splat_URP

    [Header("Decay")]
    [Range(0.80f, 1.0f)] public float fade = 0.97f;
    public bool enableDecay = true;

    [Header("Debug")]
    public bool debugDrawRT = true;
    public Vector2 debugDrawPos = new Vector2(10, 10);
    public float debugDrawSize = 256;

    RenderTexture _rtA;
    RenderTexture _rtB;
    MaterialPropertyBlock _mpb;

    static readonly int ID_TrailRT = Shader.PropertyToID("_TrailRT");
    static readonly int ID_TrailWorldSize = Shader.PropertyToID("_TrailWorldSize");
    static readonly int ID_TrailCenterXZ = Shader.PropertyToID("_TrailCenterXZ");
    static readonly int ID_TrailFlipV = Shader.PropertyToID("_TrailFlipV");

    // Важно: это мы добавим в blit-шейдеры (Decay/Splat)
    static readonly int ID_BlitFlipY = Shader.PropertyToID("_BlitFlipY");

    static readonly int ID_Fade = Shader.PropertyToID("_Fade");
    static readonly int ID_SplatUV = Shader.PropertyToID("_SplatUV");
    static readonly int ID_Radius = Shader.PropertyToID("_Radius");
    static readonly int ID_Strength = Shader.PropertyToID("_Strength");
    static readonly int ID_Hardness = Shader.PropertyToID("_Hardness");

    void Awake()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        CreateRTs();
        Clear();

        UpdateWorldMapping();
        PushToPlane();
    }

    void OnDisable()
    {
        ReleaseRTs();
    }

    void OnValidate()
    {
        // OnValidate вызывается ДО Awake — поэтому mpb может быть null
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        if (!Application.isPlaying) return;

        if (_rtA == null && enabled)
        {
            CreateRTs();
            Clear();
        }

        UpdateWorldMapping();
        PushToPlane();
    }

    void Update()
    {
        if (_rtA == null) return;

        UpdateWorldMapping();

        // Если fade == 1, decay делать НЕ НАДО: это пустой ping-pong blit, который часто и даёт переворот/мерцание
        if (enableDecay && fade < 0.9999f)
            DoDecay();
    }

    void LateUpdate()
    {
        // В LateUpdate — чтобы точно после всех AddSplatWorld в этом кадре
        if (_rtA == null) return;
        PushToPlane();
    }

    void UpdateWorldMapping()
    {
        if (!autoFromRendererBounds || targetRenderer == null) return;

        var b = targetRenderer.bounds;
        worldCenterXZ = new Vector2(b.center.x, b.center.z);
        worldSize = new Vector2(
            Mathf.Max(0.0001f, b.size.x),
            Mathf.Max(0.0001f, b.size.z)
        );
    }

    void CreateRTs()
    {
        ReleaseRTs();

        _rtA = new RenderTexture(resolution, resolution, 0, format, RenderTextureReadWrite.Linear);
        _rtA.name = "TrailRT_A";
        _rtA.filterMode = filterMode;
        _rtA.wrapMode = TextureWrapMode.Clamp;
        _rtA.useMipMap = false;
        _rtA.autoGenerateMips = false;
        _rtA.Create();

        _rtB = new RenderTexture(resolution, resolution, 0, format, RenderTextureReadWrite.Linear);
        _rtB.name = "TrailRT_B";
        _rtB.filterMode = filterMode;
        _rtB.wrapMode = TextureWrapMode.Clamp;
        _rtB.useMipMap = false;
        _rtB.autoGenerateMips = false;
        _rtB.Create();
    }

    void ReleaseRTs()
    {
        ReleaseOne(ref _rtA);
        ReleaseOne(ref _rtB);
    }

    void ReleaseOne(ref RenderTexture rt)
    {
        if (rt == null) return;

        rt.Release();
        if (Application.isPlaying) Destroy(rt);
        else DestroyImmediate(rt);
        rt = null;
    }

    void Clear()
    {
        if (_rtA == null) return;

        var prev = RenderTexture.active;

        RenderTexture.active = _rtA;
        GL.Clear(false, true, Color.black);

        RenderTexture.active = _rtB;
        GL.Clear(false, true, Color.black);

        RenderTexture.active = prev;
    }

    void BlitSRP(RenderTexture src, RenderTexture dst, Material mat)
    {
        if (src == null || dst == null || mat == null) return;

        mat.SetTexture("_BlitTexture", src);
        mat.SetTexture("_MainTex", src); // запасной вариант

        // Это заработает только если ты добавишь float _BlitFlipY в blit-шейдеры
        mat.SetFloat(ID_BlitFlipY, blitFlipY ? 1f : 0f);

        var cmd = CommandBufferPool.Get("TrailRT_Blit");
        cmd.Blit(src, dst, mat);
        Graphics.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void DoDecay()
    {
        if (decayMaterial == null) return;

        decayMaterial.SetFloat(ID_Fade, fade);
        BlitSRP(_rtA, _rtB, decayMaterial);
        SwapRT();
    }

    void SwapRT()
    {
        var t = _rtA;
        _rtA = _rtB;
        _rtB = t;
    }

    void PushToPlane()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (targetRenderer == null) return;

        var mats = targetRenderer.sharedMaterials;
        if (mats == null || mats.Length == 0) return;

        materialIndex = Mathf.Clamp(materialIndex, 0, mats.Length - 1);

        targetRenderer.GetPropertyBlock(_mpb, materialIndex);

        _mpb.SetTexture(ID_TrailRT, _rtA);
        _mpb.SetVector(ID_TrailWorldSize, worldSize);
        _mpb.SetVector(ID_TrailCenterXZ, worldCenterXZ);
        _mpb.SetFloat(ID_TrailFlipV, flipV ? 1f : 0f);

        targetRenderer.SetPropertyBlock(_mpb, materialIndex);

        // Только чтобы видеть RT в инспекторе материала (это НЕ обязательно)
        if (showRTInMaterialInspector)
        {
            var mat = mats[materialIndex];
            if (mat != null)
            {
                mat.SetTexture(ID_TrailRT, _rtA);
                mat.SetVector(ID_TrailWorldSize, worldSize);
                mat.SetVector(ID_TrailCenterXZ, worldCenterXZ);
                mat.SetFloat(ID_TrailFlipV, flipV ? 1f : 0f);
            }
        }
    }

    public bool WorldToUV(Vector3 worldPos, out Vector2 uv01)
    {
        float u = (worldPos.x - worldCenterXZ.x) / worldSize.x + 0.5f;
        float v = (worldPos.z - worldCenterXZ.y) / worldSize.y + 0.5f;

        uv01 = new Vector2(u, v);
        return (u >= 0f && u <= 1f && v >= 0f && v <= 1f);
    }

    public void AddSplatWorld(Vector3 worldPos, float radiusWorld, float strength = 1f, float hardness = 4f)
    {
        if (_rtA == null || splatMaterial == null) return;
        if (!WorldToUV(worldPos, out var uv)) return;

        float denom = Mathf.Max(0.0001f, Mathf.Max(worldSize.x, worldSize.y));
        float radiusUV = radiusWorld / denom;

        AddSplatUV(uv, radiusUV, strength, hardness);
    }

    public void AddSplatUV(Vector2 uv01, float radiusUV, float strength = 1f, float hardness = 4f)
    {
        if (_rtA == null || splatMaterial == null) return;

        splatMaterial.SetVector(ID_SplatUV, uv01);
        splatMaterial.SetFloat(ID_Radius, Mathf.Max(0.0001f, radiusUV));
        splatMaterial.SetFloat(ID_Strength, strength);
        splatMaterial.SetFloat(ID_Hardness, hardness);

        BlitSRP(_rtA, _rtB, splatMaterial);
        SwapRT();
    }

    void OnGUI()
    {
        if (!debugDrawRT || _rtA == null) return;

        GUI.DrawTexture(
            new Rect(debugDrawPos.x, debugDrawPos.y, debugDrawSize, debugDrawSize),
            _rtA,
            ScaleMode.ScaleToFit,
            false
        );
    }

    [ContextMenu("DEBUG: Clear RT")]
    void DebugClear()
    {
        Clear();
        PushToPlane();
    }
}
