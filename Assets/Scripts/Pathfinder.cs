using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Pathfinder
{
    public bool TryCalculatePath(Vector3 origin, Vector3 destination, float maxPathLength, out List<Vector3> pathPoints)
    {
        pathPoints = new List<Vector3>();
        NavMeshPath path = new NavMeshPath();
        bool pathBuilt = NavMesh.CalculatePath(origin, destination, NavMesh.AllAreas, path);
        if (!pathBuilt)
        {
            return false;
        }
        pathPoints.Add(path.corners[0]);
        for(int i = 1; i < path.corners.Length; i++)
        {
            float distanceToPrevious = Vector3.Distance(path.corners[i], path.corners[i-1]);
            maxPathLength -= distanceToPrevious;
            if( maxPathLength < 0)
            {
                pathPoints.Add(path.corners[i] + (path.corners[i-1] - path.corners[i]).normalized * Math.Abs(maxPathLength));
                return false;
            }
            pathPoints.Add(path.corners[i]);
        }
        return true;
    }
}
