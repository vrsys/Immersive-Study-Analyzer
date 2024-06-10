using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Vrsys;
using Vrsys.Scripts.Recording;

public class RayHandler : MonoBehaviourPunCallbacks, IPunObservable
{
    public enum Hand {Left, Right};
    public enum RayEnd {IntersectionSphere, Infinity, Other}

    private LineRenderer rightRayRender;
    private LineRenderer leftRayRender;

    private GameObject leftRayIntersectionSphere;
    private GameObject rightRayIntersectionSphere;

    private ViewingSetupHMDAnatomy hmd;
    private AvatarHMDAnatomy avatar;
    private bool viewSetup = false;
    private bool initialized = false;
    private Color leftRayColor = Color.blue;
    private Color rightRayColor = Color.blue;

    // see: https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html - Create Primitive may fail at runtime!
    private SphereCollider _placeHolder = new SphereCollider();
    
    // Update is called once per frame
    void Update()
    {

        if (!initialized)
        {
            if (photonView != null && photonView.IsMine)
            {
                EnsureViewingSetup();
                leftRayRender = hmd.leftController.GetComponent<LineRenderer>();
                leftRayRender.enabled = false;
                rightRayRender = hmd.rightController.GetComponent<LineRenderer>();
                rightRayRender.enabled = false;

                CreateIntersectionSpheres();
                initialized = true;
            }
            else if(photonView != null && !photonView.IsMine)
            {
                avatar = gameObject.GetComponent<AvatarHMDAnatomy>();
                leftRayRender = avatar.handLeft.GetComponent<LineRenderer>();
                leftRayRender.enabled = false;
                rightRayRender = avatar.handRight.GetComponent<LineRenderer>();
                rightRayRender.enabled = false;
                
                CreateIntersectionSpheres();
                
                initialized = true;
            }
        }
    }
    
    private void CreateIntersectionSpheres()
    {
        leftRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftRayIntersectionSphere.name = "Left Ray Intersection Sphere";
        leftRayIntersectionSphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        leftRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.yellow;
        leftRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false;
        leftRayIntersectionSphere.SetActive(false);
        leftRayIntersectionSphere.transform.parent = gameObject.transform;
            
        rightRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightRayIntersectionSphere.name = "Right Ray Intersection Sphere";
        rightRayIntersectionSphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        rightRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.yellow;
        rightRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false;
        rightRayIntersectionSphere.SetActive(false);
        rightRayIntersectionSphere.transform.parent = gameObject.transform;
    }

    public void ActivateRay(Hand hand){

        if (initialized)
        {
            if (hand == Hand.Left)
            {
                leftRayRender.enabled = true;
                leftRayIntersectionSphere.SetActive(true);
            }
            else
            {
                rightRayRender.enabled = true;
                rightRayIntersectionSphere.SetActive(true);
            }
        }
    }

    public void DeactivateRay(Hand hand){

        if (initialized)
        {
            if (hand == Hand.Left)
            {
                leftRayRender.enabled = false;
                leftRayIntersectionSphere.SetActive(false);
            }
            else
            {
                rightRayRender.enabled = false;
                rightRayIntersectionSphere.SetActive(false);
            }
        }
    }

    public void UpdateRay(Hand hand, Vector3 position, RayEnd rayEnd)
    {
        if (!initialized)
            return;
        
        GameObject handGameObject = GetHand(hand);

        var rayRenderer = hand == Hand.Left ? leftRayRender : rightRayRender;
        var intersectionSphere = hand == Hand.Left ? leftRayIntersectionSphere : rightRayIntersectionSphere;

        intersectionSphere.transform.position = position;
        rayRenderer.positionCount = 2;
        ActivateIntersectionSphere(hand);
        
        if(rayEnd == RayEnd.Other){
            rayRenderer.SetPosition(0, handGameObject.transform.position);
            rayRenderer.SetPosition(1, position);
            return;
        } else if(rayEnd == RayEnd.IntersectionSphere){
            if(intersectionSphere.activeSelf){
                rayRenderer.SetPosition(0, handGameObject.transform.position);
                rayRenderer.SetPosition(1, intersectionSphere.transform.position);
                return; 
            }
        }

        rayRenderer.SetPosition(0, handGameObject.transform.position);
        rayRenderer.SetPosition(1,  handGameObject.transform.position +  handGameObject.transform.TransformDirection(Vector3.forward) * 1000);
    }

    private GameObject GetHand(Hand hand)
    {
        if(photonView.IsMine){
            return hand == Hand.Left? hmd.leftController : hmd.rightController;
        }
        
        return hand == Hand.Left? avatar.handLeft : avatar.handRight;
    }
    
    public void ActivateIntersectionSphere(Hand hand)
    {
        (hand == Hand.Left ? leftRayIntersectionSphere : rightRayIntersectionSphere)?.SetActive(true);
    }

    public void DeactivateIntersectionSphere(Hand hand)
    {
        (hand == Hand.Left ? leftRayIntersectionSphere : rightRayIntersectionSphere)?.SetActive(false);
    }

    public void UpdateIntersectionSphere(Hand hand, Vector3 position)
    {
        if (hand == Hand.Left && leftRayIntersectionSphere != null)
        {
            leftRayIntersectionSphere.transform.position = position;
        }
        else if (hand == Hand.Right && rightRayIntersectionSphere != null)
        {
            rightRayIntersectionSphere.transform.position = position;
        }
    }

    bool EnsureViewingSetup() 
    {
        if(hmd != null)
            return true;

        if(GetComponent<NetworkUser>().viewingSetupAnatomy == null)
            return false;

        if(!(GetComponent<NetworkUser>().viewingSetupAnatomy is ViewingSetupHMDAnatomy))
            return false;

        hmd = GetComponent<NetworkUser>().viewingSetupAnatomy as ViewingSetupHMDAnatomy;

        return true;
    }

    public GameObject GetLeftIntersectionSphere()
    {
        return leftRayIntersectionSphere;
    }

    public GameObject GetRightIntersectionSphere()
    {
        return rightRayIntersectionSphere;
    }

    public void SetRayColor(Hand hand, Color color)
    {
        if (hand == Hand.Left)
        {
            leftRayColor = color;
            if (leftRayRender != null)
            {
                leftRayRender.startColor = leftRayColor;
                leftRayRender.endColor = leftRayColor;
            }
        }
        else if(hand == Hand.Right)
        {
            rightRayColor = color;
            if (rightRayRender != null)
            {
                rightRayRender.startColor = rightRayColor;
                rightRayRender.endColor = rightRayColor;
            }
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting && photonView.IsMine)
        {
            bool leftHandActive = false;
            bool rightHandActive = false;
            
            Vector3 leftPosition = Vector3.zero;
            Vector3 rightPosition = Vector3.zero;

            bool leftRayActive = false;
            bool rightRayActive = false;

            Color leftRayCol = Color.white;
            Color rightRayCol = Color.white;
            
            if (initialized)
            {
                leftHandActive =  leftRayIntersectionSphere.activeSelf;
                leftPosition = leftRayIntersectionSphere.transform.position;
                
                rightHandActive =  rightRayIntersectionSphere.activeSelf;
                rightPosition = rightRayIntersectionSphere.transform.position;

                leftRayActive = leftRayRender.enabled;
                rightRayActive = rightRayRender.enabled;

                leftRayCol = leftRayColor;
                rightRayCol = rightRayColor;
            }
            
            stream.SendNext(leftHandActive);
            stream.SendNext(leftPosition);
            stream.SendNext(rightHandActive);
            stream.SendNext(rightPosition);
            stream.SendNext(leftRayActive);
            stream.SendNext(rightRayActive);
            stream.SendNext(leftRayCol.r);
            stream.SendNext(leftRayCol.g);
            stream.SendNext(leftRayCol.b);
            stream.SendNext(rightRayCol.r);
            stream.SendNext(rightRayCol.g);
            stream.SendNext(rightRayCol.b);
        }
        else if (stream.IsReading)
        {
            bool leftHandActive = (bool)stream.ReceiveNext();
            Vector3 leftPosition = (Vector3)stream.ReceiveNext();
            bool rightHandActive = (bool)stream.ReceiveNext();
            Vector3 rightPosition = (Vector3)stream.ReceiveNext();
            bool leftRayActive = (bool)stream.ReceiveNext();
            bool rightRayActive = (bool)stream.ReceiveNext();
            Color leftRayCol = new Color((float)stream.ReceiveNext(), (float)stream.ReceiveNext(), (float)stream.ReceiveNext());
            Color rightRayCol = new Color((float)stream.ReceiveNext(), (float)stream.ReceiveNext(), (float)stream.ReceiveNext());
            
            if (initialized)
            {
                leftRayRender.enabled = leftRayActive;
                rightRayRender.enabled = rightRayActive;
                
                leftRayIntersectionSphere.SetActive(leftHandActive);
                if(leftHandActive)
                    UpdateRay(Hand.Left, leftPosition, RayEnd.IntersectionSphere);
                rightRayIntersectionSphere.SetActive(rightHandActive);
                if(rightHandActive)
                    UpdateRay(Hand.Right, rightPosition, RayEnd.IntersectionSphere);
                leftRayIntersectionSphere.transform.position = leftPosition;
                rightRayIntersectionSphere.transform.position = rightPosition;
                leftRayColor = leftRayCol;
                rightRayColor = rightRayCol;

                
            }
        }
    }

    #region RecordingReplayFunctionality

    public Color GetRayColor(Hand hand)
    {
        if (hand == Hand.Left)
            return leftRayColor;
        
        return rightRayColor;
    }
    
    public int GetRayState(Hand hand)
    {
        if (hand == Hand.Left && leftRayRender != null)
        {
            if (leftRayRender.enabled && leftRayRender.positionCount > 0)
                return 1;
        }
        else if(hand == Hand.Right && rightRayRender != null)
        {
            if (rightRayRender.enabled && rightRayRender.positionCount > 0)
                return 1;
        }

        return 0;
    }

    public Vector3 GetRayEndPoint(Hand hand)
    {
        if (hand == Hand.Left && leftRayRender != null)
        {
            return leftRayRender.GetPosition(1);
        }
        else if (hand == Hand.Right && rightRayRender!= null)
        {
            return rightRayRender.GetPosition(1);
        }

        return Vector3.zero;
    }
    
    public void ChangeRayState(Hand hand, int state)
    {
        if (hand == Hand.Left)
        {
            if(leftRayRender == null)
                leftRayRender = Utils.GetChildByName(gameObject, "HandLeft").GetComponent<LineRenderer>();
            if(state == 1)
            {
                leftRayRender.enabled = true;
            }
            else
            {
                leftRayRender.enabled = false;
            }
                
        }
        else
        {
            if(rightRayRender == null)
                rightRayRender = Utils.GetChildByName(gameObject, "HandRight").GetComponent<LineRenderer>();
            if (state == 1)
            {
                rightRayRender.enabled = true;
            }
            else
            {
                rightRayRender.enabled = false;
            }
        }
    }
    
    public void UpdateRecordedRay(Hand hand, Vector3 position, RayEnd rayEnd)
    {
        if (hand == Hand.Left)
        {
            if(leftRayRender == null)
                leftRayRender = Utils.GetChildByName(gameObject, "HandLeft").GetComponent<LineRenderer>();
            leftRayRender.positionCount = 2;
            leftRayRender.SetPosition(0, leftRayRender.transform.position);
            leftRayRender.SetPosition(1, position);
        }
        else
        {
            if(rightRayRender == null)
                leftRayRender = Utils.GetChildByName(gameObject, "HandRight").GetComponent<LineRenderer>();
            rightRayRender.positionCount = 2;
            rightRayRender.SetPosition(0, rightRayRender.transform.position);
            rightRayRender.SetPosition(1, position);
        }
    }

    #endregion
}

