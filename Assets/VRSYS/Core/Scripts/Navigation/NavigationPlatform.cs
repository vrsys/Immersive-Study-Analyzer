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
//   Authors:        Sebastian Muehlhaus, Lucky Chandrautama
//   Date:           2022
//-----------------------------------------------------------------


using Photon.Pun;
using UnityEngine;

namespace Vrsys
{
    /*
     * Added to root of navigation platform prefabs, as instantiated by NetworkNavigationPlatformManager.
     */
    public class NavigationPlatform : MonoBehaviourPunCallbacks
    {
        public string initPlatformId = "";
        public string platformId { get; private set; }
        
        public bool debugAutoNavigationEnabled = false;
        public bool dontDestroyOnLoad = false;

        float startTime;

        private void Awake()
        {
        }

        private void Start()
        {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
            startTime = Time.timeSinceLevelLoad;
            if (initPlatformId.Length != 0)
            {
                SetPlatformId(initPlatformId);
            }
        }

        private void FixedUpdate()
        {
            if (photonView.IsMine && debugAutoNavigationEnabled)
            {
                DebugNavigationUpdate();
            }
        }

        void DebugNavigationUpdate()
        {
            // how much displacement from the starting position over time slowed down to 30%
            Vector3 movementDisplacement = new Vector3(Mathf.Sin(((Time.time - startTime)) * 0.3f * Mathf.PI), 0.0f, 0.0f);

            // scale movement by 25
            transform.position = Vector3.Lerp(transform.position, 25.0f * movementDisplacement, Time.deltaTime);
        }

        [PunRPC]
        public void SetPlatformId(string id)
        {
            platformId = id;
            gameObject.name = "Navigation Platform [" + platformId + "]";
            var platformManager = NetworkNavigationPlatformManager.instance;
            if (platformManager && platformManager.GetPlatform(id) == null)
            {
                platformManager.AddLocalPlatform(id, gameObject);
            }
        }
    }

}