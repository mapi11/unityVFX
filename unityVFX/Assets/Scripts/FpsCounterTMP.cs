using TMPro;
using UnityEngine;

public sealed class FpsCounterTMP : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text _text;

    [Header("Update")]
    [Tooltip("Как часто обновлять текст (сек). 0.05 = ~20 раз/сек")]
    [SerializeField] private float _updateInterval = 0.05f;

    [Tooltip("Показывать ещё и время кадра (ms)")]
    [SerializeField] private bool _showMs = false;

    private int _frames;
    private float _timeAcc;

    private void Awake()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        _updateInterval = Mathf.Max(0.01f, _updateInterval);
    }

    private void Update()
    {
        _frames++;
        _timeAcc += Time.unscaledDeltaTime;

        if (_timeAcc < _updateInterval) return;

        float fps = _frames / _timeAcc;

        if (_showMs)
        {
            float ms = (_timeAcc / _frames) * 1000f;
            _text.SetText("{0:0} FPS\n{1:0.0} ms", fps, ms);
        }
        else
        {
            _text.SetText("{0:0} FPS", fps);
        }

        _frames = 0;
        _timeAcc = 0f;
    }
}