using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineRendererPositionChecker : MonoBehaviour
{
    public float threshold = 0.1f; // Distance threshold to consider as "close to zero"
    public float forwardMultiplier = 30.0f; // Multiplier for the forward direction

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        if(lineRenderer == null)
            return;
        
        if (lineRenderer.positionCount < 2)
            return;

        Vector3 secondPosition = lineRenderer.GetPosition(1);
        if (Vector3.Distance(secondPosition, Vector3.zero) < threshold)
        {
            Vector3 newPosition = transform.position + transform.forward * forwardMultiplier;
            lineRenderer.SetPosition(1, newPosition);
        }
    }
}