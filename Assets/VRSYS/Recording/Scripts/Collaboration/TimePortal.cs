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

using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Vrsys;
using VRSYS.Recording.Scripts;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(NetworkController))]
    [RequireComponent(typeof(RecorderController))]
    [RequireComponent(typeof(RecorderState))]
    public class TimePortal : MonoBehaviourPun
    {
        public InputActionProperty activatePortalAction;
        public GameObject portal;
        public RadialMenuManager radialMenuManager;

        private UIController _uiController;
        private RecorderController _recorderController;
        private RecorderState _state;
        private NetworkController _networkController;
        private float _lastStateChange;
        private float _lastPortalTime = -1.0f;
        private bool _portalActive = false;
        private bool _initialPlaybackStatePaused;
        private bool _initialUIState;
        private AvatarHMDAnatomy _hmdAnatomy;
        private string _selectedUserName;
        private int _selectedUserId = -1;
        private float _selectedUserTime = 3.0f;
        private AudioRecorder _microphoneRecorder;
        private AudioRecorder _audioListenerRecorder;
        private Dictionary<GameObject, GameObject> _externalUserAndPortalExternalUser;
        private TextMeshProUGUI _portalText;

        private TooltipHandler tooltipHandler;

        private Tooltip timeportalTooltip;
        // Start is called before the first frame update
        void Start()
        {
            _state = GetComponent<RecorderState>();
            _networkController = GetComponent<NetworkController>();
            _recorderController = GetComponent<RecorderController>();
            _uiController = GetComponent<UIController>();
            activatePortalAction.action.Enable();
            portal.SetActive(false);

            _portalText = Utils.GetChildBySubstring(gameObject, "PortalTime").GetComponent<TextMeshProUGUI>();

            _externalUserAndPortalExternalUser = new Dictionary<GameObject, GameObject>();
            
            tooltipHandler = NetworkUser.localGameObject.GetComponent<TooltipHandler>();
            
            timeportalTooltip = new Tooltip();
            timeportalTooltip.hand = TooltipHand.Right;
            timeportalTooltip.tooltipName = "TimePortal";
            timeportalTooltip.tooltipText = "TimePortal";
            timeportalTooltip.actionButtonReference = Tooltip.ActionButton.SecondaryButton;
            tooltipHandler.AddTooltip(timeportalTooltip);
        }

        // Update is called once per frame
        void Update()
        {
            HandleInput();
        }

        public void TogglePortalState()
        {
            if(!photonView.IsMine)
                return;
            if (!_portalActive)
                ActivatePortal();
            else
                DeactivatePortal();
        }

        private void HandleInput()
        {
            if (!photonView.IsMine)
                return;
            
            if (_state.currentState == State.Replaying && activatePortalAction.action.triggered &&
                Time.time - _lastStateChange > 0.5f)
            {
                TogglePortalState();
                _lastStateChange = Time.time;
            }

            if (_portalActive && _hmdAnatomy != null)
            {
                _state.timePortalNode.transform.position = Vector3.zero;
                _state.timePortalNode.transform.rotation = Quaternion.identity;
                
                portal.transform.position = _hmdAnatomy.handLeft.transform.position + 0.1f * Vector3.up;
                Vector3 eulerRot = _hmdAnatomy.head.transform.rotation.eulerAngles;
                portal.transform.rotation = Quaternion.Euler(new Vector3(0.0f, eulerRot.y, 0.0f));
                portal.transform.localScale = 0.2f * Vector3.one;

                if (_networkController._userReplayTimes.ContainsKey(_selectedUserId))
                {
                    _selectedUserTime = _networkController._userReplayTimes[_selectedUserId] + (Time.time - _networkController._userReplayTimesUpdateTime[_selectedUserId]);
                    
                    if(Mathf.Abs(_state.currentPortalTime - _selectedUserTime) > 0.3f)
                        _state.currentPortalTime = _selectedUserTime;
                    
                    if (Mathf.Abs(_lastPortalTime - _selectedUserTime) < 0.0001f)
                        _state.portalReplayPaused = true;
                    else
                    {
                        _state.portalReplayPaused = false;
                        _lastPortalTime = _selectedUserTime;
                    }
                }
                else
                {
                    _state.currentPortalTime = _state.recordingDuration / 2.0f;
                }

                _portalText.text = "Portal Time: " + _state.currentPortalTime.ToString("F1");
                foreach (var kv in _externalUserAndPortalExternalUser)
                {
                    if (kv.Key != null && kv.Value != null)
                        Utils.CopyLocalTransformations(kv.Key.transform, kv.Value.transform);
                    else
                        Debug.LogError("Error. Portal external users not correctly set.");
                }
            }
        }

        private void CreateTimePortalObjects()
        {
            Debug.Log("Creating portal");
            if (_state.timePortalNode.transform.childCount == 0)
            {
                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                // clone scene for preview purpose
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    if (rootObjects[i] != null)
                    {
                        bool isRecordingSetup = rootObjects[i].name.Contains("__RECORDING__");
                        bool isNetworkSetup = rootObjects[i].name.Contains("__NETWORKING__");
                        bool isLocalUser = rootObjects[i].name.Contains("[Local User]") &&
                                           !rootObjects[i].name.Contains("[Rec]");
                        bool isExternalUser = rootObjects[i].name.Contains("[External User]") &&
                                              !rootObjects[i].name.Contains("[Rec]");
                        bool isDontDestroyOnLoad = rootObjects[i].name.Contains("DontDestroy");
                        if (!isRecordingSetup && !isNetworkSetup && !isLocalUser && !isDontDestroyOnLoad)
                        {
                            GameObject preview = Instantiate(rootObjects[i]);
                            preview.transform.parent = _state.timePortalNode.transform;
                            preview.name = preview.name.Replace("(Clone)", "");

                            Utils.DestroyNetworkAndCameraComponentsRecursive(preview);
                            Utils.MarkPortalRecorder(preview);
                            Utils.RecursivelySetLayer(preview, LayerMask.NameToLayer("TimePortal"));
                            if (isExternalUser)
                            {
                                Utils.RemoveCustomComponents(preview.transform);
                                _externalUserAndPortalExternalUser.Add(rootObjects[i], preview);
                                Utils.MakeAllChildrenNonTransparent(preview);
                                preview.name += "[Portal]";
                            }
                        }
                    }
                }

                rootObjects = DontDestroySceneAccessor.Instance.GetAllRootsOfDontDestroyOnLoad();
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    if (rootObjects[i] != null)
                    {
                        bool isRecordingSetup = rootObjects[i].name.Contains("__RECORDING__");
                        bool isNetworkSetup = rootObjects[i].name.Contains("__NETWORKING__");
                        bool isLocalUser = rootObjects[i].name.Contains("[Local User]") &&
                                           !rootObjects[i].name.Contains("[Rec]");
                        bool isExternalUser = rootObjects[i].name.Contains("[External User]") &&
                                              !rootObjects[i].name.Contains("[Rec]");
                        bool isDontDestroyOnLoad = rootObjects[i].name.Contains("DontDestroy");
                        if (!isRecordingSetup && !isNetworkSetup && !isLocalUser && !isDontDestroyOnLoad)
                        {
                            GameObject preview = Instantiate(rootObjects[i]);
                            preview.transform.parent = _state.timePortalNode.transform;
                            preview.name = preview.name.Replace("(Clone)", "");

                            Utils.DestroyNetworkAndCameraComponentsRecursive(preview);
                            Utils.MarkPortalRecorder(preview);
                            Utils.RecursivelySetLayer(preview, LayerMask.NameToLayer("TimePortal"));
                            
                            if (isExternalUser)
                            {
                                Utils.RemoveCustomComponents(preview.transform);
                                _externalUserAndPortalExternalUser.Add(rootObjects[i], preview);
                                Utils.MakeAllChildrenNonTransparent(preview);
                                preview.name += "[Portal]";
                            }
                        }
                    }
                }

                _hmdAnatomy = (AvatarHMDAnatomy)NetworkUser.localNetworkUser.avatarAnatomy;
            }
        }

        public void ActivatePortal()
        {
            if (photonView.IsMine)
                photonView.RPC(nameof(ActivatePortalRPC), RpcTarget.All);
        }

        [PunRPC]
        public void ActivatePortalRPC()
        {
            if (!photonView.IsMine)
            {
                // TODO: handle portal representation for other user
                portal.SetActive(true);
            }
            else
            {
                portal.SetActive(true);
                _initialUIState = _uiController.GetUIVisibility();
                _uiController.SetUIVisibility(true);
                CreateTimePortalObjects();
                CreateTimePortalAudio();
                // TODO: fix continuous playback of current time and portal time

                foreach (var kv in _networkController._userReplayTimes)
                {
                    if (kv.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        _selectedUserTime = kv.Value;
                        _selectedUserId = kv.Key;
                        _selectedUserName = PhotonNetwork.CurrentRoom.Players[kv.Key].NickName;
                        break;
                    }
                }

                _state.currentPortalTime = _selectedUserTime;
                _initialPlaybackStatePaused = _state.replayPaused;
                _state.replayPaused = true;
                _portalActive = true;
            }

            radialMenuManager.SetRadialMenuState(false);
        }
        
        public void DeactivatePortal()
        {
            if (photonView.IsMine)
                photonView.RPC(nameof(DeactivatePortalRPC), RpcTarget.All);
        }
        
        [PunRPC]
        public void DeactivatePortalRPC()
        {
            if (!photonView.IsMine)
            {     
                portal.SetActive(false);
            } else {
                _externalUserAndPortalExternalUser.Clear();
                portal.SetActive(false);
                _uiController.SetUIVisibility(_initialUIState);
                DestroyTimePortalObjects();
                DestroyTimePortalAudio();
                _state.replayPaused = _initialPlaybackStatePaused;
                _portalActive = false;
                _state.currentPortalTime = -1.0f;
                _selectedUserId = -1;
                _selectedUserTime = 3.0f;
            }

            radialMenuManager.SetRadialMenuState(true);
        }
        
        private void CreateTimePortalAudio()
        {
            _microphoneRecorder = _state.timePortalNode.AddComponent<AudioRecorder>();
            _microphoneRecorder.SetId(0);
            _microphoneRecorder.Controller = _recorderController;
            _microphoneRecorder.MarkAsPortalRecorder();

            _audioListenerRecorder = _state.timePortalNode.AddComponent<AudioRecorder>();
            _audioListenerRecorder.SetId(1);
            _audioListenerRecorder.Controller = _recorderController;
            _audioListenerRecorder.MarkAsPortalRecorder();
        }

        private void DestroyTimePortalObjects()
        {
            Debug.Log("Destroying portal objects");
            if (_state.timePortalNode.transform.childCount > 0)
            {
                Utils.DestroyChildren(_state.timePortalNode);
            }
        }

        private void DestroyTimePortalAudio()
        {
            Debug.Log("Destroying portal audio");
            Destroy(_microphoneRecorder);
            Destroy(_audioListenerRecorder);
        }
    }
}