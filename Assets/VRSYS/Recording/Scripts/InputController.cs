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

using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRSYS.Scripts.Recording
{
    
    [RequireComponent(typeof(RecorderController))]
    public class InputController : MonoBehaviourPun
    {
        public bool inputEnabled = true;
        public RecorderState state;

        public InputActionProperty recordDesktop;
        public InputActionProperty replayDesktop;
        public InputActionProperty switchUserDesktop;
        public InputActionProperty switchReplayFileDesktop;

        public InputActionProperty recordHMD;
        public InputActionProperty replayHMD;
        public InputActionProperty switchUserHMD;
        public InputActionProperty switchInteractionHMD;
        public InputActionProperty switchFilterHMD;
        
        private float _interactionToggleTime;
        private float _recordingToggleTime;
        private float _replayToggleTime;
        private float _userSwitchToggleTime;

        private RecorderController _controller;
        
        public void Start()
        {
            if (!photonView.IsMine)
                return;
            
            if (inputEnabled)
            {
                recordDesktop.action.Enable();
                replayDesktop.action.Enable();
                switchUserDesktop.action.Enable();
                recordHMD.action.Enable();
                replayHMD.action.Enable();
                switchUserHMD.action.Enable();
                switchInteractionHMD.action.Enable();
                switchFilterHMD.action.Enable();
                switchReplayFileDesktop.action.Enable();
            }

            _controller = GetComponent<RecorderController>();
        }

        public void Update()
        {
            if (!photonView.IsMine)
                return;
            HandleInput();
        }

        public void TogglePlayback()
        {
            if (!photonView.IsMine)
                return;
            
            if (state.currentState == State.Idle)
            {
                Debug.Log("Sending start replay/download event to all clients.");
                _controller.PrepareAndStartDistributedReplay();
            }
            else if (state.currentState == State.Replaying)
            {
                Debug.Log("Sending end replay event to all clients.");
                _controller.SendEndReplayEvent();
            }
        }

        public void ToggleRecording()
        {
            if (!photonView.IsMine)
                return;
            
            bool localRecording = true;
            
            if (state.currentState == State.Idle)
            {
                Debug.Log("Sending start recording event to all clients.");
                
                if (localRecording)
                {
                    _controller.PrepareRecording();
                    _controller.StartRecording();
                }
                else 
                    _controller.SendStartRecordingEvents();
            }
            else if (state.currentState == State.Recording)
            {
                Debug.Log("Sending end recording event to all clients.");
                _controller.EndRecording();
            }
        }

        public void ToggleFileSelectionSwitch()
        {
            if (!photonView.IsMine)
                return;
            
            bool found = false;
            for (int i = 0; i < state.replayList.replayNames.Length; ++i)
            {
                if (state.replayList.replayNames[i] == state.selectedReplayFile)
                {
                    state.selectedReplayFile = state.replayList.replayNames[(i + 1) % state.replayList.replayNames.Length];
                    found = true;
                    break;
                }
            }

            if (!found && state.replayList.replayNames.Length > 0)
            {
                state.selectedReplayFile = state.replayList.replayNames[0];
            }

            state.selectedReplayFileUpdated = true;
        }
        
        private void HandleInput()
        {
            if (!photonView.IsMine)
                return;
            
            // modify interaction method
            if (switchInteractionHMD.action.triggered)
            {
                bool stateSwitch = false;

                if (Time.time - _interactionToggleTime > 0.5)
                {
                    _interactionToggleTime = Time.time;
                    stateSwitch = true;
                }

                if (stateSwitch)
                {
                    switch (state.currentInteraction)
                    {
                        case Interaction.LinearThumbstick:
                            state.currentInteraction = Interaction.NonLinearThumbstick;
                            break;
                        case Interaction.NonLinearThumbstick:
                            state.currentInteraction = Interaction.SliderZoom;
                            break;
                        case Interaction.SliderZoom:
                            state.currentInteraction = Interaction.LinearThumbstick;
                            break;
                        case Interaction.LinearRay:
                            state.currentInteraction = Interaction.NonLinearRay;
                            break;
                        case Interaction.NonLinearRay:
                            state.currentInteraction = Interaction.GrabTurn;
                            break;
                        case Interaction.GrabTurn:
                            state.currentInteraction = Interaction.LinearThumbstick;
                            break;
                    }
                }
            }
            
            if (switchFilterHMD.action.triggered)
            {
                bool stateSwitch = false;

                if (Time.time - _interactionToggleTime > 0.5)
                {
                    _interactionToggleTime = Time.time;
                    stateSwitch = true;
                }

                if (stateSwitch)
                {
                    switch (state.currentFilter)
                    {
                        case InteractionFilter.NoFilter:
                            state.currentFilter = InteractionFilter.OneEuroFilter;
                            break;
                        case InteractionFilter.OneEuroFilter:
                            state.currentFilter = InteractionFilter.NoFilter;
                            break;
                    }
                }
            }

            if (switchReplayFileDesktop.action.triggered)
                ToggleFileSelectionSwitch();
            
            // start/end recording
            if (recordDesktop.action.triggered || recordHMD.action.triggered)
            {
                Debug.LogWarning("Trying to start/stop recording");
                bool stateSwitch = false;

                if (Time.time - _recordingToggleTime > 0.5)
                {
                    _recordingToggleTime = Time.time;
                    stateSwitch = true;
                }

                if(stateSwitch)
                    ToggleRecording();
            }

            // start/end replay
            if ((replayDesktop.action.triggered || replayHMD.action.triggered && PhotonNetwork.IsMasterClient))
            {
                Debug.LogWarning("Trying to start/stop replay");
                bool stateSwitch = false;
                if (Time.time - _replayToggleTime > 0.5)
                {
                    _replayToggleTime = Time.time;
                    stateSwitch = true;
                }
                
                if(stateSwitch)
                    TogglePlayback();
            }

            // change selected user
            if (switchUserDesktop.action.triggered || switchUserHMD.action.triggered)
            {
                if (state.currentState == State.Replaying)
                {
                    bool stateSwitch = false;

                    if (Time.time - _userSwitchToggleTime > 0.5)
                    {
                        _userSwitchToggleTime = Time.time;
                        stateSwitch = true;
                    }

                    if (stateSwitch)
                    {
                        int index = 0;
                        foreach (var kv in state.selectableUsers)
                        {
                            if (kv.Key == state.selectedUser)
                            {
                                break;
                            }

                            index++;
                        }

                        index = (index + 1) % state.selectableUsers.Count;

                        int i = 0;
                        foreach (var kv in state.selectableUsers)
                        {
                            if (i == index)
                            {
                                state.selectedUser = kv.Key;
                                state.selectedUserUpdated = true;
                                break;
                            }

                            i++;
                        }
                    }
                }
            }
        }
    }
}