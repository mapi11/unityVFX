using UnityEngine;

public class SimpleBobbing : MonoBehaviour
{
    [SerializeField] private float _amplitude = 0.08f; // высота (метры)
    [SerializeField] private float _speed = 1.0f;      // скорость
    [SerializeField] private float _offset = 0.0f;     // сдвиг фазы (если нужно)

    private float _startY;

    private void Awake()
    {
        _startY = transform.position.y;
    }

    private void Update()
    {
        float y = _startY + Mathf.Sin(Time.time * _speed + _offset) * _amplitude;
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }
}