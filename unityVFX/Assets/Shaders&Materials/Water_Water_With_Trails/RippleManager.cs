using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class RippleManager : MonoBehaviour
{
    public static RippleManager Instance { get; private set; }

    [Header("World mapping (water area)")]
    public Vector2 worldOriginXZ = new Vector2(-5f, -5f); // низ-лево области воды в мире (X,Z)
    public Vector2 worldSizeXZ = new Vector2(10f, 10f); // размер области воды в мире (X,Z)
    public Vector2 uvOffset = Vector2.zero;          // если нужно сдвигать UV в RT

    [Header("RenderTexture")]
    public int rtSize = 512;
    public FilterMode filter = FilterMode.Bilinear;

    [Header("Materials (Hidden shaders)")]
    public Material matDecay; // HiddenRippleDecay
    public Material matSplat; // HiddenRippleSplat

    [Header("Decay params")]
    [Range(0f, 1f)] public float decay = 0.02f; // скорость затухания
    public float decayTo = 0.0f;                // к чему стремится (обычно 0)

    // ВАЖНО: глобальные имена
    static readonly int _propRippleRT = Shader.PropertyToID("_RippleRT");
    static readonly int _propRipleRT = Shader.PropertyToID("_RipleRT"); // на случай опечатки в графе

    static readonly int _propMainTex = Shader.PropertyToID("_MainTex");

    // Splat shader props
    static readonly int _propCenter = Shader.PropertyToID("_Center");
    static readonly int _propRadius = Shader.PropertyToID("_Radius");
    static readonly int _propStrength = Shader.PropertyToID("_Strength");
    static readonly int _propHardness = Shader.PropertyToID("_Hardness");

    // Decay shader props
    static readonly int _propDecay = Shader.PropertyToID("_Decay");
    static readonly int _propDecayTo = Shader.PropertyToID("_DecayTo");

    RenderTexture _rtA;
    RenderTexture _rtB;

    readonly List<RippleEmitter> _emitters = new List<RippleEmitter>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        CreateRTs();
        ClearRT(_rtA, 0f);
        ClearRT(_rtB, 0f);

        PushGlobal(_rtA);

        // найдём все эмиттеры
        _emitters.Clear();
        _emitters.AddRange(FindObjectsOfType<RippleEmitter>(true));
    }

    void OnDisable()
    {
        ReleaseRTs();
    }

    void CreateRTs()
    {
        var desc = new RenderTextureDescriptor(rtSize, rtSize);
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;
        desc.graphicsFormat = GraphicsFormat.R16_SFloat; // один канал, float
        desc.sRGB = false;

        _rtA = new RenderTexture(desc) { name = "_RippleRT_A", filterMode = filter, wrapMode = TextureWrapMode.Clamp };
        _rtB = new RenderTexture(desc) { name = "_RippleRT_B", filterMode = filter, wrapMode = TextureWrapMode.Clamp };

        _rtA.Create();
        _rtB.Create();
    }

    void ReleaseRTs()
    {
        if (_rtA) { _rtA.Release(); Destroy(_rtA); }
        if (_rtB) { _rtB.Release(); Destroy(_rtB); }
        _rtA = null;
        _rtB = null;
    }

    void ClearRT(RenderTexture rt, float value)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, new Color(value, 0, 0, 0));
        RenderTexture.active = prev;
    }

    void PushGlobal(RenderTexture rt)
    {
        Shader.SetGlobalTexture(_propRippleRT, rt);
        Shader.SetGlobalTexture(_propRipleRT, rt); // на случай опечатки
    }

    void Update()
    {
        if (!_rtA || !_rtB || !matDecay || !matSplat) return;

        // ping-pong: читаем из _rtA, пишем в _rtB
        // 1) decay pass
        matDecay.SetFloat(_propDecay, decay);
        matDecay.SetFloat(_propDecayTo, decayTo);
        Graphics.Blit(_rtA, _rtB, matDecay);

        // 2) splat pass (можно несколько раз)
        foreach (var e in _emitters)
        {
            if (!e) continue;

            if (!e.TryGetSplat(out var uv, out var radius, out var strength, out var hardness))
                continue;

            matSplat.SetVector(_propCenter, new Vector4(uv.x, uv.y, 0, 0));
            matSplat.SetFloat(_propRadius, radius);
            matSplat.SetFloat(_propStrength, strength);
            matSplat.SetFloat(_propHardness, hardness);

            // additive splat поверх текущего содержимого _rtB
            var tmp = RenderTexture.GetTemporary(_rtB.descriptor);
            Graphics.Blit(_rtB, tmp); // копия текущего
            Graphics.Blit(tmp, _rtB, matSplat);
            RenderTexture.ReleaseTemporary(tmp);
        }

        // swap
        var t = _rtA;
        _rtA = _rtB;
        _rtB = t;

        PushGlobal(_rtA);
    }

    // --- World -> UV mapping ---

    public Vector2 WorldToUV(Vector3 worldPos)
    {
        float u = (worldPos.x - worldOriginXZ.x) / Mathf.Max(0.0001f, worldSizeXZ.x);
        float v = (worldPos.z - worldOriginXZ.y) / Mathf.Max(0.0001f, worldSizeXZ.y);
        return new Vector2(u, v) + uvOffset;
    }

    // Версия с проверкой "внутри ли воды"
    public bool WorldToUV(Vector3 worldPos, out Vector2 uv)
    {
        uv = WorldToUV(worldPos);
        return (uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f);
    }
}
