using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRangeVizualizer : MonoBehaviour
{
    [SerializeField] private GameObject rangeCircleVisuals;
    [SerializeField] private float groundY;
    public void DrawRange(Vector3 origin, float radius)
    {
 
        rangeCircleVisuals.transform.position = new Vector3(origin.x, groundY, origin.z);
        rangeCircleVisuals.SetActive(true);
        rangeCircleVisuals.transform.localScale = new Vector3(radius * 2, rangeCircleVisuals.transform.localScale.y, radius * 2);
    }

    public void EraseRange()
    {
        rangeCircleVisuals.SetActive(false);
    }
}
