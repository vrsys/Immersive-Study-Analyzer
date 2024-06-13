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
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Vrsys;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(RecorderController))]
    public class TimeInteractor : MonoBehaviourPun
    {
        public bool inputActivated;
        
        public InputActionProperty pauseDesktop;
        public InputActionProperty rewindDesktop;
        public InputActionProperty forwardDesktop;

        public InputActionProperty timeNavigation;
        public InputActionProperty scaleModification;
        public InputActionProperty timeNavigationActive;
        public InputActionProperty scaleModificationActive;
        public InputActionProperty timeGrab;
        public InputActionProperty pauseHMD;
        public InputActionProperty leftTriggerValue;
        
        private Vector2 _leftThumb;
        private Vector2 _rightThumb;
        
        private OneEuroFilter oneEuroFilter = new OneEuroFilter(1.0f, 1.50f,60.0f,0.7f,0.007f);

        private float _lastTimeInteractionTime;
        private float _lastRayInteractionTime;
        private float _sliderStateChangeTime;

        private float _zoomTime = -1.0f;
        private float _originalZoomTime = -1.0f;

        private RecorderState _state;
        private NetworkController _networkController;

        private float _lastPlaybackTimeShareUpdate;
        private float _lastTimeInteraction;
        private float _lastTimeTeleport;
        private float _stopToggleTime;
        private float _firstThumbstickHigh;

        // time grab variables
        private bool _grabActive;
        private Vector3 _initialHeadPos;
        private Vector3 _initalHandPos;
        private Vector3 _initialHeadUp;
        private float _initialTime;
        private GameObject _openHand;
        private GameObject _clutchedHand;
        private GameObject _timeHandle;
        private GameObject _slider;
        private GameObject _localUser;
        private GameObject _localUserHead;
        private GameObject _localUserLeftHand;
        private GameObject _localUserRightHand;

        private GameObject _startTime;
        private GameObject _endTime;

        private GameObject _currentSelectedUser;

        private bool _hmd;
        
        public void Start()
        {
            if (!photonView.IsMine)
                return;
            _slider = Utils.GetChildByName(gameObject,"TimeSlider");
            _timeHandle = Utils.GetChildByName(_slider, "Handle");
            _openHand = Utils.GetChildByName(gameObject, "HandOpen");
            _clutchedHand = Utils.GetChildByName(gameObject, "HandClosed");
            _startTime = GameObject.Find("StartTime");       
            _endTime = GameObject.Find("EndTime");

            if (inputActivated)
            {
                pauseDesktop.action.Enable();
                forwardDesktop.action.Enable();
                rewindDesktop.action.Enable();
                timeNavigation.action.Enable();
                scaleModification.action.Enable();
                timeNavigationActive.action.Enable();
                scaleModificationActive.action.Enable();
                timeGrab.action.Enable();
                pauseHMD.action.Enable();
                leftTriggerValue.action.Enable();
            }

            _state = GetComponent<RecorderState>();
            _networkController = GetComponent<NetworkController>();
        }
        
        public void Update()
        {
            if (!photonView.IsMine)
                return;
            if (PhotonNetwork.InRoom && _localUser == null)
            {
                _localUser = NetworkUser.localGameObject;
                _localUserHead = Utils.GetChildBySubstring(_localUser, "Head");
                _localUserLeftHand = Utils.GetChildBySubstring(_localUser, "HandLeft");
                _localUserRightHand = Utils.GetChildBySubstring(_localUser, "HandRight");

                _state.localUserHead = _localUserHead;

                if (_localUserLeftHand != null)
                {
                    XRController controller = _localUserLeftHand.GetComponent<XRController>();

                    if (controller != null && false)
                    {
                        _hmd = true;
                        controller.enableInputTracking = false;
                        controller.enableInputActions = true;
                        controller.controllerNode = XRNode.LeftHand;
                        controller.selectUsage = InputHelpers.Button.Grip;
                        controller.activateUsage = InputHelpers.Button.Trigger;
                        controller.uiPressUsage = InputHelpers.Button.Trigger;
                        XRRayInteractor rayInteractor = _localUserLeftHand.AddComponent<XRRayInteractor>();
                        //XRInteractionManager manager = leftHand.AddComponent<XRInteractionManager>();
                        XRInteractorLineVisual lineVisual = _localUserLeftHand.AddComponent<XRInteractorLineVisual>();
                        LineRenderer renderer = _localUserLeftHand.GetComponent<LineRenderer>();
                        Material mat = Resources.Load<Material>("Record&Replay/UIRayMaterial");
                        renderer.material = mat;
                        lineVisual.lineWidth = 0.02f;
                        //rayInteractor.interactionManager = manager;
                        // Populate the color keys at the relative time 0 and 1 (0 and 100%)

                        Gradient gradient = new Gradient();
                        GradientColorKey[] colorKey;
                        GradientAlphaKey[] alphaKey;

                        colorKey = new GradientColorKey[2];
                        colorKey[0].color = Color.white;
                        colorKey[0].time = 0.0f;
                        colorKey[1].color = Color.white;
                        colorKey[1].time = 1.0f;

                        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
                        alphaKey = new GradientAlphaKey[2];
                        alphaKey[0].alpha = 1.0f;
                        alphaKey[0].time = 0.0f;
                        alphaKey[1].alpha = 0.0f;
                        alphaKey[1].time = 1.0f;

                        gradient.SetKeys(colorKey, alphaKey);

                        lineVisual.validColorGradient = gradient;

                        colorKey = new GradientColorKey[2];
                        colorKey[0].color = Color.red;
                        colorKey[0].time = 0.0f;
                        colorKey[1].color = Color.red;
                        colorKey[1].time = 1.0f;

                        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
                        alphaKey = new GradientAlphaKey[2];
                        alphaKey[0].alpha = 1.0f;
                        alphaKey[0].time = 0.0f;
                        alphaKey[1].alpha = 0.0f;
                        alphaKey[1].time = 1.0f;

                        gradient.SetKeys(colorKey, alphaKey);

                        lineVisual.invalidColorGradient = gradient;
                        rayInteractor.enableUIInteraction = true;
                    }
                }
            }

            if (_state.currentState == State.Idle)
            {
                if ((pauseHMD.action.triggered || pauseDesktop.action.triggered) && Time.time - _stopToggleTime > 0.5)
                {
                    InputController controller = GetComponent<InputController>();
                    controller.TogglePlayback();
                    //ToggleLocalPause();
                    //ToggleGlobalPause();
                    _stopToggleTime = Time.time;
                }
            }
            if(_state.currentState == State.Replaying || _state.currentState == State.PreviewReplay)
                TimeControl();
        }

        public void ProcessValue(float value)
        {
            if (!photonView.IsMine)
                return;
            float deltaT = Mathf.Abs(_state.currentReplayTime - value);
            float deltaP = Mathf.Abs(_state.lastPreviewTime - value);
            float deltaI = Mathf.Abs(Time.time - _lastTimeInteractionTime);
            float deltaRI = Mathf.Abs(Time.time - _lastRayInteractionTime);
            
            bool activatePreview = (deltaT > 0.01f && deltaP > 0.001f) || deltaI <= 0.1f || deltaRI <= 0.1f;

            if (activatePreview && _state.previewMode)
                _state.currentState = State.PreviewReplay;

        }
        
        public float Filter(float time)
        {
            if (_state.currentFilter == InteractionFilter.NoFilter)
                return time;
    
            return oneEuroFilter.Filter(time);
            
        }

        public void ToggleGlobalPause()
        {
            if (!photonView.IsMine)
                return;
            _networkController.TogglePlayPauseReplayOnAllClients(!_state.replayPaused);
        }
        
        public void ToggleLocalPause()
        {
            if (!photonView.IsMine)
                return;
            _state.replayPaused = !_state.replayPaused;
        }
        
        private float TimeInteraction()
        {
            float timeDif = 0.0f;
            float minZoomTime = 10.0f;
            
            _leftThumb = timeNavigation.action.ReadValue<Vector2>() * (Time.deltaTime * 5.0f);
            float x = _leftThumb.magnitude;
            if (timeNavigationActive.action.IsPressed())
                timeDif = _leftThumb.x > 0 ? x : -x;
            
            if (forwardDesktop.action.IsPressed())
                timeDif = Time.deltaTime * 8.0f;

            if (rewindDesktop.action.IsPressed())
                timeDif = -Time.deltaTime * 8.0f;

            if ((pauseHMD.action.triggered || pauseDesktop.action.triggered) && Time.time - _stopToggleTime > 0.5)
            {
                ToggleLocalPause();
                //ToggleGlobalPause();
                _stopToggleTime = Time.time;
            }
            
            return timeDif;
            
            
            _rightThumb = scaleModification.action.ReadValue<Vector2>() * Time.deltaTime;

            float y = _rightThumb.y;

            if (forwardDesktop.action.IsPressed())
                timeDif = Time.deltaTime * 3.0f;

            if (rewindDesktop.action.IsPressed())
                timeDif = -Time.deltaTime * 3.0f;
            
            // stop time by pressing "O"
            if ((pauseHMD.action.triggered || pauseDesktop.action.triggered) && Time.time - _stopToggleTime > 0.5)
            {
                _state.replayPaused = !_state.replayPaused;
                _stopToggleTime = Time.time;
            }
            
            if (_state.currentInteraction == Interaction.SliderZoom)
            {
                if (_zoomTime < 0.0f || _zoomTime > _state.recordingDuration)
                    _zoomTime = _state.recordingDuration;
                
                float minThreshold = 0.35f;
                float zoomFactor = 4.0f;
                float scale = 0.005f;
                
                if (y > minThreshold)
                {
                    y -= minThreshold;
                    _zoomTime -= y * zoomFactor;
                    if (_zoomTime <= minZoomTime)
                        _zoomTime = minZoomTime;
                } else if (y < -minThreshold)
                {
                    y += minThreshold;
                    _zoomTime -= y * zoomFactor;

                    if (_zoomTime >= 2.0f * _state.recordingDuration)
                        _zoomTime = 2.0f * _state.recordingDuration; 
                }

                if (timeNavigationActive.action.IsPressed())
                {
                    if (_leftThumb.x > minThreshold)
                        timeDif = scale * _zoomTime * (x - minThreshold);
                    else if (_leftThumb.x < -minThreshold)
                        timeDif = -scale * _zoomTime * (x + minThreshold);
                    else
                        timeDif = 0.0f;
                }

                float min = 0.0f;
                float max = 0.0f;
                
                if (_zoomTime >= 0.0f && _zoomTime <= 2.0f * _state.recordingDuration)
                {
                    min = _state.currentReplayTime - 0.5f * _zoomTime;
                    max = _state.currentReplayTime + 0.5f * _zoomTime;
                }

                _state.currentMinSliderValue = min < 0.0f ? 0.0f : min;
                _state.currentMaxSliderValue = max >= _state.recordingDuration ? _state.recordingDuration : max;

                if (_state.currentPreviewTime >= 0.0f && Mathf.Abs(_state.currentMinSliderValue - _state.currentPreviewTime) <= 0.5f)
                {
                    if(_originalZoomTime < 0.0f)
                        _originalZoomTime = _zoomTime;
                    // modify zoom time such that current position is in the middle of the time slider if possible
                    _zoomTime = Mathf.Abs(_state.currentReplayTime - _state.currentPreviewTime) * 2.0f;

                    if (_zoomTime < _originalZoomTime)
                    {
                        _zoomTime = _originalZoomTime;
                        _originalZoomTime = -1.0f;
                    }

                    _state.currentMinSliderValue  = _state.currentPreviewTime;
                }

                if (_state.currentPreviewTime >= 0.0f && Mathf.Abs(_state.currentMaxSliderValue - _state.currentPreviewTime) <= 0.5f)
                {
                    if(_originalZoomTime < 0.0f)
                        _originalZoomTime = _zoomTime;
                    // modify zoom time such that current position is in the middle of the time slider if possible
                    _zoomTime = Mathf.Abs(_state.currentReplayTime - _state.currentPreviewTime) * 2.0f;
                
                    if (_zoomTime < _originalZoomTime)
                    {
                        _zoomTime = _originalZoomTime;
                        _originalZoomTime = -1.0f;
                    }
                
                    _state.currentMaxSliderValue = _state.currentPreviewTime;
                }
            }
            
            /*
            if (_state.currentInteraction == Interaction.LinearThumbstick)
                if (timeNavigationActive.action.IsPressed())
                    timeDif = _leftThumb.x > 0 ? x : -x;

            if (_state.currentInteraction == Interaction.NonLinearThumbstick)
            {
                if (timeNavigationActive.action.IsPressed())
                {
                    if (x > 0.95f && _firstThumbstickHigh < 0.0f)
                        _firstThumbstickHigh = Time.time;
                    else
                        _firstThumbstickHigh = -1.0f;

                    float scale = 1.0f;
                    if (_firstThumbstickHigh >= 0.0f)
                        scale += (float)Math.Log((Time.time - _firstThumbstickHigh) / 5.0f + 10.0f);

                    y = 1.0f / (1.0f + (float)Math.Exp(-(x - 0.5f) * 10.0f));
                    y *= scale;
                    timeDif = _leftThumb.x > 0 ? y : -y;
                }
            }

            if (_state.currentInteraction == Interaction.GrabTurn)
            {
                if (!_openHand.activeSelf && !_grabActive)
                    _openHand.SetActive(true);
                
                float scale = 1.0f;
                if (timeGrab.action.IsPressed())
                {
                    if (!_grabActive)
                    {
                        _grabActive = true;
                        _initialHeadPos = _localUserHead.transform.position;
                        _initalHandPos = _localUserLeftHand.transform.position;
                        _initialHeadUp = _localUserHead.transform.up.normalized;
                        _initialTime = _state.currentReplayTime;
                        _openHand.SetActive(false);
                        _clutchedHand.SetActive(true);

                        GameObject handMesh = Utils.GetChildByName(_localUserLeftHand, "HandMesh");
                        MeshRenderer r = handMesh.GetComponent<MeshRenderer>();
                        r.material.SetColor("_Color", Color.blue);
                    }

                    Vector3 currentHandPos = _localUserLeftHand.transform.position;
                    Vector3 initialDir = _initalHandPos - _initialHeadPos;
                    Vector3 currentDir = currentHandPos - _initialHeadPos;
                    float angle = Vector3.Angle(initialDir, currentDir);

                    Vector3 normal = Vector3.Cross(initialDir, _initialHeadUp).normalized;
                    float dot = Vector3.Dot(currentDir, normal);

                    bool isLeft = dot > 0.0f;
                    bool isRight = dot < 0.0f;

                    y = angle < 30.0f ? Mathf.Pow(angle / 9.655f, 3.0f) : angle;

                    float timeComp = _state.currentPreviewTime < 0.0f ? _state.currentReplayTime : _state.currentPreviewTime;
                    if (isLeft)
                        timeDif = _initialTime - y * scale - timeComp;
                    else if (isRight)
                        timeDif = _initialTime + y * scale - timeComp;
                    else
                        timeDif = _initialTime - timeComp;
                    
                }
                else
                {
                    if (_grabActive)
                    {
                        _grabActive = false;
                        _openHand.SetActive(true);
                        _clutchedHand.SetActive(false);
                        GameObject handMesh = Utils.GetChildByName(_localUserLeftHand, "HandMesh");
                        MeshRenderer r = handMesh.GetComponent<MeshRenderer>();
                        r.material.SetColor("_Color", Color.white);
                    }
                }
            }

            if (_state.currentInteraction == Interaction.LinearRay)
            {
                if (timeGrab.action.IsPressed())
                {
                    Vector3 intersect = Utils.IntersectionRayPlane(_localUserLeftHand.transform.position,
                        _localUserLeftHand.transform.forward, _slider.transform.position, _slider.transform.forward);
                    float minThreshold = 0.05f;
                    Vector3 diff = intersect - _timeHandle.transform.position;
                    Vector3 right = _slider.transform.right;
                    Vector3 up = _slider.transform.up;
                    // a -> right, b -> up such hat diff = a * right + b * up 
                    // using linear algebra (2x2 matrix inverse)
                    float s = 1.0f / (right.x * up.y - right.y * up.x);
                    float irx = s * up.y;
                    float iux = s * (-up.x);
                    float iry = s * (-right.y);
                    float iuy = s * right.x;

                    float a = irx * diff.x + iux * diff.y;
                    float b = iry * diff.x + iuy * diff.y;

                    // normalize a and b with respect to the total width of the time slider
                    float maxDist = (_startTime.gameObject.transform.position - _endTime.gameObject.transform.position).magnitude;
                    a /= maxDist;
                    b /= maxDist;

                    if (b < 0.0f)
                        b = -b;

                    if (b > minThreshold)
                        timeDif = a * (1 - (b - minThreshold));
                }
            }

            if (_state.currentInteraction == Interaction.NonLinearRay)
            {
                if (timeGrab.action.IsPressed())
                {
                    Vector3 intersect = Utils.IntersectionRayPlane(_localUserLeftHand.transform.position,
                        _localUserLeftHand.transform.forward, _slider.transform.position, _slider.transform.forward);
                    float minThreshold = 0.05f;
                    Vector3 diff = intersect - _timeHandle.transform.position;
                    Vector3 right = _slider.transform.right;
                    Vector3 up = _slider.transform.up;
                    // a -> right, b -> up such hat diff = a * right + b * up 
                    // using linear algebra (2x2 matrix inverse)
                    float s = 1.0f / (right.x * up.y - right.y * up.x);
                    float irx = s * up.y;
                    float iux = s * (-up.x);
                    float iry = s * (-right.y);
                    float iuy = s * right.x;

                    float a = irx * diff.x + iux * diff.y;
                    float b = iry * diff.x + iuy * diff.y;

                    // normalize a and b with respect to the total width of the time slider
                    float maxDist = (_startTime.gameObject.transform.position - _endTime.gameObject.transform.position)
                        .magnitude;
                    a /= maxDist;
                    b /= maxDist;

                    if (b < 0.0f)
                        b = -b;

                    if (b > minThreshold)
                    {
                        // compute relative change in time
                        x = (b - minThreshold);
                        // modified sigmoid function
                        x = (float)(1.0f / (1.0f + Math.Exp(-20.0f * x + 5.0f)));
                        timeDif = a * (1 - x);
                    }
                }
            }
            
            return timeDif;
            */
        }

        private void SendCurrentTimesToCollaborators()
        {
            if (!photonView.IsMine)
                return;
            
            // update replay time of current user for all other user
            if (Mathf.Abs(Time.time - _lastPlaybackTimeShareUpdate) >= 0.1f)
            {
                _networkController.UpdateUserReplayTimeOnAllClientsEvent(_state.currentReplayTime);
                //_networkController.UpdateUserPreviewTimeOnAllClientsEvent(_state.currentPreviewTime);
                _lastPlaybackTimeShareUpdate = Time.time;
            }
        }
        
        private void RayCastTimeTeleport()
        {
            if (!photonView.IsMine)
                return;
            GameObject selectedUser = null;
            
            if (_currentSelectedUser != null)
                selectedUser = _currentSelectedUser;
            else
            {
                GameObject selection = Utils.RayCastSelection(_localUserLeftHand.transform.position, _localUserLeftHand.transform.forward);
                selectedUser = Utils.IsPartOfUser(selection);
            }

            LineRenderer renderer = null;
            
            if(_localUserLeftHand != null)
                renderer = _localUserLeftHand.GetComponent<LineRenderer>();
            
            if(renderer != null)
                renderer.material.color = selectedUser != null ? Color.blue : Color.white;

            if (selectedUser != null)
            {
                float value = leftTriggerValue.action.ReadValue<float>();

                foreach (KeyValuePair<int, string> kv in _networkController._userNames)
                {
                    if (selectedUser.name.Contains(kv.Value))
                    {
                        int key = kv.Key;
                        float selectedUserTime = _networkController._userReplayTimes[key];
                        
                        if (value >= 0.1f && !timeGrab.action.IsPressed() && Mathf.Abs(Time.time - _lastTimeTeleport) >= 1.0f)
                        {
                            //Debug.Log("Preview of user: " + selectedUser.name + " and time: " + selectedUserTime);
                            
                            if (_currentSelectedUser == null)
                                _currentSelectedUser = selectedUser;
                            
                            _state.currentPreviewTime = selectedUserTime;
                            _lastTimeInteraction = Time.time;
                        }
                        else if (timeGrab.action.triggered && Mathf.Abs(Time.time - _lastTimeTeleport) >= 1.0f)
                        {
                            if (_currentSelectedUser != null)
                                _currentSelectedUser = null;
                            
                            Debug.Log("Teleporting to user: " + selectedUser.name + " and time: " + selectedUserTime);
                            _state.currentPreviewTime = -1.0f;
                            _state.currentReplayTime = selectedUserTime;
                            _lastTimeInteraction = 0.0f;
                            _lastTimeTeleport = Time.time;
                            _state.replayPaused = false;
                        }
                        else if(_currentSelectedUser != null)
                        {
                            _currentSelectedUser = null;
                            Debug.Log("Teleporting to user: " + selectedUser.name + " and time: " + selectedUserTime);
                            _state.currentPreviewTime = -1.0f;
                            _state.currentReplayTime = selectedUserTime;
                            _lastTimeInteraction = 0.0f;
                            _lastTimeTeleport = Time.time;
                            _state.replayPaused = false;
                        }
                    }
                }
            }
        }
        
        private void TimeControl()
        {
            if (!photonView.IsMine)
                return;
            float timeDif = TimeInteraction();

            if (timeDif != 0.0f)
            {
                if (_state.currentState == State.Replaying && _state.previewMode)
                {
                    _state.currentState = State.PreviewReplay;
                    _state.currentPreviewTime = _state.currentReplayTime;
                }

                if (_state.currentState == State.PreviewReplay)
                {
                    if (_state.currentPreviewTime + timeDif < _state.recordingDuration - 0.1f &&
                        _state.currentPreviewTime + timeDif >= 0.0f)
                        _state.currentPreviewTime += timeDif;
                }
                else
                {
                    if (_state.currentReplayTime + timeDif < _state.recordingDuration - 0.1f &&
                        _state.currentReplayTime + timeDif >= 0.0f)
                        _state.currentReplayTime += timeDif;
                }

                _lastTimeInteraction = Time.time;
            }

            if (_state.currentState == State.PreviewReplay && Mathf.Abs(_lastTimeInteraction - Time.time) >= 1.0f)
            {
                _state.currentState = State.Replaying;
                _state.currentReplayTime = _state.currentPreviewTime;
                _state.currentPreviewTime = -1.0f;
                _state.replayPaused = false;
            }

            // this is being done to give the user a bit more control over the time selection
            if (_state.currentState == State.Replaying && !_state.replayPaused)
            {
                if (_state.currentReplayTime + Time.deltaTime >= _state.recordingDuration - 0.1f)
                {
                    //_state.currentReplayTime = 0.01f;
                } else {
                    _state.currentReplayTime += Time.deltaTime;
                }
            }

            if (_state.currentState == State.Replaying && _state.currentPortalTime > 0.0f && !_state.portalReplayPaused)
            {
                _state.currentPortalTime += Time.deltaTime;
            }

            if (_hmd)
                RayCastTimeTeleport();

            if(_state.currentState == State.Replaying || _state.currentState == State.PreviewReplay)
                SendCurrentTimesToCollaborators();
        }
    }
}