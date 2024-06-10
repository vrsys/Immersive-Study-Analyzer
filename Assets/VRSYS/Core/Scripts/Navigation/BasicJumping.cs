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
//   Authors:        Ephraim Schott, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Photon.Pun;


[RequireComponent(typeof(JumpingRay))]
public class BasicJumping : MonoBehaviourPunCallbacks, IPunObservable
{
    public bool isInitialized = false;
    private bool isLocal;
    public bool isRightHanded = true;

    public float indicationThreshhold = 0.25f;
    public float setTargetThreshhold = 0.9f;
    private float trigger = 0;

    private GameObject cam = null;
    private GameObject hmdPlatform = null;
    private GameObject controller = null;

    //Input Parameters
    public InputActionProperty teleportAction;

    private Vector3 startPosition = Vector3.zero;//new Vector3(70.28f, 22.26f, 37.78f);
    private Quaternion startRotation = Quaternion.identity; //Vector3(0,312.894073,0)
    private Quaternion rotTowardsHit = Quaternion.identity;

    public bool triggerPressed = false;
    public bool triggerReleased = false;

    private Vector3 jumpingTargetPosition;
    private Vector3 circlePosition;

    private bool rayActivated = false;
    private bool targetSet = false;
    private bool isValid = false;

    [Header("Configure Visual Feedback")]
    public GameObject avatarPrefab;
    public GameObject jumpingCirclePrefab;
    public GameObject jumpingArrowRingPrefab;
    public Material avatarNoAccessMaterial;
    public Material previewMaterial;
    public Material fadeMaterial;

    private GameObject jumpingPositionPreview = null;
    private GameObject jumpingCircleStartPreview = null;
    private GameObject jumpingCircleFullPreview = null;
    private GameObject jumpingAvatarPreview = null;
    private GameObject avatarHead = null;
    private GameObject avatarShirt = null;

    private Material avatarAccessMaterial;

    [Header("Configure Raycast")]
    public LayerMask hitLayer;

    private RaycastHit hit;
    private Collider hitObject;
    private Vector3 hitVector;
    private Vector3 hitNormal;

    //Setup Jumping Ray
    public JumpingRay jumpingRay;

    public class RayStateChangedEvent : UnityEvent<bool> { };
    RayStateChangedEvent rayStateChanged = new RayStateChangedEvent();

    void Awake()
    {
        Initialize();
        if (isInitialized)
        {
            Setup();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        Destroy(jumpingPositionPreview);
        Destroy(jumpingCircleStartPreview);
        Destroy(jumpingCircleFullPreview);
        Destroy(jumpingAvatarPreview);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocal) return;

        if (controller != null) // guard
        {
            trigger = teleportAction.action.ReadValue<float>();

            UpdateRayVisualization(trigger, 0.00001f);

            if (trigger > 0.001f) // touched
            {
                hitObject = jumpingRay.hitObject;
                hitVector = jumpingRay.hitVector;
                hitNormal = jumpingRay.hitNormal;

                if (trigger > 0.05f && !targetSet && isValid)
                {
                    Quaternion direction = Quaternion.LookRotation((new Vector3(hitVector.x, 0f, hitVector.z) - new Vector3(cam.transform.position.x, 0f, cam.transform.position.z)).normalized, Vector3.up);
                    UpdateTargetVisualization(hitVector, direction);
                }

                if (trigger < indicationThreshhold)
                {
                    HideAvatar();
                }

                // get collider and position from teleportRay
                if (trigger > indicationThreshhold && triggerPressed == false) // pressed first time
                {
                    if (hitObject != null && hitObject.gameObject.layer == Vrsys.Utility.LayermaskToLayer(hitLayer.value)) // guard
                    {
                        SetJumpingTarget(hitVector);
                    }
                }

                if (trigger > setTargetThreshhold && triggerPressed == false) // pressed first time
                {
                    if (hitObject != null && hitObject.gameObject.layer == Vrsys.Utility.LayermaskToLayer(hitLayer.value)) // guard
                    {
                        SetJumpingTarget(hitVector);
                        triggerPressed = true;
                        targetSet = true;
                        jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
                    }
                }
                else if (trigger > setTargetThreshhold && triggerPressed) // pressed
                {
                    if (hit.collider != null) // guard
                    {
                        AdjustJumpingOrientation(hitVector);

                    }
                    else
                    {
                        AdjustJumpingOrientation(hitVector);
                    }
                }
                else if (trigger < setTargetThreshhold && triggerPressed) // touched
                {
                    triggerPressed = false;
                    triggerReleased = true;
                }
            }
            else // if not touched
            {
                if (jumpingPositionPreview.activeSelf) // released
                {
                    jumpingPositionPreview.SetActive(false); // hide
                    jumpingCircleStartPreview.SetActive(false); // hide
                    jumpingCircleFullPreview.SetActive(false); // hide
                }
                HideAvatar();
                if (triggerPressed) triggerReleased = true;
                targetSet = false;
            }

            if (triggerReleased)
            {
                jumpingPositionPreview.SetActive(false); // hide
                jumpingCircleStartPreview.SetActive(false); // hide
                jumpingCircleFullPreview.SetActive(false); // hide
                HideAvatar();
                Jump();
                triggerReleased = false;

            }
        }
    }

    void Initialize()
    {
        if (isActiveAndEnabled && !isInitialized)
        {
            if (photonView.IsMine)  // init if photon view is mine
            {

                isInitialized = true;
                isLocal = true;
            }
            else
            {
                isInitialized = true;
                isLocal = false;
            }

        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting && photonView.IsMine)
        {
            stream.SendNext(isValid);
            stream.SendNext(rayActivated);
            stream.SendNext(trigger);
            stream.SendNext(jumpingCircleFullPreview.activeSelf);
            stream.SendNext(jumpingAvatarPreview.activeSelf);
            stream.SendNext(circlePosition);
            stream.SendNext(jumpingAvatarPreview.transform.position);
            stream.SendNext(jumpingCircleFullPreview.transform.rotation);
        }
        else if (stream.IsReading)
        {
            isValid = (bool)stream.ReceiveNext();
            rayActivated = (bool)stream.ReceiveNext();
            trigger = (float)stream.ReceiveNext();
            bool circleActivated = (bool)stream.ReceiveNext();
            bool avatarActivated = (bool)stream.ReceiveNext();
            circlePosition = (Vector3)stream.ReceiveNext();
            Vector3 avatarPos = (Vector3)stream.ReceiveNext();
            Quaternion avatarRot = (Quaternion)stream.ReceiveNext();


            jumpingRay.rayActive = rayActivated;
            jumpingRay.RayValid(isValid);

            jumpingPositionPreview.SetActive(circleActivated);
            jumpingCircleStartPreview.SetActive(circleActivated);
            jumpingCircleFullPreview.SetActive(circleActivated);
            if (circleActivated)
            {
                UpdateTargetVisualization(circlePosition, avatarRot);
                if (trigger < 0.9f)
                {
                    jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = avatarAccessMaterial;
                }
                else
                {
                    jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
                }
            }

            jumpingAvatarPreview.SetActive(avatarActivated);
            if (avatarActivated)
            {
                ShowAvatar(avatarPos, avatarRot);
            }

            if (isValid)
            {
                UpdateValidityVisualization(isValid);
            }
        }
    }

    private void Jump()
    {
        // Calculate required turn angle
        Quaternion camLocalRot = cam.transform.localRotation;
        Quaternion platformLocalRot = transform.localRotation;
        float goalAngle = rotTowardsHit.eulerAngles.y;
        float turnAngle = goalAngle - (camLocalRot.eulerAngles.y + platformLocalRot.eulerAngles.y);

        // Rotate platform
        transform.Rotate(0f, turnAngle, 0f);

        // Adjust offset to new platform orientation
        Vector3 a = new Vector3(hmdPlatform.transform.position.x, this.transform.position.y, hmdPlatform.transform.position.z);
        Vector3 b = new Vector3(cam.transform.position.x, this.transform.position.y, cam.transform.position.z);
        Vector3 centerOffset = b - a;

        // jump to new position and apply offset
        transform.position = jumpingTargetPosition - centerOffset;
    }

    private void UpdateRayVisualization(float inputValue, float threshold)
    {

        // Visualize ray if input value is bigger than a certain treshhold
        if (inputValue > threshold && rayActivated == false)
        {
            jumpingRay.rayActive = true;
            rayStateChanged.Invoke(true);
            rayActivated = true;
        }
        else if (inputValue < threshold && rayActivated)
        {
            jumpingRay.rayActive = false;
            rayStateChanged.Invoke(false);
            rayActivated = false;
        }

        // update ray length and intersection point of ray
        if (rayActivated && !targetSet)
        { // if ray is on
            // get collider and position from teleportRay
            hitObject = jumpingRay.hitObject;
            hitVector = jumpingRay.hitVector;
            hitNormal = jumpingRay.hitNormal;

            if (hitObject != null && hitObject.gameObject.layer == Vrsys.Utility.LayermaskToLayer(hitLayer.value)) // discern between valid and invalid hit here (invalid should change ray color/material but not show target)
            {
                // visualize valid ray
                if (isValid == false)
                {
                    jumpingRay.RayValid(true);
                    UpdateValidityVisualization(true);
                }
                isValid = true;
            }
            else
            {
                if (isValid == true)
                {
                    jumpingRay.RayValid(false);
                    UpdateValidityVisualization(false);
                }
                isValid = false;
            }
        }
    }


    private void UpdateTargetVisualization(Vector3 circlePos, Quaternion orientation)
    {
        jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = avatarAccessMaterial;
        fadeMaterial.color = new Color(0.0f, 0.0f + trigger, 1f - trigger, 0.4f + 0.4f * trigger);
        jumpingPositionPreview.GetComponentInChildren<MeshRenderer>().material = fadeMaterial;
        float circleSize = trigger;
        jumpingPositionPreview.SetActive(true);
        jumpingCircleStartPreview.SetActive(true);
        jumpingCircleFullPreview.SetActive(true);
        circlePosition = circlePos;
        jumpingPositionPreview.transform.position = circlePosition;
        jumpingCircleStartPreview.transform.position = circlePosition;
        jumpingCircleFullPreview.transform.position = circlePosition;
        jumpingCircleFullPreview.transform.rotation = orientation;
        if (circleSize > setTargetThreshhold) circleSize = setTargetThreshhold;
        jumpingPositionPreview.transform.localScale = new Vector3(circleSize, 0.02f, circleSize) * transform.lossyScale.x;
        
        jumpingCircleFullPreview.transform.localScale = new Vector3(setTargetThreshhold * 1.55f, 0.02f, setTargetThreshhold * 1.55f) * transform.lossyScale.x;
        jumpingCircleStartPreview.transform.localScale = new Vector3(indicationThreshhold / 2, 0.02f, indicationThreshhold / 2) * transform.lossyScale.x;
    }

    public void UpdateValidityVisualization(bool accessible)
    {
        if (accessible)
        {
            jumpingPositionPreview.GetComponentInChildren<MeshRenderer>().material = fadeMaterial;
            if (!targetSet)
            {
                jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = avatarAccessMaterial;
            }
            else
            {
                jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
            }
            jumpingCircleStartPreview.GetComponentInChildren<MeshRenderer>().material = avatarAccessMaterial;
            avatarHead.GetComponentInChildren<MeshRenderer>().material = avatarAccessMaterial;
            avatarShirt.GetComponentInChildren<MeshRenderer>().material = avatarAccessMaterial;
        }
        else
        {
            jumpingPositionPreview.GetComponentInChildren<MeshRenderer>().material = avatarNoAccessMaterial;
            jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = avatarNoAccessMaterial;
            jumpingCircleStartPreview.GetComponentInChildren<MeshRenderer>().material = avatarNoAccessMaterial;
            avatarHead.GetComponentInChildren<MeshRenderer>().material = avatarNoAccessMaterial;
            avatarShirt.GetComponentInChildren<MeshRenderer>().material = avatarNoAccessMaterial;
        }
    }

    private void SetJumpingTarget(Vector3 hitPos)
    {
        jumpingTargetPosition = hitPos;

        float previewPersonHeight = cam.transform.position.y - transform.position.y;
        Vector3 avatarPos = jumpingTargetPosition + new Vector3(0f, previewPersonHeight, 0f);
        Quaternion avatarRot = Quaternion.LookRotation((new Vector3(jumpingTargetPosition.x, 0f, jumpingTargetPosition.z) - new Vector3(cam.transform.position.x, 0f, cam.transform.position.z)).normalized, Vector3.up);

        ShowAvatar(avatarPos, avatarRot);
    }

    private void AdjustJumpingOrientation(Vector3 hitPos)
    {
        // update height of preview avatar
        Vector3 pos = jumpingAvatarPreview.transform.position;
        float previewPersonHeight = cam.transform.position.y - transform.position.y;
        jumpingAvatarPreview.transform.position = new Vector3(pos.x, jumpingTargetPosition.y + previewPersonHeight, pos.z);

        // rotate avatar in direction of new hit point

        if ((new Vector3(hitPos.x, 0f, hitPos.z) - new Vector3(pos.x, 0f, pos.z)).magnitude > 0.2f)
        {
            rotTowardsHit = Quaternion.LookRotation((hitPos - pos).normalized, Vector3.up);
            jumpingAvatarPreview.transform.rotation = Quaternion.Euler(0f, rotTowardsHit.eulerAngles.y, 0f);
            jumpingCircleFullPreview.transform.rotation = Quaternion.Euler(0f, rotTowardsHit.eulerAngles.y, 0f);
        }
    }


    private void ShowAvatar(Vector3 worldPos, Quaternion worldRot)
    {
        jumpingAvatarPreview.SetActive(true); // show
        jumpingAvatarPreview.transform.position = worldPos;
        jumpingAvatarPreview.transform.rotation = worldRot;
        jumpingAvatarPreview.transform.localScale = transform.lossyScale;
        jumpingCircleFullPreview.transform.rotation = worldRot;
    }

    private void HideAvatar()
    {
        jumpingAvatarPreview.SetActive(false); // hide
    }


    private void Setup()
    {
        if(photonView.Owner == null)
            return;
        
        if (isLocal)
        {
            var viewingSetup = Vrsys.NetworkUser.localNetworkUser.viewingSetupAnatomy;
            if (viewingSetup)
            {
                if (viewingSetup is Vrsys.ViewingSetupHMDAnatomy)
                {
                    var hmd = (Vrsys.ViewingSetupHMDAnatomy)viewingSetup;
                    if (!isRightHanded)
                    {
                        controller = hmd.leftController;
                    }
                    else
                    {
                        controller = hmd.rightController;
                    }
                    cam = hmd.mainCamera;
                    hmdPlatform = hmd.gameObject;
                }
                else
                {
                    Debug.LogError("Desktop user can not init Jumping");
                }
            }

        }
        else
        {
            var avatarAnatomy = GetComponent<Vrsys.AvatarHMDAnatomy>();
            controller = avatarAnatomy.handRight;
            cam = avatarAnatomy.head;
        }

        jumpingRay = GetComponent<JumpingRay>();
        jumpingRay.controller = controller;

        // geometry for Avatar visualization
        if (previewMaterial == null)
            previewMaterial = MaterialsFactory.CreatePreviewMaterial();
        if (fadeMaterial == null)
            fadeMaterial = MaterialsFactory.CreateFadeMaterial();

        if (photonView != null)
        {
            jumpingPositionPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jumpingPositionPreview.transform.localScale = new Vector3(1f, 0.02f, 1f) * transform.lossyScale.x;
            jumpingPositionPreview.name = "Jumping Position Preview ['" + photonView.Owner.NickName + "']";
            ;
            jumpingPositionPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            jumpingPositionPreview.GetComponentInChildren<MeshRenderer>().material = fadeMaterial;
            jumpingPositionPreview.SetActive(false); // hide

            jumpingCircleStartPreview = Instantiate(jumpingCirclePrefab, startPosition, startRotation) as GameObject;
            jumpingCircleStartPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
            jumpingCircleStartPreview.name =
                "Jumping Position Start Circle Preview ['" + photonView.Owner.NickName + "']";
            ;
            jumpingCircleStartPreview.transform.localScale =
                new Vector3(indicationThreshhold / 2, 0.02f, indicationThreshhold / 2) * transform.lossyScale.x;
            jumpingCircleStartPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            jumpingCircleStartPreview.SetActive(false); // hide

            jumpingCircleFullPreview = Instantiate(jumpingArrowRingPrefab, startPosition, startRotation) as GameObject;
            jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
            jumpingCircleFullPreview.name =
                "Jumping Position Full Circle Preview ['" + photonView.Owner.NickName + "']";
            ;
            jumpingCircleFullPreview.transform.localScale =
                new Vector3(setTargetThreshhold * 1.55f, 0.02f, setTargetThreshhold * 1.55f) * transform.lossyScale.x;
            jumpingCircleFullPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            jumpingCircleFullPreview.SetActive(false); // hide

            jumpingAvatarPreview = Instantiate(avatarPrefab, startPosition, startRotation) as GameObject;
            jumpingAvatarPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            jumpingAvatarPreview.name = "Jumping Avatar Preview ['" + photonView.Owner.NickName + "']";
            jumpingAvatarPreview.transform.localScale *= transform.lossyScale.x;
            jumpingAvatarPreview.SetActive(false);

            DontDestroyOnLoad(jumpingPositionPreview);
            DontDestroyOnLoad(jumpingCircleStartPreview);
            DontDestroyOnLoad(jumpingCircleFullPreview);
            DontDestroyOnLoad(jumpingAvatarPreview);

            avatarHead = Vrsys.Utility.FindRecursive(jumpingAvatarPreview, "AvatarHead");
            avatarShirt = Vrsys.Utility.FindRecursive(jumpingAvatarPreview, "Shirt Male");

            avatarAccessMaterial = jumpingAvatarPreview.GetComponentInChildren<Renderer>().material;
        }
    }
}