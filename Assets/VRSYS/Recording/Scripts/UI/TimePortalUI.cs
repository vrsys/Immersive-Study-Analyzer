// VRSYS plugin of Virtual Reality and Visualization Group (Bauhaus-University Weimar)
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
// Copyright (c) 2024 Virtual Reality and Visualization Group
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
//   Authors:        Anton Lammert
//   Date:           2024
//-----------------------------------------------------------------

using System;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Vrsys;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    public class TimePortalUI : MonoBehaviourPun
    {
        public GameObject portal;
        public Material portalMaterial;
        private bool _cameraInitialized, _portalScaleAdjusted, _textureIntialized = false;
        private RectTransform _rectTransform;

        public void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        
        public void Update()
        {
            if (PhotonNetwork.InRoom)
            {
                if (photonView.IsMine)
                {
                    if (!_cameraInitialized)
                    {
                        Canvas canvas = GetComponent<Canvas>();
                        canvas.worldCamera = Utils.GetChildCamera(NetworkUser.localNetworkUser.gameObject).GetComponent<Camera>();
                        _cameraInitialized = true;
                        
                    }

                    if (_rectTransform.localScale.x * 1.0f/100.0f <= 0.01f && !_portalScaleAdjusted)
                    {
                        var localScale = _rectTransform.localScale * 1.0f/100.0f;
                        portal.transform.localScale = Vector3.Scale(portal.transform.localScale, new Vector3(1.0f/localScale.x, 1.0f/localScale.y, 1.0f/localScale.z));
                        _portalScaleAdjusted = true;
                    }
                }
                else
                {
                    if (!_textureIntialized)
                    {
                        GameObject stereoViewGo = Utils.GetChildBySubstring(gameObject, "StereoView");
                        if (stereoViewGo != null)
                        {
                            MeshRenderer meshRenderer = stereoViewGo.GetComponent<MeshRenderer>();
                            meshRenderer.material = portalMaterial;
                        }
                        _textureIntialized = true;
                    }
                }
            }
        }
    }
}