using UnityEngine;

public class LookAt : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void Start()
    {

    }

    private void LateUpdate()
    {
        if (_target == null) return;
        transform.LookAt(_target);
    }
}