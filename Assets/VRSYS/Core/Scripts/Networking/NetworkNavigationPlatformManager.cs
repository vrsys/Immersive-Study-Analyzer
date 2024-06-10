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
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Vrsys
{
    /*
     *  NetworkNavigationPlatformManager coordinates request and delivery of 
     *  NetworkNavigationPlatform instances. In multi-user distributed environments
     *  a navigation platform groups X amount of users on a shared navigation device.
     *  if the platform is transformed, all gathered users should be transformed
     *  accordingly. Users can request a platform by name (string). The reply will be delivered
     *  asynchronously via UnityEvent (see PlatformReply). If a platform exists for the
     *  requested ID PlatformReply will be invoked instantly including the corresponding
     *  platform's GameObject. Otherwise, a new platform will be instantiated 
     *  via PhotonNetwork.InstantiateRoomObject. This ensures existance and sharability of
     *  control for the platform across all remote clients. After creation, PlatformReply
     *  will be invoked.
     */
    public class NetworkNavigationPlatformManager : MonoBehaviourPunCallbacks
    {
        [Serializable]
        public struct PlatformItem {
            public string platformId;
            public GameObject gameObject;

            public PlatformItem(string pId, GameObject go)
            {
                platformId = pId;
                gameObject = go;
            }

            public bool Equals(PlatformItem other)
            {
                return (platformId.Equals(other.platformId) && gameObject.Equals(other.gameObject));
            }

        }

        public static NetworkNavigationPlatformManager instance;

        public List<PlatformItem> platforms = new List<PlatformItem>();
        
        public class PlatformEvent : UnityEvent<string, GameObject> { }

        public PlatformEvent PlatformReply = new PlatformEvent();

        private void Awake()
        {
            if(instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                FindPlatforms();
            }
        }

        private void FindPlatforms()
        {
            /* 
             * Assuming every platform gameobjects are located at the root level of the scene
             * this add automatically at Awake all available Navigation Platform gameobjects to the manager.
             */
            GameObject[] roots =  UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var item in roots)
            {
                bool platformDetected = item.TryGetComponent(out NavigationPlatform navigationPlatform);
                if (platformDetected)
                {
                    PlatformItem platformItem = new PlatformItem(navigationPlatform.initPlatformId, item);

                    if (!platforms.Contains(platformItem))
                    {
                        platforms.Add(platformItem);
                    }
                }
            }
        }

        /*
         * Called by NetworkNavigationPlatform via RPC.
         * If you want to request a distributed platform see RequestPlatform.
         * This will ensure that NetworkNavigationPlatformManager
         * keeps track of all NetworkNavigationPlatforms created by remote
         * clients:
         * 1. remote client adds platform (see RequestPlatform)
         * 2. platformInstance.RPC("SetPlatformId",...) is called
         * 3. RPC arrives at local client (see NetworkNavigationPlatform.SetPlatformId)
         * 4. local client adds platform to NetworkNavigationManager.instance.AddLocalPlatform
         */
        public void AddLocalPlatform(string name, GameObject go) 
        {
            foreach (var item in platforms) {
                if (name == item.platformId)
                    throw new Exception("Plattform '" + name + "' already exists.");
            }
            platforms.Add(new PlatformItem(name, go));
            PlatformReply.Invoke(name, go);
        }

        public GameObject GetPlatform(string platformId) 
        {
            foreach (var item in platforms) 
            {
                if (item.platformId == platformId)
                    return item.gameObject;
            }
            return null;
        }

        /*
         * Adds a new remote platform and invokes a reply with the corresponding 
         * platform's GameObject on given callbackInterface. 
         * If a platform with given name already exists, the existing object will be
         * included in the reply instead.
         */
        public void RequestPlatform(string name, CallbackInterface callbackInterface, string prefabPath = "") 
        {
            PlatformReply.AddListener(callbackInterface.OnPlatformReply);
            var platform = GetPlatform(name);
            if (platform != null)
            {
                PlatformReply.Invoke(name, platform);
            }
            else if (PhotonNetwork.IsMasterClient) 
            {
                if(prefabPath.Length == 0) 
                {
                    prefabPath = "Navigation Assets/NavigationPlatform";
                }
                var go = PhotonNetwork.InstantiateRoomObject(prefabPath, Vector3.zero, Quaternion.identity);
                var p = go.GetComponent<NavigationPlatform>();
                p.photonView.RPC("SetPlatformId", RpcTarget.AllBuffered, new object[] { name });
            }
        }

        public interface CallbackInterface 
        {
            void OnPlatformReply(string name, GameObject go);
        }
    }

}