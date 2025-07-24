using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color validPathColor;
    [SerializeField] private Color invalidPathColor;
 
    public void DrawValidPath(List<Vector3> points)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(validPathColor, 0f), new GradientColorKey(validPathColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1) }
        );
        lineRenderer.colorGradient = gradient;
        lineRenderer.material.color = validPathColor;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    public void DrawInvalidPath(List<Vector3> points)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(invalidPathColor, 0f), new GradientColorKey(invalidPathColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1) }
        );
        lineRenderer.colorGradient = gradient;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    public void ErasePathes()
    {
        lineRenderer.positionCount = 0;
    }
}
