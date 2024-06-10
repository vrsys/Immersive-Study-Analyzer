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
//   Authors:        Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vrsys.Scripts.Portals
{
    public class PortalExitHeadTracking : MonoBehaviour
    {
        public Transform portalEntranceHead;
        public Transform portalEntranceScreen;
        public Transform portalExitScreen;

        public Transform leftEye;
        public Transform rightEye;
        public float eyeDist = 0.064f;
        // Update is called once per frame
        void LateUpdate()
        {
            if (portalEntranceHead != null)
            {
                // local to head
                Quaternion a = portalEntranceScreen.rotation;
                Quaternion b = portalEntranceHead.rotation;
                Quaternion d = b * Quaternion.Inverse(a);
            
                Vector3 p = portalEntranceHead.transform.position;
                Vector4 r = portalEntranceScreen.worldToLocalMatrix * new Vector4(p.x, p.y, p.z, 1.0f);
                Vector3 e = new Vector3(r.x / r.w, r.y / r.w, r.z / r.w);
                transform.rotation = d * portalExitScreen.rotation;
                transform.localPosition = e;

                if (leftEye != null && rightEye != null)
                {
                    leftEye.transform.position = portalEntranceHead.transform.position - 0.5f * eyeDist * portalEntranceHead.transform.right;
                    rightEye.transform.position = portalEntranceHead.transform.position + 0.5f * eyeDist * portalEntranceHead.transform.right;
                }
                // global
                //transform.localRotation = portalEntranceHead.rotation;
                //transform.localPosition = portalExitScreen.localPosition - (portalEntranceScreen.transform.position - portalEntranceHead.transform.position);

                // static
                //transform.localPosition = new Vector3(0.0f, 0.0f, -0.3f);
            }
            else
            {
                if(NetworkUser.localNetworkUser != null)
                    portalEntranceHead = NetworkUser.localNetworkUser.avatarAnatomy.head.transform;
            }
        }
    }
}
