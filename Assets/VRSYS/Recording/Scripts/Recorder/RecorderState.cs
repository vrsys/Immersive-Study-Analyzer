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
using System.Collections.Generic;
using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    public enum State
    {
        Idle, Recording, Replaying, PreparingReplay, PrepareRecording, PreviewReplay
    }
    
    public enum Interaction
    {
        LinearThumbstick, NonLinearThumbstick, SliderZoom, LinearRay, NonLinearRay, GrabTurn    
    }

    public enum InteractionFilter
    {
        NoFilter, OneEuroFilter
    }
    
    [Serializable]
    public class ReplayList
    {
        public string[] replayNames = null;
    }
    
    public class RecorderState : MonoBehaviour
    {
        [Tooltip("ID of the recorder.")]
        public int recorderID;
        public State currentState = State.Idle;
        public string recordingDirectory;
        [Tooltip("Name of the recording file created.")]
        public string recordingFile;
        [Tooltip("List of all servers that can be used to upload and download recording files.")]
        public List<String> serverList;
        [Tooltip("The node under which the time portal scene will be created.")]
        public GameObject timePortalNode;
        [Tooltip("The node under which the preview scene will be created.")]
        public GameObject previewNode;
        [Tooltip("Boolean that activates a transparent preview mode during temporal navigation.")]
        public bool previewMode = false;
        [Tooltip("Name of the recording that should be used for playback")]
        public string fixedPlaybackRecordingName = "";
        
        private NetworkController networkController;
        private RecorderController recorderController;
        private TimeInteractor timeInteractor;
        
        [HideInInspector] public Interaction currentInteraction = Interaction.LinearThumbstick;
        [HideInInspector] public InteractionFilter currentFilter = InteractionFilter.NoFilter;
        [HideInInspector] public float recordingStartTime = -1.0f;
        
        [HideInInspector] public float currentRecordingTime = -1.0f;
        [HideInInspector] public float currentReplayTime = -1.0f;
        [HideInInspector] public float currentPortalTime = -1.0f;
        [HideInInspector] public float currentPreviewTime = -1.0f;
        [HideInInspector] public float lastPreviewTime = -1.0f;
        
        [HideInInspector] public float recordingDuration = -1.0f;
        [HideInInspector] public float currentMinSliderValue = -1.0f;
        [HideInInspector] public float currentMaxSliderValue = -1.0f;
        [HideInInspector] public Dictionary<string, GameObject> selectableUsers;
        [HideInInspector] public Dictionary<string, bool> recordedObjectPresent;
        [HideInInspector] public Dictionary<int, GameObject> originalIdGameObjects;
        [HideInInspector] public Dictionary<int, int> newIdOriginalId;
        [HideInInspector] public string selectedServer;
        [HideInInspector] public string selectedUser;
        [HideInInspector] public bool selectedUserUpdated;
        [HideInInspector] public string selectedReplayFile;
        [HideInInspector] public string selectedPartnerReplayFile;
        [HideInInspector] public bool selectedReplayFileUpdated;
        [HideInInspector] public ReplayList replayList;
        [HideInInspector] public GameObject localUserHead;
        [HideInInspector] public GameObject localRecordedUserHead;
        [HideInInspector] public GameObject externalRecordedUserHead;
        [HideInInspector] public bool replayPaused = false;
        [HideInInspector] public bool portalReplayPaused = false;
        
        public void Start()
        {
            networkController = GetComponent<NetworkController>();
            recorderController = GetComponent<RecorderController>();
            timeInteractor = GetComponent<TimeInteractor>();

            originalIdGameObjects = new Dictionary<int, GameObject>();
            newIdOriginalId = new Dictionary<int, int>();
            selectableUsers = new Dictionary<string, GameObject>();
        }
    }
}