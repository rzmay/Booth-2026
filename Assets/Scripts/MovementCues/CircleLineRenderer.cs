using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class CircleLineRenderer : MonoBehaviour
{
    [Min(3)]
    public int resolution = 32;

    public float radius = 1f;

    public bool updateContinuously = true;

    private LineRenderer lineRenderer;

    public Func<float, float> OffsetFunction { get; set; }

    private void OnEnable()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
        UpdateCircle();
    }

    private void OnValidate()
    {
        resolution = Mathf.Max(3, resolution);

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.loop = true;
        UpdateCircle();
    }

    private void Update()
    {
        if (updateContinuously)
            UpdateCircle();
    }

    public void UpdateCircle()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / resolution;     // 0–1
            float angle = t * Mathf.PI * 2f;

            float currentRadius = radius;

            if (OffsetFunction != null)
                currentRadius += OffsetFunction(t);

            float x = Mathf.Cos(angle) * currentRadius;
            float y = Mathf.Sin(angle) * currentRadius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
    public void SetOffsetFunction(Func<float, float> offsetFunc)
    {
        OffsetFunction = offsetFunc;
    }

    public void ClearOffsetFunction()
    {
        OffsetFunction = null;
    }
}
