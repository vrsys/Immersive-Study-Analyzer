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
//   Authors:        Lucky Chandrautama, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------
// Description:
// The `SpheronNavigation.cs` is best suited for Desktop and Powerwall users and responsible to: 

// 1. detect the connectivity of the spheron to the machine where the owner of platfom is running the app.   
// 2. handle the movement and orientation of the platform

// For the minimal example, a demo scene can be found in `Assets/Scenes/SpheronSampleScene.unity`.
// This script is currently attached to `Assets/VRSYS/Core/Resources/UserPrefabs/DesktopViewingSetup.prefab`
// and take input as defined by `Assets/VRSYS/Core/Input Actions/VRSYS Input Actions.inputactions`
// under the `SpheronInput` input action tab. 
//-----------------------------------------------------------------
// Default controls:
// 1. Left stick movement = 2D movement along lcoal X and Z axis of the platform
// 2. Left stick rotation = Rotation around the local Y axis (yaw) of the platform
// 3. Right stick movement = 1D movement along local Y axis of the platform
// 4. Right stick rotation = Rotation around the local X axis (pitch) of the platform
// 5. Middle button = Reset platform to start position
// 6. Back button = Reset platform to start rotation
// 7. Left stick button = Rotation speedup
// 8. Right stick button = Translation speedup
// 9. Trigger button = unassigned


using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vrsys
{
    public class SpheronNavigation : MonoBehaviourPunCallbacks
    {

        public bool isProjectionWallMaster
        {
            get
            {
                var projectionWallConfig = GetComponent<ProjectionWallSystemConfigParser>();
                if (projectionWallConfig == null)
                    return false;
                return projectionWallConfig.localUserSettings.masterFlag;
            }
        }

        private List<string> inputDevicesNames = new List<string>();
        private Vector3 rotationInput = Vector3.zero;
        private Vector3 originPosition;
        private Vector3 originRotation;
        private string spheronName = "BUW Spheron (HID)";


        [Header("Detected Spheron Device")]
        public InputDevice spheronDevice;

        [Header("Navigation Platform")]
        [Tooltip("Apply the transfromation by the Spacemouse to the navigation platform target")]
        public bool applyToPlatform = false;
        [Tooltip("The navigation platform target according to PlatformID set in the Navigation Platform Link")]
        public GameObject navigationTarget;



        [Header("Input Properties")]
        public InputActionProperty middleButton;
        public InputActionProperty triggerButton;
        public InputActionProperty backButton;

        public InputActionProperty rightStick;
        public InputActionProperty rightStickRotation;
        public InputActionProperty rightStickButton;

        public InputActionProperty leftStickHorizontal;
        public InputActionProperty leftStickVertical;
        public InputActionProperty leftStickRotation;
        public InputActionProperty leftStickButton;

        [Header("Transformation velocity")]
        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float translationVelocity = 3.0f;

        [Tooltip("Pitch Rotation Velocity [degree/sec]")]
        [Range(1.0f, 10.0f)]
        public float pitchVelocity = 5.0f;

        [Tooltip("Yaw Rotation Velocity [degree/sec]")]
        [Range(1.0f, 10.0f)]
        public float yawVelocity = 5.0f;

        [Tooltip("Rotation Speedup Factor")]
        [Range(1.0f, 10.0f)]
        public float rotationSpeedup = 2.0f;

        [Tooltip("Translation Speedup Factor")]
        [Range(1.0f, 10.0f)]
        public float translationSpeedup = 2.0f;

        [Header("Rotation Constraints")]
        public bool disablePitch;
        public bool disableYaw;



        // Start is called before the first frame update
        void Start()
        {
            Initialize();

            if (EnsureViewingSetup())
            {
                originPosition = navigationTarget.transform.localPosition;
                originRotation = navigationTarget.transform.localEulerAngles;
            }
        }

        // Update is called once per frame
        void Update()
        {
            Initialize();

            if (EnsureViewingSetup())
            {
                MapInput(CalcTranslationInput(), CalcRotationInput(), originPosition, originRotation);
            }
        }



        private void Initialize()
        {
            // This script should only compute for the local user
            if (!photonView.IsMine || !isProjectionWallMaster)
            {
                Destroy(this);
            }
        }


        private Vector3 CalcTranslationInput()
        {
            Vector2 xzInput = rightStick.action.ReadValue<Vector2>();
            float xInput = leftStickHorizontal.action.ReadValue<float>();
            float yInput = leftStickVertical.action.ReadValue<float>();


            Vector3 transInput = Vector3.zero;

            // right input
            transInput.z += xzInput.y > 0f ? 1.0f : 0.0f;
            transInput.z -= xzInput.y < 0f ? 1.0f : 0.0f;


            transInput.x += xzInput.x > 0f ? 1.0f : 0.0f;
            transInput.x -= xzInput.x < 0f ? 1.0f : 0.0f;


            // left input
            transInput.y += yInput > 0f ? 1.0f : 0.0f;
            transInput.y -= yInput < 0f ? 1.0f : 0.0f;
            //transInput.x += xInput > 0f ? 1.0f : 0.0f;
            //transInput.x -= xInput < 0f ? 1.0f : 0.0f;


            float speedup = 1.0f;
            if (rightStickButton.action.ReadValue<float>() > 0.0f)
            {
                speedup = translationSpeedup;
            }


            return transInput * translationVelocity * speedup * Time.deltaTime;
        }

        private Vector3 CalcRotationInput()
        {

            float speedup = 1.0f;
            if (leftStickButton.action.ReadValue<float>() > 0.0f)
            {
                speedup = rotationSpeedup;
            }

            float yaw = rightStickRotation.action.ReadValue<float>();
            float pitch = leftStickRotation.action.ReadValue<float>();

            float inputY = yaw;
            float inputX = pitch;

            // yaw rot input
            if(!disableYaw)
            {
                rotationInput.y += inputY * yawVelocity * speedup * Time.deltaTime;
                //rotationInput.y = Mathf.Clamp(rotationInput.y, -80, 80);
            }

            // pitch rot input
            if(!disablePitch)
            {
                rotationInput.x -= inputX * pitchVelocity * speedup * Time.deltaTime;
                //rotationInput.x = Mathf.Clamp(rotationInput.x, -80, 80);
            }

            return rotationInput;
        }

        private void MapInput(Vector3 transInput, Vector3 rotInput, Vector3 originPosition, Vector3 originRotation)
        {
            // map translation input
            if (transInput.magnitude > 0.0f)
            {
                navigationTarget.transform.Translate(transInput);
            }

            // map rotation input
            if (rotInput.magnitude > 0.0f)
            {

                navigationTarget.transform.localRotation = Quaternion.Euler(rotInput.x, rotInput.y, 0.0f);
            }

            float resetPositionButton = backButton.action.ReadValue<float>();
            if (resetPositionButton > 0.0f)
            {
                navigationTarget.transform.localPosition = originPosition;

            }


            float resetRotationButton = middleButton.action.ReadValue<float>();
            if (resetRotationButton > 0.0f)
            {

                rotationInput = Vector2.zero;
                navigationTarget.transform.localRotation = Quaternion.Euler(originRotation.x, originRotation.y, 0.0f);
            }
        }

        private bool CheckSpheronConnection()
        {

            List<InputDevice> devices = new List<InputDevice>(InputSystem.devices);

            if (devices != null)
            {
                foreach (var item in devices)
                {
                    string desc = item.description.ToString();
                    inputDevicesNames.Add(desc);
                }

                foreach (var item in inputDevicesNames)
                {
                    if (item.ToLowerInvariant().Contains(spheronName.ToLowerInvariant()))
                    {

                        NetworkUser user = gameObject.GetComponent<NetworkUser>();
                        Debug.Log(spheronName + " is connected to " + user.userName);

                        return true;
                    }
                }

            }

            return false;

        }



        bool EnsureViewingSetup()
        {
            if (navigationTarget == null && isProjectionWallMaster)
            {
                if (applyToPlatform)
                {
                    var platformLink = GetComponent<NavigationPlatformLink>();
                    if (platformLink == null || platformLink.platform == null)
                        return false;
                    navigationTarget = platformLink.platform.gameObject;
                    var pView = navigationTarget.GetComponent<PhotonView>();
                    if (pView != null && CheckSpheronConnection())
                        pView.RequestOwnership();
                }
                else if (NetworkUser.localNetworkUser != null && NetworkUser.localNetworkUser.viewingSetupAnatomy != null)
                {
                    if (CheckSpheronConnection())
                    {
                        navigationTarget = NetworkUser.localNetworkUser.viewingSetupAnatomy.mainCamera;
                    }
                }
            }

            return navigationTarget != null;
        }

    }
}
