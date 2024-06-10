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
//   Authors:        Ephraim Schott, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vrsys
{
    public class DesktopNavigation : MonoBehaviourPunCallbacks
    {

        public InputActionProperty move;
        public InputActionProperty viewYaw;
        public InputActionProperty viewPitch;


        [Tooltip("Translation Velocity [m/sec]")]
        [Range(0.1f, 10.0f)]
        public float translationVelocity = 3.0f;

        [Tooltip("Rotation Velocity [degree/sec]")]
        [Range(1.0f, 10.0f)]
        public float rotationVelocity = 5.0f;

        private Vector3 rotInput = Vector3.zero;

        ViewingSetupAnatomy viewingSetup;

        private void Start()
        {
            // This script should only compute for the local user
            if (!photonView.IsMine)
                Destroy(this);
        }

        void Update()
        {
            // Only calculate & apply input if local user fully instantiated
            if (EnsureViewingSetup())
            {
                MapInput(CalcTranslationInput(), CalcRotationInput());
            }
        }

        private Vector3 CalcTranslationInput()
        {
            Vector2 input = move.action.ReadValue<Vector2>();
            Vector3 transInput = Vector3.zero;

            // foward input
            transInput.z += input.y > 0f ? 1.0f : 0.0f;
            transInput.z -= input.y < 0f ? 1.0f : 0.0f;
            transInput.x += input.x > 0f ? 1.0f : 0.0f;
            transInput.x -= input.x < 0f ? 1.0f : 0.0f;

            return transInput * translationVelocity * Time.deltaTime;
        }

        private Vector3 CalcRotationInput()
        {
            float yaw = viewYaw.action.ReadValue<float>();
            float pitch = viewPitch.action.ReadValue<float>();
            float inputY = yaw;
            float inputX = pitch;

            // head rot input
            rotInput.y += inputY * rotationVelocity * Time.deltaTime;

            // pitch rot input
            rotInput.x -= inputX * rotationVelocity * Time.deltaTime ;
            rotInput.x = Mathf.Clamp(rotInput.x, -80, 80);

            return rotInput;
        }

        private void MapInput(Vector3 transInput, Vector3 rotInput)
        {
            // map translation input
            if (transInput.magnitude > 0.0f)
            {
                viewingSetup.mainCamera.transform.Translate(transInput);
            }

            // map rotation input
            if (rotInput.magnitude > 0.0f)
            {
                viewingSetup.mainCamera.transform.localRotation = Quaternion.Euler(rotInput.x, rotInput.y, 0.0f);
            }
        }

        bool EnsureViewingSetup()
        {
            if (viewingSetup == null)
            {
                if (NetworkUser.localNetworkUser != null)
                {
                    viewingSetup = NetworkUser.localNetworkUser.viewingSetupAnatomy;
                }
            }
            return viewingSetup != null;
        }
    }
}