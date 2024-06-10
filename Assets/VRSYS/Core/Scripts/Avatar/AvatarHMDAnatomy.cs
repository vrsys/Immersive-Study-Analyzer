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

using UnityEngine;

namespace Vrsys 
{
    public class AvatarHMDAnatomy : AvatarAnatomy
    {
        public GameObject body;
        public GameObject handLeft;
        public GameObject handRight;

        protected override void ParseComponents()
        {
            base.ParseComponents();

            if (body == null)
            {
                body = transform.Find("Head/Body")?.gameObject;
            }
            if (handLeft == null)
            {
                handLeft = transform.Find("HandLeft")?.gameObject;
            }
            if (handRight == null)
            {
                handRight = transform.Find("HandRight")?.gameObject;
            }
        }

        public override void ConnectFrom(ViewingSetupAnatomy viewingSetup)
        {
            if (viewingSetup is ViewingSetupHMDAnatomy)
            {
                var hmd = viewingSetup as ViewingSetupHMDAnatomy;

                head.transform.position = Vector3.zero;
                head.transform.rotation = Quaternion.identity;
                head.transform.SetParent(hmd.mainCamera.transform, false);

                if(hmd.leftController != null)
                {
                    handLeft.transform.position = Vector3.zero;
                    handLeft.transform.rotation = Quaternion.identity;
                    handLeft.transform.SetParent(hmd.leftController.transform, false);
                }

                if(hmd.rightController != null)
                {
                    handRight.transform.position = Vector3.zero;
                    handRight.transform.rotation = Quaternion.identity;
                    handRight.transform.SetParent(hmd.rightController.transform, false);
                }
            }
            else 
            {
                throw new System.ArgumentException("Incompatible viewing setup. Connection only supported with '" + typeof(ViewingSetupHMDAnatomy).Name + "'.");
            }
        }

        public override void SetColor(Color color)
        {
            if (body == null)
            {
                Debug.LogWarning("Cannot change avatar color. Body not set.");
                return;
            }

            var renderer = body.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            else
            {
                Debug.LogWarning("Cannot change avatar color. No renderer set in children of body.");
            }
        }

        public override Color? GetColor()
        {
            if (body == null) 
            {
                return null;
            }

            var renderer = body.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return renderer.material.color;
            }

            return null;
        }
    }
}
