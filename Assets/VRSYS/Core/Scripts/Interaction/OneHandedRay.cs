using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Vrsys;

[RequireComponent(typeof(RayHandler))]
public class OneHandedRay : MonoBehaviourPun
{
    public InputActionProperty teleportAction;
    public InputActionProperty toggleRayAction;
    private ViewingSetupHMDAnatomy hmd;
    private AvatarHMDAnatomy hmdAnatomy;
    private RayHandler rayHandler;
    private bool rayActivated, _switch, _shouldActivate = false;
    private XRRayInteractor _interactor;

    private TooltipHandler _tooltipHandler;
    private Tooltip rayTooltip;
    private Tooltip selectionTooltip;
    
    // Start is called before the first frame update
    void Start()
    {
        _tooltipHandler = NetworkUser.localGameObject.GetComponent<TooltipHandler>();
        
        rayTooltip = new Tooltip();
        rayTooltip.hand = TooltipHand.Right;
        rayTooltip.tooltipName = "Ray Toggle";
        rayTooltip.tooltipText = "Ray ON";
        rayTooltip.actionButtonReference = Tooltip.ActionButton.PrimaryButton;
        //_tooltipHandler.AddTooltip(rayTooltip);
        _tooltipHandler.AddBaseTooltip(rayTooltip);
        
        selectionTooltip = new Tooltip();
        selectionTooltip.hand = TooltipHand.Right;
        selectionTooltip.tooltipName = "RaySelect";
        selectionTooltip.tooltipText = "RaySelect";
        selectionTooltip.actionButtonReference = Tooltip.ActionButton.Trigger;
        _tooltipHandler.AddTooltip(selectionTooltip);
        _tooltipHandler.HideTooltip(selectionTooltip);
    }

    private void Awake()
    {
        rayHandler = GetComponent<RayHandler>();
        teleportAction.action.Enable();
        toggleRayAction.action.Enable();
    }

    private void OnEnable()
    {
        if (EnsureViewingSetup())
        {
            XRDirectInteractor[] interactors = hmd.leftController.GetComponentsInChildren<XRDirectInteractor>(true);
            foreach(var interactor in interactors)
                interactor.enabled = false;

            interactors = hmd.rightController.GetComponentsInChildren<XRDirectInteractor>(true);
            foreach(var interactor in interactors)
                interactor.enabled = false;
            
            XRRayInteractor[] rayInteractors = hmd.leftController.GetComponentsInChildren<XRRayInteractor>(true);
            foreach(var interactor in rayInteractors)
                interactor.enabled = false;

            _interactor = hmd.rightController.GetComponentInChildren<XRRayInteractor>(true);
        }
    }

    private void OnDisable()
    {
        if (EnsureViewingSetup())
        {
            XRDirectInteractor[] interactors = hmd.leftController.GetComponentsInChildren<XRDirectInteractor>(true);
            foreach(var interactor in interactors)
                interactor.enabled = true;

            interactors = hmd.rightController.GetComponentsInChildren<XRDirectInteractor>(true);
            foreach(var interactor in interactors)
                interactor.enabled = true;
            
            XRRayInteractor[] rayInteractors = hmd.leftController.GetComponentsInChildren<XRRayInteractor>(true);
            foreach(var interactor in rayInteractors)
                interactor.enabled = true;
            
            if (hmd.rightController != null)
            {
                if (rayActivated)
                {
                    rayHandler.DeactivateRay(RayHandler.Hand.Right);
                    rayActivated = false;
                }
            }
        }
    }

    private void OnDestroy()
    {
        OnDisable();
    }
    
    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine && hmd.rightController != null)
        {
            float trigger = teleportAction.action.ReadValue<float>();

            if (trigger > 0.03f) 
            {
                if (rayActivated)
                {
                    rayHandler.DeactivateRay(RayHandler.Hand.Right);
                    rayActivated = false;
                    _shouldActivate = true;
                }
            }
            else
            {
                _shouldActivate = _shouldActivate || toggleRayAction.action.triggered;
                if (_shouldActivate)
                {
                    rayActivated = !rayActivated;
                    _switch = true;
                    _shouldActivate = false;
                }

                if (_switch)
                {
                    if (rayActivated)
                    {
                        rayHandler.ActivateRay(RayHandler.Hand.Right);
                        _tooltipHandler.ShowTooltip(selectionTooltip);
                        _tooltipHandler.UpdateTooltipText(rayTooltip, "Ray OFF");
                    }
                    else
                    {
                        rayHandler.DeactivateRay(RayHandler.Hand.Right);
                        _tooltipHandler.HideTooltip(selectionTooltip);
                        _tooltipHandler.UpdateTooltipText(rayTooltip, "Ray ON");
                    }

                    _interactor.enabled = rayActivated;
                    _switch = false;
                }

                if (rayActivated)
                {
                    rayHandler.ActivateRay(RayHandler.Hand.Right);
                    Transform right = hmd.rightController.transform;
                    Vector3 rightStart = right.position;
                    Vector3 rightDir = right.TransformDirection(Vector3.forward).normalized;
                    Vector3 rightEnd = rightStart + 30.0f * rightDir;
                    RaycastHit hit;
                    Vector3 pos, normal;
                    int a;
                    bool h;
                    _interactor.TryGetHitInfo(out pos, out normal, out a, out h);
                    rightEnd = pos;

                    //if (Physics.Raycast(hmd.rightController.transform.position, hmd.rightController.transform.TransformDirection(Vector3.forward),
                    //        out hit, 100, ~0))
                    //{
                    //    rightEnd = rightStart + hit.distance * rightDir;
                    //}

                    rayHandler.ActivateIntersectionSphere(RayHandler.Hand.Right);
                    rayHandler.UpdateRay(RayHandler.Hand.Right, rightEnd, RayHandler.RayEnd.Other);
                }
                else
                {
                    //_tooltipHandler.HideTooltip(selectionTooltip);
                    //rayTooltip.tooltipText = "Ray ON";
                }
            }
        } 
    }
    
    bool EnsureViewingSetup() 
    {
        if(hmd != null)
            return true;

        NetworkUser networkUser = GetComponent<NetworkUser>();

        if (networkUser == null)
            return false;
        
        if (GetComponent<NetworkUser>().viewingSetupAnatomy == null)
        {
            hmdAnatomy = GetComponent<AvatarAnatomy>() as AvatarHMDAnatomy;
            return false;
        }

        if(!(GetComponent<NetworkUser>().viewingSetupAnatomy is ViewingSetupHMDAnatomy))
            return false;

        hmd = GetComponent<NetworkUser>().viewingSetupAnatomy as ViewingSetupHMDAnatomy;

        return true;
    }
}
