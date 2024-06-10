// VRSYS plugin of Virtual Reality and Visualization Research Group (Bauhaus University Weimar)
//  _    ______  _______  _______
// | |  / / __ \/ ___/\ \/ / ___/
// | | / / /_/ /\__ \  \  /\__ \ 
// | |/ / _, _/___/ /  / /___/ / 
// |___/_/ |_|/____/  /_//____/  
//
//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2022 Virtual Reality and Visualization Research Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Ephraim Schott
//   Date:           2022
//-----------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class HandRay : MonoBehaviour
{
    public enum Hand
    {
        Left,
        Right
    }

    public enum RayState
    {
        None,
        Hover,
        Pressed
    }


    public Hand hand;
    public RayState rayState;
    public InputActionProperty hoverAction;
    public InputActionProperty clickAction;

    public bool hasTooltips = false;
    public TooltipHandler tooltipHandler;
    public Tooltip showRayTooltip;
    public Tooltip pressUITooltip;

    public bool isActive { get; private set; } = false;
    public float rayLength = 10.0f;

    //colors
    public Material hitNothingColor;
    public Material hitSomethingColor;

    private GameObject remoteRay;
    private GameObject remoteHit;

    private GameObject xrRayInteractorGO;
    private LineRenderer rayRenderer;
    private UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual xrLineVisual;
    private UnityEngine.XR.Interaction.Toolkit.XRRayInteractor xrRayInteractor;
    private UnityEngine.XR.Interaction.Toolkit.ActionBasedController xrController;

    // variables for intersection with UI and geometry 
    private bool uiHitFlag;
    private bool geoHitFlag;
    private UnityEngine.EventSystems.RaycastResult uiRaycastResult;
    private RaycastHit geometryRaycastResult;
    private int intersectionState = 0; // 0 = nothing; 1 = UI; 2 = geometry

    private Vector3 originPosition;
    private Vector3 hitPosition;

    private bool isInitialized = false;
    private bool isLocal = false;

    private void Awake()
    {
        Initialize();
    }


    // Start is called before the first frame update
    void Start()
    {
        if (isInitialized && isLocal)
        {
            if (hand == Hand.Left)
            {
                xrRayInteractorGO = Vrsys.Utility.FindRecursive(this.gameObject, "LeftHand Controller");
                xrController = xrRayInteractorGO.GetComponent<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
            }
            else if (hand == Hand.Right)
            {
                xrRayInteractorGO = Vrsys.Utility.FindRecursive(this.gameObject, "RightHand Controller");
                xrController = xrRayInteractorGO.GetComponent<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
            }
            xrLineVisual = xrRayInteractorGO.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual>();
            xrRayInteractor = xrRayInteractorGO.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRRayInteractor>();
            rayRenderer = xrRayInteractorGO.GetComponent<LineRenderer>();
            rayRenderer.startWidth = 0.01f;
            rayRenderer.positionCount = 2;
            rayRenderer.enabled = isActive;

            xrRayInteractorGO.SetActive(true);
            xrLineVisual.enabled = false;

            remoteHit = CreateDefaultHitVisualization("Test User");
            remoteHit.SetActive(false);

            // Setup tooltips
            if (tooltipHandler == null)
            {
                tooltipHandler = GetComponent<TooltipHandler>();
            }
            InitializeTooltips();
            
        }
    }

    private void OnDisable()
    {
        if (isInitialized)
        {
            if (remoteHit != null) remoteHit.SetActive(false);
            if (remoteRay != null) remoteRay.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (isInitialized)
        {
            if (remoteHit != null) Destroy(remoteHit);
            if (remoteRay != null) Destroy(remoteRay);
        }
    }

    private void OnEnable()
    {
        if (!isLocal)
        {
            if (remoteRay == null) remoteRay = CreateDefaultRayVisualization("Test User");
            remoteRay.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitialized && isLocal)
        {
            // clear
            uiHitFlag = false;
            geoHitFlag = false;
            intersectionState = 0;
            hitPosition = Vector3.zero;

            bool hoverValue = hoverAction.action.IsPressed();
            if (hoverValue && hoverValue != isActive)
            {
                isActive = true;
                rayRenderer.enabled = true;
                xrLineVisual.enabled = true;
                xrRayInteractor.maxRaycastDistance = 10;
                HandlePointing();
                rayState = RayState.Hover;
                tooltipHandler.HideTooltip(showRayTooltip);
            }
            else if (hoverValue && isActive)
            {
                HandlePointing();
            }
            else if (!hoverValue && hoverValue != isActive)
            {
                isActive = false;
                rayRenderer.enabled = false;
                xrLineVisual.enabled = false;
                xrRayInteractor.maxRaycastDistance = 0;
                rayState = RayState.None;
                remoteHit.SetActive(false);
                tooltipHandler.ShowTooltip(showRayTooltip, add: true);
            }
        }
        else if (isInitialized && !isLocal)
        {
            if (!rayRenderer.enabled && isActive) { rayRenderer.enabled = true; }
            else if (rayRenderer.enabled && !isActive) { rayRenderer.enabled = false; }
            if (isActive)
            {
                rayRenderer.positionCount = 2;
                rayRenderer.SetPosition(0, originPosition);
                rayRenderer.SetPosition(1, hitPosition);
            }
        }
    }

    void Initialize()
    {
        if (isActiveAndEnabled && !isInitialized)
        {
            isInitialized = true;
            isLocal = true;
        }
    }

    void InitializeTooltips()
    {
        if (hasTooltips)
        {
            TooltipHandler tooltipHandler = GetComponent<TooltipHandler>();
            tooltipHandler.ShowTooltip(showRayTooltip, add: true);
        }
    }

    void HandlePointing()
    {
        uiHitFlag = xrRayInteractor.TryGetCurrentUIRaycastResult(out uiRaycastResult);
        geoHitFlag = xrRayInteractor.TryGetCurrent3DRaycastHit(out geometryRaycastResult);
        // identify intersection state
        intersectionState = HandleIntersection();

        originPosition = xrRayInteractorGO.transform.position;
        switch (intersectionState)
        {
            case 0: // nothing hit
                hitPosition = new Vector3(0, 0, 0);
                rayRenderer.material = hitNothingColor;
                remoteHit.SetActive(false);
                tooltipHandler.HideTooltip(pressUITooltip);
                
                break;
            case 1: // UI hit
                hitPosition = uiRaycastResult.worldPosition;
                rayRenderer.material = hitSomethingColor;

                rayRenderer.positionCount = 2;
                rayRenderer.SetPosition(0, originPosition);
                rayRenderer.SetPosition(1, hitPosition);

                remoteHit.transform.position = hitPosition;
                remoteHit.SetActive(true);
                tooltipHandler.ShowTooltip(pressUITooltip);
                break;
            case 2: // geometry hit            
                hitPosition = geometryRaycastResult.point;
                rayRenderer.material = hitSomethingColor;

                rayRenderer.positionCount = 2;
                rayRenderer.SetPosition(0, originPosition);
                rayRenderer.SetPosition(1, hitPosition);

                remoteHit.transform.position = hitPosition;
                remoteHit.SetActive(true);
                tooltipHandler.HideTooltip(pressUITooltip);
                break;
        }
    }

    public Vector3? GetCurrentIntersectionPoint()
    {
        if (intersectionState == 1) // UI hit
            return uiRaycastResult.worldPosition;
        else if (intersectionState == 2) // geometry hit
            return geometryRaycastResult.point;
        else
            return null;
    }

    public int HandleIntersection()
    {
        // identify intersection state
        if (uiHitFlag && !geoHitFlag)
        {
            return 1;
        }
        else if (!uiHitFlag && geoHitFlag)
        {
            return 2;
        }
        else if (uiHitFlag && geoHitFlag) // both hit -> take nearest intersection
        {
            if (uiRaycastResult.distance <= geometryRaycastResult.distance)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
        else // nothing hit
        {
            return 0;
        }
    }

    private GameObject CreateDefaultHitVisualization(string userName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.name = userName + " PointingRay Hit";
        go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        go.GetComponent<MeshRenderer>().material.color = Color.red;
        go.GetComponent<SphereCollider>().enabled = false;
        return go;
    }

    private GameObject CreateDefaultRayVisualization(string userName)
    {
        var go = new GameObject("Ray Visualization");
        if (hand == Hand.Left)
        {
            go.name = userName + " Left Hand Ray";
        }
        else if (hand == Hand.Right)
        {
            go.name = userName + " Right Hand Ray";
        }
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        LineRenderer r = go.AddComponent<LineRenderer>();
        r.startWidth = 0.01f;
        r.positionCount = 2;
        return go;
    }
}
