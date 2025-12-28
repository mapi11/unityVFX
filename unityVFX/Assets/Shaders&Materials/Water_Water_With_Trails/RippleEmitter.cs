using UnityEngine;

public class RippleEmitter : MonoBehaviour
{
    [Header("Splat params")]
    [SerializeField] float radius = 0.25f;      // в UV (примерно 0.05..0.5)
    [SerializeField] float strength = 1.0f;     // сила следа
    [SerializeField] float hardness = 8.0f;     // резкость пятна (чем больше, тем жёстче край)

    [Header("Emission")]
    [SerializeField] float minMoveDistance = 0.01f; // чтобы не спамить сплаты от микродрожи
    [SerializeField] bool requireManagerBounds = true; // если true — рисуем только когда объект в зоне воды

    Vector3 _prevPos;
    bool _hasPrev;

    void OnEnable()
    {
        _hasPrev = false;
    }

    public bool TryGetSplat(out Vector2 uv, out float outRadius, out float outStrength, out float outHardness)
    {
        uv = default;
        outRadius = radius;
        outStrength = strength;
        outHardness = hardness;

        var mgr = RippleManager.Instance;
        if (!mgr) return false;

        var pos = transform.position;

        if (!_hasPrev)
        {
            _prevPos = pos;
            _hasPrev = true;
            return false;
        }

        float moved = (pos - _prevPos).magnitude;
        _prevPos = pos;

        if (moved < minMoveDistance) return false;

        if (requireManagerBounds)
        {
            if (!mgr.WorldToUV(pos, out uv))
                return false;
        }
        else
        {
            uv = mgr.WorldToUV(pos);
        }

        return true;
    }
}
