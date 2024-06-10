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
//   Authors:        Ephraim Schott, Sebastian Muehlhaus, Tim Weissker, Clara Pauline Bimberg
//   Date:           2022
//-----------------------------------------------------------------

using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

namespace Vrsys
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager instance;
        private string logTag;

        public bool verbose = false;

        public bool applyPositionToAddedUserPrefabs = true;

        [Tooltip("Class will attempt to instantiate PlayerPrefs.GetString(\"UserPrefabDir\", \"\") + \" / \" + PlayerPrefs.GetString(\"UserPrefabName\", \"\"). If values are not set, fallback will be used.")]
        public GameObject fallbackUserPrefab;
        [Tooltip("Class will attempt to instantiate PlayerPrefs.GetString(\"UserPrefabDir\", \"\") + \" / \" + PlayerPrefs.GetString(\"UserPrefabName\", \"\"). If values are not set, fallback will be used.")]
        public string fallbackUserPrefabResourceDirectory = "UserPrefabs";

        public List<GameObject> userGameobjects = new List<GameObject>();
        public Dictionary<string, GameObject> userGameobjectDict = new Dictionary<string, GameObject>();

        private string userPrefabPath 
        {
            get 
            {
                string p = PlayerPrefs.GetString("UserPrefabDir", "") + "/" + PlayerPrefs.GetString("UserPrefabName", "");
                return (p == "/" && fallbackUserPrefab != null) ? fallbackUserPrefabResourceDirectory + "/" + fallbackUserPrefab.name : p;
            }
        }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }

            instance = this;
        }

        void Start()
        {
            logTag = GetType().Name;

            if (verbose)
            {
                Debug.Log(logTag + ": User prefab path: " + userPrefabPath);
            }

            if (!NetworkUser.localGameObject)
            {
                InstantiateUser();
            }

            if (PhotonNetwork.IsMasterClient)
            {
                // Since the master client already called OnJoinedRoom() in the launcher scene, we have to call it again here; 
                // other clients will directly trigger the OnJoinedRoom() callback in this class
                OnJoinedRoom();
            }
        }

        private void InstantiateUser()
        {
            GameObject userInstance = PhotonNetwork.Instantiate(userPrefabPath, Vector3.zero, Quaternion.identity, 0);
            if(applyPositionToAddedUserPrefabs)
            {
                userInstance.transform.position = gameObject.transform.position;
                userInstance.transform.rotation = gameObject.transform.rotation;
            }

            userGameobjects.Add(userInstance);
        }

        public override void OnJoinedRoom()
        {
            string connectedRoomName = PhotonNetwork.CurrentRoom.Name;
            if (verbose)
            {
                Debug.Log(logTag + ": Successfully connected to room " + connectedRoomName + ". Have fun!");
                Debug.Log(logTag + ": There are " + (PhotonNetwork.CurrentRoom.PlayerCount - 1) + " other participants in this room.");
            }
        }

        public override void OnLeftRoom()
        {
            /*
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            */
        }

        public override void OnPlayerEnteredRoom(Player other)
        {
            if (verbose)
            {
                Debug.Log(logTag + ": " + other.NickName + " has entered the room.");
            }
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            if (verbose)
            {
                Debug.Log(logTag + ": " + other.NickName + " has left the room.");
            }
        }
    }

}