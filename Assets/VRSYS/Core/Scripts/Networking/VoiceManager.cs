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

using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;

using TMPro;

namespace Vrsys
{
    public class VoiceManager : MonoBehaviourPunCallbacks
    {
        private static VoiceManager s_Instance = null;
        public static VoiceManager Instance
        {
            get
            {
                return s_Instance;
            }
        }
        private void Awake()
        {
            if (s_Instance != null)
            {
                Destroy(this);

                return;
            }

            s_Instance = this;
        }

        public List<string> availableMicrophones;
        public Recorder userRecorder;
        public string selectedMicrophone;
        public bool isMuted = false;
        public bool isRecording = false;

        void Start()
        {
            userRecorder = GetComponent<Recorder>();

            availableMicrophones = Microphone.devices.ToList();
            userRecorder.TransmitEnabled = !isMuted;
            if (availableMicrophones.Count > 0)
            {
                userRecorder.UnityMicrophoneDevice = availableMicrophones[0];
                userRecorder.RestartRecording();
                isRecording = true;
            }
            else
            {
                Debug.LogWarning("No available microphones detected.");
            }

            Debug.Log("Connecting Voice...");
            GetComponent<ConnectAndJoin>().RoomName = PhotonNetwork.CurrentRoom.Name;
            GetComponent<ConnectAndJoin>().ConnectNow();
            userRecorder.Init(GetComponent<VoiceConnection>());
        }

        public override void OnJoinedRoom()
        {

        }

        public List<string> GetMicrophones()
        {
            availableMicrophones = Microphone.devices.ToList();
            return availableMicrophones;
        }

        public void SetMicrophone(string microphone)
        {
            if (availableMicrophones.Contains(microphone))
            {
                selectedMicrophone = microphone;
                userRecorder.UnityMicrophoneDevice = selectedMicrophone;
                userRecorder.RestartRecording();
            }
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            userRecorder.TransmitEnabled = !isMuted;
        }

        public void StartRecording()
        {
            if (!isRecording)
            {
                userRecorder.RestartRecording();
            }
        }

        public void StopRecording()
        {
            if (isRecording)
            {
                userRecorder.StopRecording();
            }
        }

    }
}