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
//   Authors:        Andre Kunert
//   Date:           2022
//-----------------------------------------------------------------

using UnityEngine;
using Photon.Pun;

namespace Vrsys
{
    public class Groundfollowing : MonoBehaviourPunCallbacks
    {
        public float targetHeight = 0.0f; // targeted height level above ground (required in Desktop viewing setups)
        private float rayStartHeight = 1.0f; // defines height from which ray is shot downwards (required to climb upstairs)
        private bool fallingFlag;
        private float fallStartTime;
        private RaycastHit rayHit;

        [Tooltip("LayerMask for Groundfollowing")]
        public LayerMask layerMask = -1; // -1 everything

        private ViewingSetupAnatomy viewingSetup;

        private bool isInitialized = false;

        void Awake()
        {
            Initialize();
            if (isInitialized)
            {
                viewingSetup = NetworkUser.localNetworkUser.viewingSetupAnatomy;
            }
        }

        private void Start()
        {
        }

        private void Update()
        {
            if (isInitialized){
                UpdateGroundfollowing();
            } else {
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

        public override void OnDisable()
        {
            base.OnDisable();
            fallingFlag = false;
        }

        public void UpdateGroundfollowing()
        {
            Vector3 startPos = Vector3.zero;
            if (transform == viewingSetup.mainCamera.transform) // navigation node is camera node 
            {
                startPos = transform.position + new Vector3(0.0f, rayStartHeight * transform.localScale.x, 0.0f);
            }
            else
            {
                //startPos = transform.position + transform.TransformDirection(new Vector3(0.0f, startHeight, 0.0f)); // platform center
                startPos = transform.position + transform.TransformDirection(new Vector3(viewingSetup.mainCamera.transform.localPosition.x, rayStartHeight * transform.localScale.x, viewingSetup.mainCamera.transform.localPosition.z)); // user pos
            }


            if (Physics.Raycast(startPos, Vector3.down, out rayHit, Mathf.Infinity, layerMask))
            {
                float heightOffset = rayHit.distance - rayStartHeight * transform.localScale.x - targetHeight * transform.localScale.x;
                //Debug.Log("GF Hit: " + rayHit.distance  + " " + heightOffset + " " + rayHit.point);

                if (heightOffset > 1.0f * transform.localScale.x) // falling
                {
                    if (fallingFlag == false) // start falling
                    {
                        fallingFlag = true;
                        fallStartTime = Time.time;
                    }

                    float fallTime = Time.time - fallStartTime;
                    Vector3 fallVec = Vector3.down * Mathf.Min(9.81f / 2.0f * Mathf.Pow(fallTime, 2.0f), 100.0f); // Weg-Zeit Gesetz
                                                                                                                  //transform.Translate(fallVec * Time.deltaTime); // correction in in navigation coordinate system (e.g. orientation)
                    transform.position += fallVec * Time.deltaTime;
                }
                else // near surface
                {
                    fallingFlag = false;

                    float verticalInput = Mathf.Pow(heightOffset * 0.5f, 2.0f) * -40.0f;
                    verticalInput = Mathf.Min(verticalInput, Mathf.Abs(heightOffset)); // clamp actual height correction to max height offset
                    if (heightOffset < 0.0f) verticalInput *= -1.0f;
                    //transform.Translate(new Vector3(0.0f, verticalInput * Time.deltaTime, 0.0f)); // correction in navigation coordinate system (e.g. orientation)
                    transform.position += new Vector3(0.0f, verticalInput * Time.deltaTime, 0.0f);
                }
            }
        }
    }
}
