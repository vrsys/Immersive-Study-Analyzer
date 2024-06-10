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
//   Authors:        Ephraim Schott, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Vrsys
{
    public class NetworkLobbyMenu : MonoBehaviour
    {
        // editor options

        [Header("Required Settings")]
        public NetworkSetup networkSetup;

        // UI

        private TMP_InputField userNameInput;
        private TMP_InputField addRoomInput;
        private ToggleGroup colorToggleGroup;
        private TMP_Dropdown deviceDrop;
        private TMP_Dropdown roomDrop;
        private Toggle networkSwitch;
        private TMP_Text statusText;
        private Button singleStartButton;
        private Button createJoinButton;
        private Button addRoomButton;
        private GameObject singleUserStartPanel;
        private GameObject multiUserStartPanel;

        // extracted options 

        private bool networkEnabled = true;
        private List<SupportedDeviceType> availableDevices = new List<SupportedDeviceType>();
        private List<string> deviceDropList;
        private List<string> rooms;

        // connection state management

        private NetworkSetup.ConnectionState connectionStatus;
        private Color notConnectedColor;
        private Color connectedColor;

        private void Awake()
        {
            if (networkSetup == null)
            {
                networkSetup = GameObject.Find("NetworkSetup").GetComponent<NetworkSetup>();
            }

            if (userNameInput == null)
            {
                userNameInput = GameObject.Find("UserNameInput").GetComponent<TMP_InputField>();
            }

            if (addRoomInput == null)
            {
                addRoomInput = GameObject.Find("AddRoomInput").GetComponent<TMP_InputField>();
            }

            if (colorToggleGroup == null)
            {
                colorToggleGroup = GameObject.Find("UserColorsPanel").GetComponent<ToggleGroup>();
            }

            if (deviceDrop == null)
            {
                deviceDrop = GameObject.Find("DeviceDrop").GetComponent<TMP_Dropdown>();
            }

            if (roomDrop == null)
            {
                roomDrop = GameObject.Find("RoomDrop").GetComponent<TMP_Dropdown>();
            }

            if (networkSwitch == null)
            {
                networkSwitch = GameObject.Find("NetworkToggle").GetComponent<Toggle>();
            }

            if (statusText == null)
            {
                statusText = GameObject.Find("StatusText").GetComponent<TMP_Text>();
            }

            if (singleStartButton == null)
            {
                singleStartButton = GameObject.Find("SingleStartButton").GetComponent<Button>();
            }

            if (createJoinButton == null)
            {
                createJoinButton = GameObject.Find("CreateJoinButton").GetComponent<Button>();
            }

            if (addRoomButton == null)
            {
                addRoomButton = GameObject.Find("AddRoomButton").GetComponent<Button>();
            }

            if (singleUserStartPanel == null)
            {
                singleUserStartPanel = GameObject.Find("SingleUserPanel");
            }

            if (multiUserStartPanel == null)
            {
                multiUserStartPanel = GameObject.Find("MultiUserPanel");
            }

            // Setup Listeners
            singleStartButton.onClick.AddListener(delegate { StartSingleApplication(); });
            createJoinButton.onClick.AddListener(delegate { CreateOrJoinRoom(); });
            addRoomButton.onClick.AddListener(delegate { AddRoom(); });
            networkSwitch.onValueChanged.AddListener(delegate { ToggleNetwork(); });

            networkSwitch.enabled = networkSetup.networkEnabled;
            if (networkSwitch.isOn)
            {
                networkEnabled = true;
                singleUserStartPanel.SetActive(false);
            }
            else
            {
                networkEnabled = false;
                multiUserStartPanel.SetActive(false);
            }

            deviceDropList = new List<string>();
            rooms = new List<string> { "Gropius Room", "Schiller Room", "Wieland Room" };
            notConnectedColor = new Color32(156, 0, 0, 255);
            connectedColor = new Color32(49, 255, 57, 255);
        }

        void Start()
        {
            UpdateXRDeviceDropdown();
            UpdateRooms();
        }

        private void OnEnable()
        {
            networkSetup.OnUpdatedRooms += UpdateRooms;
            networkSetup.OnConnectionStatusChanged += UpdateConnectionStatusText;
        }

        private void OnDisable()
        {
            networkSetup.OnUpdatedRooms -= UpdateRooms;
            networkSetup.OnConnectionStatusChanged -= UpdateConnectionStatusText;
        }

        private void UpdateXRDeviceDropdown()
        {
            //
            //if (SystemInfo.deviceType == DeviceType.Desktop)
            //{
            //    availableDevices.Add(SupportedDeviceType.Desktop);
            //}


            //if (SystemInfo.deviceType == DeviceType.Handheld)
            //{
            //    availableDevices.Add(SupportedDeviceType.AR);
            //}

#if UNITY_2019
            // Unity 2019
            if (XRDevice.isPresent)
            {
                Debug.Log("HMD DETECTED");
                availableDevices.Add("HMD");
            }
            else
            {
                Debug.Log("HMD NOT DETECTED");
            }
#endif

            // Unity 2020
            //var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            //SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
            //foreach (var xrDisplay in xrDisplaySubsystems)
            //{
            //    Debug.Log(xrDisplay.SubsystemDescriptor.id);
            //    if (xrDisplay.SubsystemDescriptor.id.Equals("OpenXR Display"))
            //    {
            //        availableDevices.Add(SupportedDeviceType.HMD);
            //    }

            //    if (xrDisplay.SubsystemDescriptor.id.Equals("OpenVR Display"))
            //    {
            //        availableDevices.Add(SupportedDeviceType.HMD);
            //    }
            //    if (xrDisplay.running)
            //    {
            //        if (xrDisplay.SubsystemDescriptor.id.Equals("oculus display"))
            //        {
            //            availableDevices.Add(SupportedDeviceType.HMD);
            //        }
            //    }
            //}


            foreach (var item in networkSetup.supportedUserDevices)
            {
                availableDevices.Add(item.device);

            }

            foreach (SupportedDeviceType device in availableDevices)
            {
                if (networkSetup.supportedDevices.ContainsKey(device)) 
                {
                    deviceDropList.Add(device.ToString());
                }
            }

            deviceDrop.ClearOptions();
            deviceDrop.AddOptions(deviceDropList);
            deviceDrop.value = deviceDropList.IndexOf(networkSetup.selectedDeviceType.ToString());
        }


        public void ToggleNetwork()
        {
            networkEnabled = !networkEnabled;
            if (networkEnabled)
            {
                singleUserStartPanel.SetActive(false);
                multiUserStartPanel.SetActive(true);
                networkSetup.EnableNetworking();
            }
            else
            {
                singleUserStartPanel.SetActive(true);
                multiUserStartPanel.SetActive(false);
                networkSetup.DisableNetworking();
            }
        }



        public void AddRoom()
        {
            if (addRoomInput.text != "")
            {
                string newRoomName = addRoomInput.text;
                if (!rooms.Contains(newRoomName)) rooms.Add(newRoomName);

                addRoomInput.text = "";
            }
            UpdateRooms();
            roomDrop.value = rooms.Count;
        }

        public void UpdateConnectionStatusText()
        {
            connectionStatus = networkSetup.GetConnectionStatus();
            statusText.text = ConnectionStateToString(connectionStatus);
            if (connectionStatus == NetworkSetup.ConnectionState.Disabled || connectionStatus == NetworkSetup.ConnectionState.JoinedLobby)
            {
                createJoinButton.interactable = true;
                networkSwitch.interactable = true;
            }
            else
            {
                createJoinButton.interactable = false;
                networkSwitch.interactable = false;
            }

            if (networkSetup.IsNetworkEnabled())
            {
                statusText.color = connectedColor;
            }
            else
            {
                statusText.color = notConnectedColor;
            }
        }

        public void UpdateRooms()
        {
            roomDrop.ClearOptions();
            Dictionary<string, int> usersPerRoom = networkSetup.GetUsersPerRoom();
            int maxUsersPerRoom = networkSetup.maxUsers;

            // Add available online rooms to the default rooms
            foreach (KeyValuePair<string, int> room in usersPerRoom)
            {
                if (!rooms.Contains(room.Key)) rooms.Add(room.Key);
            }

            // Create room options from rooms list and add the user numbers
            List<string> roomDropOptions = new List<string>();
            foreach (string room in rooms)
            {
                if (!usersPerRoom.ContainsKey(room))
                {
                    roomDropOptions.Add(room + " (0/" + maxUsersPerRoom.ToString() + ")");

                }
                else
                {
                    roomDropOptions.Add(room + " (" + usersPerRoom[room].ToString() + "/" + maxUsersPerRoom.ToString() + ")");
                }
            }

            // Add the options created in the List above
            roomDrop.AddOptions(roomDropOptions);
        }

        private void PrepareStart()
        {

            // Assign default user name 
            if (userNameInput.text == "")
            {
                networkSetup.userName = "DefaultUser" + UnityEngine.Random.Range(0, 10000).ToString();
            }
            else
            {
                networkSetup.userName = userNameInput.text;
            }

            // Set settings
            networkSetup.roomName = FormatRoomName(roomDrop.options[roomDrop.value].text);
            SupportedDeviceType currentDeviceType = (SupportedDeviceType)Enum.Parse(typeof(SupportedDeviceType), deviceDrop.options[deviceDrop.value].text);
            networkSetup.selectedDeviceType = currentDeviceType;
            IEnumerator<Toggle> toggleEnum = colorToggleGroup.ActiveToggles().GetEnumerator();
            toggleEnum.MoveNext();
            PlayerPrefs.SetString("UserColor", toggleEnum.Current.name);
        }

        private string FormatRoomName(string originalRoomName)
        {
            List<string> roomNameParts = originalRoomName.Split(new string[] { "(" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            roomNameParts.RemoveAt(roomNameParts.Count - 1);
            string formattedRoomName = string.Join("(", roomNameParts);
            formattedRoomName = formattedRoomName.Remove(formattedRoomName.Length - 1);
            //formattedRoomName.Remove(formattedRoomName.Length - 1);
            return formattedRoomName;
        }

        public string ConnectionStateToString(NetworkSetup.ConnectionState state)
        {
            return state.ToString();
        }

        public void StartSingleApplication()
        {
            PrepareStart();
            networkSetup.JoinOrCreateRoom();
        }

        public void CreateOrJoinRoom()
        {
            PrepareStart();
            networkSetup.JoinOrCreateRoom();
        }
    }

}