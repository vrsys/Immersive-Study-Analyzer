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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vrsys 
{
    public class NetworkSetup : MonoBehaviourPunCallbacks
    {
        [Header("Photon App Settings")]
        [Tooltip("Configure version and update rate.")]
        public string appVersion = "1";
        public bool autoSyncScene = true;
        public int networkSendRate = 20;
        public int networkSerializationRate = 15;
        public byte maxUsers = 10;

        [Header("User Settings")]
        [Tooltip("Configure user name, user prefab, color and avatar.")]
        public string userName = "";

        [Tooltip("OnJoinRoom User Prefab will be stored by name to PlayerPrefs. Make sure User Prefab Resource Directory is set accordingly.")]
        public SupportedDeviceType selectedDeviceType;
        public SupportedDevices[] supportedUserDevices;
        public Dictionary<SupportedDeviceType, GameObject> supportedDevices = new Dictionary<SupportedDeviceType, GameObject>();

        //public GameObject userPrefab;
        [Tooltip("OnJoinRoom User Prefab will be stored by name to PlayerPrefs. Therefore, User Prefab Resource Directory should correspond to the resource directory containing the User Prefab.")]
        public string userPrefabResourceDirectory = "UserPrefabs";

        [Header("Start Settings")]
        public bool autoStart = true;
        public bool networkEnabled = true;
        public string roomName = "";

        [Header("Optional Settings")]
        public bool keepSceneAfterStart = false;
        public string startScene = "";
        public bool verbose = false;

        private Dictionary<string, int> usersPerRoom;
        private ConnectionState connectionStatus = ConnectionState.Disconnected;
        private bool createdRoom = false;
        private string logTag = "";

        // Event that is called when the room list was updated
        public event UpdatedRoomsEvent OnUpdatedRooms;
        public delegate void UpdatedRoomsEvent();

        public event ConnectionStatusChangedEvent OnConnectionStatusChanged;
        public delegate void ConnectionStatusChangedEvent();

        [Serializable]
        public struct SupportedDevices
        {
            public SupportedDeviceType device;
            public GameObject userPrefab;
        }

        public enum ConnectionState
        {
            Disabled,
            Disconnected,
            Disconnecting,
            JoiningLobby,
            JoinedLobby,
            Connecting,
            Connected
        }



        void Awake()
        {
            if (PhotonNetwork.IsConnected)
                return;
                

            logTag = GetType().Name;

            // Connect to server and setup photon auto-sync, send rate etc.
            Connect();

            usersPerRoom = new Dictionary<string, int>();
            supportedDevices = new Dictionary<SupportedDeviceType, GameObject>();
            foreach (var supportedUserDevice in supportedUserDevices)
            {
                supportedDevices.Add(supportedUserDevice.device, supportedUserDevice.userPrefab);
            }

        }

        private void Start()
        {
            //if (PhotonNetwork.IsConnected)
            //{
            //    OnJoinedLobby();
            //}
        }

        public override void OnConnectedToMaster()
        {
            if (verbose)
            {
                Debug.Log(logTag + ": Connected to master!");
            }

            if (networkEnabled)
            {
                PhotonNetwork.JoinLobby();
                SetConnectionStatus(ConnectionState.JoiningLobby);
            }
            base.OnConnectedToMaster();
        }

        public override void OnConnected()
        {
            SetConnectionStatus(ConnectionState.Connected);
            base.OnConnected();
        }

        public override void OnJoinedLobby()
        {
            SetConnectionStatus(ConnectionState.JoinedLobby);
            if (autoStart)
            {
                JoinOrCreateRoom();
            }
        }

        public override void OnLeftLobby()
        {
            // If network is not enabled disconnect from photon
            if (!networkEnabled && PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            SetConnectionStatus(ConnectionState.Disconnecting);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            if (cause == DisconnectCause.DisconnectByClientLogic)
            {
                PhotonNetwork.OfflineMode = true;
                SetConnectionStatus(ConnectionState.Disabled);
            }
            base.OnDisconnected(cause);
        }

        public override void OnCreatedRoom()
        {
            if (verbose)
            {
                Debug.Log(logTag + ": Room created");
            }

            createdRoom = true;
            LoadStartScene();
        }

        public override void OnJoinedRoom()
        {
            if (verbose)
            {
                Debug.Log(logTag + ": Room joined");
            }

            if (!createdRoom && !autoSyncScene)
            {
                LoadStartScene();
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            usersPerRoom = new Dictionary<string, int>();
            foreach (RoomInfo info in roomList)
            {
                usersPerRoom.Add(info.Name, info.PlayerCount);
            }

            // Call event that rooms updated
            if (OnUpdatedRooms != null)
            {
                OnUpdatedRooms();
            }
        }

        private void Connect()
        {
            Debug.Log("Connecting...");
            PhotonNetwork.AutomaticallySyncScene = autoSyncScene;
            if (!PhotonNetwork.IsConnected)
            {
                // Connect to Photon Master Server
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = appVersion;
                SetConnectionStatus(ConnectionState.Connecting);
            }
            PhotonNetwork.SendRate = networkSendRate;
            PhotonNetwork.SerializationRate = networkSerializationRate;
        }

        private void SetConnectionStatus(ConnectionState status)
        {
            connectionStatus = status;
            if (OnConnectionStatusChanged != null)
            {
                OnConnectionStatusChanged();
            }
        }

        public void EnableNetworking()
        {
            if (connectionStatus == ConnectionState.Disconnected || connectionStatus == ConnectionState.Disabled)
            {
                networkEnabled = true;
                PhotonNetwork.OfflineMode = false;
                Connect();
            }
        }

        public void DisableNetworking()
        {
            if (connectionStatus == ConnectionState.Connected || connectionStatus == ConnectionState.JoinedLobby)
            {
                networkEnabled = false;
                if (PhotonNetwork.InLobby)
                {
                    PhotonNetwork.LeaveLobby(); // leave lobby before disconnect can be called
                }
                else if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.Disconnect();
                }
            }
        }

        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
        }

        public bool IsNetworkEnabled()
        {
            return networkEnabled;
        }

        public bool IsConnected()
        {
            return PhotonNetwork.IsConnected;
        }
        public ConnectionState GetConnectionStatus()
        {
            return connectionStatus;
        }

        public Dictionary<string, int> GetUsersPerRoom()
        {
            return usersPerRoom;
        }

        public void JoinOrCreateRoom()
        {

            if (userName == "")
            {
                userName = "DefaultUser" + UnityEngine.Random.Range(0, 10000).ToString();
            }

            if (roomName == "")
            {
                roomName = "Room" + UnityEngine.Random.Range(0, 10000).ToString();
            }

            PhotonNetwork.NickName = userName;
            PlayerPrefs.SetString("UserName", userName);
            PlayerPrefs.SetString("RoomName", roomName);

            if (supportedDevices.ContainsKey(selectedDeviceType)){

                PlayerPrefs.SetString("UserPrefabName", supportedDevices[selectedDeviceType].name);
                PlayerPrefs.SetString("UserPrefabDir", userPrefabResourceDirectory);
            }

            if (verbose)
            {
                Debug.Log(logTag + ": Joining Room '" + roomName + "' as User '" + userName + "':");
            }

            bool sent = PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxUsers, PublishUserId = true }, TypedLobby.Default);
            if(!sent)
                Debug.LogError("JoinOrCreateRoom operation not queued!");
        }

        private void LoadStartScene()
        {
            if (keepSceneAfterStart)
            {
                if (verbose)
                {
                    Debug.Log(logTag + ": Keeping this scene");
                }
            }
            else
            {
                if (startScene.Length > 0)
                {
                    PhotonNetwork.LoadLevel(startScene);
                    if (verbose)
                    {
                        Debug.Log(logTag + ": Loading Scene '" + startScene + "'");
                    }
                }
                else
                {
                    PhotonNetwork.LoadLevel(1);
                    if (verbose)
                    {
                        Debug.Log(logTag + ": Loading Scene '" + SceneManager.GetSceneAt(1).name + "'");
                    }
                }
            }
        }
    }
}
