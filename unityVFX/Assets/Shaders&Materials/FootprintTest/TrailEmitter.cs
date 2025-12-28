using UnityEngine;

public class TrailEmitter : MonoBehaviour
{
    public TrailManager manager;

    [Header("Raycast")]
    public float rayHeight = 1f;
    public float rayDistance = 2f;
    public LayerMask planeLayer;

    [Header("Splat params")]
    public float radiusWorld = 0.5f;
    public float strength = 2f;
    public float hardness = 4f;

    [Header("Rate limit")]
    public float minMove = 0.02f;
    public float minInterval = 0.02f;

    [Header("Fill gaps")]
    public bool fillGaps = true;
    public int maxSubSteps = 16;

    [Header("Debug")]
    public bool logHit = false;

    Vector3 _lastHit;
    bool _hasLast;
    float _lastEmitTime;

    void Update()
    {
        if (manager == null) return;

        if (!TryGetHitPoint(out var hitPoint))
            return;

        float now = Time.time;

        if (!_hasLast)
        {
            EmitAt(hitPoint);
            _lastHit = hitPoint;
            _lastEmitTime = now;
            _hasLast = true;
            return;
        }

        float dt = now - _lastEmitTime;

        Vector2 a = new Vector2(_lastHit.x, _lastHit.z);
        Vector2 b = new Vector2(hitPoint.x, hitPoint.z);
        float dist = Vector2.Distance(a, b);

        if (minInterval > 0f && dt < minInterval) return;
        if (minMove > 0f && dist < minMove) return;

        if (fillGaps)
            EmitAlong(_lastHit, hitPoint);
        else
            EmitAt(hitPoint);

        _lastHit = hitPoint;
        _lastEmitTime = now;
    }

    bool TryGetHitPoint(out Vector3 hitPoint)
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Ray ray = new Ray(origin, Vector3.down);

        if (Physics.Raycast(ray, out var hit, rayHeight + rayDistance, planeLayer, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;
            if (logHit) Debug.Log($"[TrailEmitter] Hit at: {hitPoint}");
            return true;
        }

        hitPoint = default;
        return false;
    }

    void EmitAt(Vector3 worldPoint)
    {
        manager.AddSplatWorld(worldPoint, radiusWorld, strength, hardness);
    }

    void EmitAlong(Vector3 from, Vector3 to)
    {
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(to.x, to.z);
        float dist = Vector2.Distance(a, b);

        float step = Mathf.Max(0.0001f, radiusWorld * 0.5f);
        int steps = Mathf.Clamp(Mathf.CeilToInt(dist / step), 1, maxSubSteps);

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector3 p = Vector3.Lerp(from, to, t);
            EmitAt(p);
        }
    }
}
