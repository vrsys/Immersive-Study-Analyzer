using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[CanSelectMultiple(false)]
[RequireComponent(typeof(MaterialHandler))]
[RequireComponent(typeof(SelectableDistributionHandler))]
public class SimpleInteractable : XRBaseInteractable
{
    public bool manipulationActivated = true;
    private bool isLocallySelected = false;
    private SelectableDistributionHandler selectableDistributionHandler;
    private MaterialHandler materialHandler;
    GameObject interactorHit;
    private Matrix4x4 interactorOffset;

    protected override void Awake()
    {
        base.Awake();
        selectableDistributionHandler = GetComponent<SelectableDistributionHandler>();
        materialHandler = GetComponent<MaterialHandler>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        XRBaseInteractor interactor = selectingInteractor;
        if (interactor != null && manipulationActivated && isLocallySelected)// One Handed
        {
            Matrix4x4 interactorMat = Matrix4x4.TRS(interactor.attachTransform.position, interactor.attachTransform.rotation, interactor.attachTransform.localScale);
            Matrix4x4 mat = interactorMat * interactorOffset;
            
            SetTransformByMatrix(this.gameObject, mat);  
        }
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        //if(!isSelected)
            materialHandler.ChangeToHoverMaterial();
    }
    
    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        XRBaseInteractor interactor = (XRBaseInteractor)args.interactorObject;
        interactor.gameObject.GetComponent<XRInteractorLineVisual>().enabled = true;
        //if(!isSelected)
            materialHandler.ResetMaterial();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!selectableDistributionHandler.isSelected)
        {
            base.OnSelectEntered(args);

            isLocallySelected = true;
            //selectableDistributionHandler.Select();
            
            XRBaseInteractor interactor = (XRBaseInteractor)args.interactorObject;
        
            CreateHitVisualization(interactor);

            interactorOffset = CalculateInteractorOffset(interactor);
        

            materialHandler.ChangeToSelectedMaterial();
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        XRBaseInteractor interactor = (XRBaseInteractor)args.interactorObject;

        RemoveHitVisualization();

        if (manipulationActivated && isLocallySelected)
        {
            Matrix4x4 interactorMat = Matrix4x4.TRS(interactor.attachTransform.position,
                interactor.attachTransform.rotation, interactor.attachTransform.localScale);
            Matrix4x4 mat = interactorMat * interactorOffset;
            SetTransformByMatrix(this.gameObject, mat);
            //selectableDistributionHandler.Deselect();
        }

        materialHandler.ResetMaterial();

        isLocallySelected = false;
    }

    private Matrix4x4 CalculateInteractorOffset(XRBaseInteractor interactor)
    {
        Matrix4x4 interactorMat = Matrix4x4.TRS(interactor.attachTransform.position, interactor.attachTransform.rotation, interactor.attachTransform.localScale);
        Matrix4x4 interactableMat = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        return Matrix4x4.Inverse(interactorMat) * interactableMat;
    }

    private void CreateHitVisualization(XRBaseInteractor interactor)
    {
        
        if (interactor is XRRayInteractor)
        {
            RaycastHit contactPoint;
            XRRayInteractor rayInteractor = (XRRayInteractor)interactor;
            rayInteractor.TryGetCurrent3DRaycastHit(out contactPoint);

            if (interactorHit != null)
            {
                Destroy(interactorHit);
            }
            interactorHit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            interactorHit.transform.position = contactPoint.point;
            interactorHit.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            interactorHit.transform.SetParent(transform, true);
            
        }
    }

    private void RemoveHitVisualization()
    {    
        Destroy(interactorHit);
    }

    private void SetTransformByMatrix(GameObject go, Matrix4x4 mat)
    {
        go.transform.localPosition = mat.GetColumn(3);
        go.transform.localRotation = mat.rotation;
        go.transform.localScale = mat.lossyScale;
    }

    private bool HasOneInteractor()
    {
        return interactorsSelecting.Count == 1;
    }

    private bool HasNoInteractors()
    {
        return interactorsSelecting.Count == 0;
    }
}
