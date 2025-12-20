using UnityEngine;
using System.Collections.Generic;

public class SmoothCameraPath : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera targetCamera;
    public Transform lookAtTarget;

    [Header("Path Settings")]
    public List<Transform> controlPoints = new List<Transform>();
    public float speed = 3f;
    public float rotationSpeed = 2f;
    public bool loop = true;
    public bool autoStart = true;

    [Range(0.1f, 2f)]
    public float tension = 0.5f; // Натяжение сплайна

    [Header("Debug")]
    [SerializeField, Range(0, 1)] private float progress;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private int segmentsBetweenPoints = 20;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (autoStart)
            StartMovement();
    }

    private void Update()
    {
        if (!isMoving || controlPoints.Count < 2 || targetCamera == null)
            return;

        UpdateCameraPosition();
        UpdateCameraRotation();

        progress += speed * Time.deltaTime / GetTotalPathLength();

        if (progress >= 1f)
        {
            if (loop)
                progress = 0f;
            else
                PauseMovement();
        }
    }

    private void UpdateCameraPosition()
    {
        float t = progress;
        targetCamera.transform.position = GetSplinePoint(t);
    }

    private void UpdateCameraRotation()
    {
        if (lookAtTarget == null) return;

        // Плавный поворот к цели
        Vector3 direction = lookAtTarget.position - targetCamera.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetCamera.transform.rotation = Quaternion.Slerp(
            targetCamera.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private Vector3 GetSplinePoint(float t)
    {
        int numSections = controlPoints.Count - (loop ? 0 : 3);
        int currPt = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);
        float u = t * numSections - currPt;

        Vector3 a = controlPoints[ClampListPos(currPt - 1)].position;
        Vector3 b = controlPoints[ClampListPos(currPt)].position;
        Vector3 c = controlPoints[ClampListPos(currPt + 1)].position;
        Vector3 d = controlPoints[ClampListPos(currPt + 2)].position;

        return 0.5f * (
            (-a + 3f * b - 3f * c + d) * (u * u * u) +
            (2f * a - 5f * b + 4f * c - d) * (u * u) +
            (-a + c) * u +
            2f * b
        );
    }

    private int ClampListPos(int pos)
    {
        if (loop)
        {
            return (pos + controlPoints.Count) % controlPoints.Count;
        }
        else
        {
            return Mathf.Clamp(pos, 0, controlPoints.Count - 1);
        }
    }

    private float GetTotalPathLength()
    {
        float length = 0f;
        int steps = 10;
        Vector3 prevPoint = GetSplinePoint(0);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 currPoint = GetSplinePoint(t);
            length += Vector3.Distance(prevPoint, currPoint);
            prevPoint = currPoint;
        }

        return length;
    }

    #region Public Controls
    public void StartMovement()
    {
        isMoving = true;
    }

    public void PauseMovement()
    {
        isMoving = false;
    }

    public void StopMovement()
    {
        isMoving = false;
        progress = 0f;
        if (targetCamera != null && controlPoints.Count > 0)
            targetCamera.transform.position = controlPoints[0].position;
    }

    public void AddControlPoint(Transform newPoint)
    {
        controlPoints.Add(newPoint);
    }

    public void RemoveControlPoint(int index)
    {
        if (index >= 0 && index < controlPoints.Count)
            controlPoints.RemoveAt(index);
    }
    #endregion

    #region Editor Visualization
    private void OnDrawGizmos()
    {
        if (controlPoints == null || controlPoints.Count < 2)
            return;

        Gizmos.color = Color.cyan;
        int steps = segmentsBetweenPoints * controlPoints.Count;

        Vector3 prevPoint = GetSplinePoint(0);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 currPoint = GetSplinePoint(t);
            Gizmos.DrawLine(prevPoint, currPoint);
            prevPoint = currPoint;
        }

        // Рисуем контрольные точки
        Gizmos.color = Color.yellow;
        foreach (var point in controlPoints)
        {
            if (point != null)
                Gizmos.DrawSphere(point.position, 0.2f);
        }
    }
    #endregion
}