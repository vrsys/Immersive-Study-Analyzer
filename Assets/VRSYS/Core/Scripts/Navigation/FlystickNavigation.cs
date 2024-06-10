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
//   Authors:        Sebastian Muehlhaus, Ephraim Schott, André Kunert
//   Date:           2022
//-----------------------------------------------------------------

using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Vrsys
{
    public class FlystickNavigation : MonoBehaviourPunCallbacks
    {
        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float transVel = 5.0f;

        [Tooltip("Rotation Velocity [deg/sec]")]
        [Range(5.0f, 90.0f)]
        public float rotVel = 20.0f;

        [Tooltip("Scale Input Factor [%/sec]")]
        [Range(0.1f, 10.0f)]
        public float scaleInputFactor = 1.0f;

        [Tooltip("Enable/Disable pitch rotation")]
        public bool pitchEnabled = false;

        public bool isGazeDirected = false;

        [Tooltip("Enable/Disable vertical translation")]
        public bool verticalTransEnabled = false;
        public bool validateAgainstNavMesh = false;
        public float navMeshTolerance = 0.1f;

        [Tooltip("Enable/Disable scaling")]
        public bool scaleEnabled = true;

        public InputActionProperty flyAction;
        public InputActionProperty rotationAction;
        public InputActionProperty scaleUpAction;
        public InputActionProperty scaleDownAction;
        public InputActionProperty sprintAction;
        public InputActionProperty resetAction;

        private float savTime = 0.0f;
        private float scaleLevelStopDuration = 1.0f; // in sec
        private bool isInitialized = false;

        private Vector3 startPos = Vector3.zero;
        private Quaternion startRot = Quaternion.identity;
        private float startScale = 1.0f;

        private ViewingSetupHMDAnatomy hmdAnatomy
        {
            get
            {
                return NetworkUser.localNetworkUser.viewingSetupAnatomy as ViewingSetupHMDAnatomy;
            }
        }

        void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            startScale = transform.localScale.x;
        }

        public void ResetTransform()
        {
            transform.position = startPos;
            transform.rotation = startRot;
            transform.localScale = new Vector3(startScale, startScale, startScale);
            // adjust near plane to scale level (should be consistent in user space, e.g. 20cm)
            hmdAnatomy.mainCamera.GetComponent<Camera>().nearClipPlane = 0.2f * startScale;
        }

        private void Update()
        {
            if (isInitialized)
            {
                if (verticalTransEnabled && validateAgainstNavMesh)
                    verticalTransEnabled = false;

                Vector3 transInput, rotInput;
                float scaleInput;

                GetNavigationInput(out transInput, out rotInput, out scaleInput);

                MapInput(transInput, rotInput, scaleInput);

                if (resetAction.action.WasReleasedThisFrame())
                    ResetTransform();


            }
            else
            {
                Initialize();
            }
        }

        void Initialize()
        {
            if (isActiveAndEnabled && !isInitialized)
            {
                if (photonView.IsMine)  // init if photon view is mine
                {
                    isInitialized = true;
                }
                else  // destroy script for remote users
                {
                    Destroy(this);
                }
            }
        }

        private void GetNavigationInput(out Vector3 transInput, out Vector3 rotInput, out float scaleInput)
        {
            transInput = Vector3.zero;
            rotInput = Vector3.zero;
            scaleInput = 0.0f;

            // get translation input
            transInput.z = flyAction.action.ReadValue<float>();

            // sprint option
            if (sprintAction != null && sprintAction.action.IsPressed())
                transInput *= 2.0f;

            // get rotation input
            if (rotationAction != null)
            {
                var primary2DAxis = rotationAction.action.ReadValue<Vector2>();
                rotInput.y = primary2DAxis.x;
                rotInput.x = primary2DAxis.y;
            }

            // get scale input
            if (scaleEnabled && scaleUpAction != null && scaleUpAction.action.IsPressed())
                scaleInput += 1.0f;
            if (scaleEnabled && scaleDownAction != null && scaleDownAction.action.IsPressed())
                scaleInput -= 1.0f;
        }

        private void MapInput(Vector3 transInput, Vector3 rotInput, float scaleInput)
        {
            // map scale input
            if (scaleInput != 0.0f)
            {
                if ((Time.time - savTime) > scaleLevelStopDuration) // not in the 1:1 scale level stop duration phase
                {
                    scaleInput = 1.0f + scaleInput * scaleInputFactor * Time.deltaTime; // transfer function
                    float newScale = transform.localScale.x * scaleInput;
                    newScale = Mathf.Clamp(newScale, 0.1f, 10.0f);

                    if ((transform.localScale.x > 1.0f && newScale < 1.0f) || (transform.localScale.x < 1.0f && newScale > 1.0f)) // passing 1:1 scale level
                    {
                        newScale = 1.0f; // snap exactely to 1:1 scale
                        savTime = Time.time;
                    }

                    transform.localScale = new Vector3(newScale, newScale, newScale); // apply new scale

                    hmdAnatomy.mainCamera.GetComponent<Camera>().nearClipPlane = 0.2f * newScale; // adjust near plane to scale level (should be consistent in user space, e.g. 20cm)
                }
            }

            // map translation input
            if (transInput.magnitude > 0.0f)
            {
                // forward movement in pointing direction
                Vector3 moveVec = isGazeDirected ? hmdAnatomy.mainCamera.transform.forward: hmdAnatomy.rightController.transform.forward;

                if (!verticalTransEnabled)
                {
                    moveVec.y = 0.0f; // restrict input to planar movement           
                    moveVec.Normalize();
                }

                moveVec = moveVec * Mathf.Pow(transInput.z, 3) * transVel * Time.deltaTime; // exponential transfer function
                moveVec *= transform.localScale.x; // translation velocity adjusted to scale level (should be consistent in user space)

                bool applyTranslation = true;
                if (validateAgainstNavMesh)
                {
                    var newPosOnMesh = hmdAnatomy.mainCamera.transform.position;
                    newPosOnMesh.y = transform.position.y;
                    newPosOnMesh += moveVec;
                    int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
                    NavMeshHit navMeshHit;
                    applyTranslation = NavMesh.SamplePosition(newPosOnMesh, out navMeshHit, navMeshTolerance, walkableMask);
                }

                if (applyTranslation)
                    transform.Translate(moveVec, Space.World);
            }

            // map rotation input
            if (rotInput.magnitude > 0.0f)
            {
                // head rotation
                float RY = Mathf.Pow(rotInput.y, 3) * Time.deltaTime * rotVel; // exponential transfer function
                                                                               // rotate around XR-Rig center
                transform.RotateAround(hmdAnatomy.mainCamera.transform.position, Vector3.up, RY); // rotate arround camera position (somewhere on platform)

                // pitch rotation
                if (pitchEnabled == true)
                {
                    float RX = Mathf.Pow(rotInput.x, 3) * Time.deltaTime * rotVel; // exponential transfer function
                                                                                   // rotate around XR-Rig center
                    Matrix4x4 rotMat = Matrix4x4.Rotate(Quaternion.Euler(0.0f, hmdAnatomy.mainCamera.transform.rotation.eulerAngles.y, 0.0f)); // only take "HEAD" rotation from users head transform
                    Vector3 rightGlobal = rotMat * Vector3.right; // define pitch axis in userspace "ONLY HEAD" orientation
                    transform.RotateAround(hmdAnatomy.mainCamera.transform.position, rightGlobal, RX); // rotate arround camera position (somewhere on platform)
                }

                // no roll rotation so far
            }

        }
    }
}