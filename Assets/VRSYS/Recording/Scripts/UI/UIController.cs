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
using System.Linq;
using System.Threading;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vrsys;
using VRSYS.Recording.Scripts;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(RecorderState))]
    [RequireComponent(typeof(TimeInteractor))]
    [RequireComponent(typeof(NetworkController))]
    public class UIController : MonoBehaviourPun
    {
        public GameObject recordingUICanvas;
        public FPSComputer fpsComputer;
        public float hmdUIScaleFactor = 0.1f;
        public float hmdUIDistance = 0.8f;
        
        private RecorderState _state;
        private TimeInteractor _timeInteractor;
        private NetworkController _networkController;
        
        private const String Left = "HandLeft";
        private const String Right = "HandRight";
        private const String Camera = "Main Camera";
        private String _uiParent = Camera;

        private Vector3 initalUIParentRotation;
        private Matrix4x4 lastDiff;
        private Vector3 targetUIParentRotation;
        private float rotationStartTime = -1.0f;
        private float rotationEndTime = -1.0f;
        private Vector3 initalUIParentPosition;
        private float positionStartTime = -1.0f;
        private float positionEndTime = -1.0f;
        private GameObject _localUser;
        private bool firstUI = true;
        
        private GameObject slider;
        private GameObject timeHandle;
        private GameObject currentTimeHandle;

        private Slider sliderComponent;
        private RectTransform currentTimeHandleRect;
        private RectTransform sliderRect;

        private GameObject curveParent;
        private GameObject timeLinesParent;

        private Text time;
        private Text startTime;
        private Text endTime;
        private Text subtitle;

        private GameObject openHand;
        private GameObject clutchedHand;

        private Dictionary<string, RectTransform> _lineElements = new Dictionary<string, RectTransform>();
        private Dictionary<string, RectTransform> _textElements = new Dictionary<string, RectTransform>();
        private Dictionary<string, Text> _textElementTexts = new Dictionary<string, Text>();
        
        private bool _isHmd = false;
        private bool _timeLineCollabActive = false;
        private bool uiActive = false;
        private int sliderWidth;
        private bool drawnOnce;
        
        private float _uiToggleTime;
        public InputActionProperty toggleUI;

        private Canvas _canvas;
        private TooltipHandler tooltipHandler;
        private Tooltip toggleUITooltip;
        
        public bool checkNetworkSetup = false;
        
        public void Start()
        {
            if (!photonView.IsMine)
            {
                recordingUICanvas.SetActive(false);
                return;
            }

            tooltipHandler = NetworkUser.localGameObject.GetComponent<TooltipHandler>();
            
            toggleUITooltip = new Tooltip();
            toggleUITooltip.hand = TooltipHand.Left;
            toggleUITooltip.tooltipName = "TimeLine";
            toggleUITooltip.tooltipText = "TimeLine";
            toggleUITooltip.actionButtonReference = Tooltip.ActionButton.Grip;
            tooltipHandler.AddTooltip(toggleUITooltip);
            
            _state = GetComponent<RecorderState>();
            _timeInteractor = GetComponent<TimeInteractor>();
            _networkController = GetComponent<NetworkController>();
            
            toggleUI.action.Enable();
            
            if (checkNetworkSetup){
                
                GameObject networkSetup = Utils.GetGameObjectBySubstring("Network Setup");
                NetworkSetup setup = networkSetup.GetComponent<NetworkSetup>();
            
                if (setup.selectedDeviceType == SupportedDeviceType.BaseAnalysisHMD || setup.selectedDeviceType == SupportedDeviceType.AdvancedAnalysisHMD)
                    _isHmd = true;
            }
            else
            {
                _isHmd = true;
            }
            

            slider = Utils.GetChildByName(recordingUICanvas,"TimeSlider");
            subtitle = Utils.GetChildByName(recordingUICanvas,"Speech2Text").GetComponent<Text>();
            startTime =  Utils.GetChildByName(recordingUICanvas, "StartTime").GetComponent<Text>();       
            endTime =  Utils.GetChildByName(recordingUICanvas,"EndTime").GetComponent<Text>();

            sliderComponent = slider.GetComponent<Slider>();
            sliderRect = slider.GetComponent<RectTransform>();
            curveParent = Utils.GetChildByName(slider, "Curve");
            currentTimeHandle = Utils.GetChildByName(slider, "CurrentTimeHandle");
            timeLinesParent = Utils.GetChildByName(slider, "TimeLines");
            
            sliderComponent.onValueChanged.AddListener(delegate(float val) { SetSliderStatus(val); });

            timeHandle = Utils.GetChildByName(slider, "Handle");
            time = Utils.GetChildByName(timeHandle, "Time").GetComponent<Text>();
            openHand = Utils.GetChildByName(timeHandle, "HandOpen");
            clutchedHand = Utils.GetChildByName(timeHandle, "HandClosed");
            currentTimeHandleRect = currentTimeHandle.GetComponent<RectTransform>();

            sliderWidth = Screen.width - 200;
            if (_isHmd)
                sliderWidth = Screen.width / 2;
            sliderWidth = Mathf.Max(sliderWidth, 2000);
            sliderRect.sizeDelta = new Vector2(sliderWidth, sliderRect.sizeDelta.y);
            sliderRect = Utils.GetChildBySubstring(slider, "StartTime").GetComponent<RectTransform>();
            sliderRect.localPosition = new Vector3(-sliderWidth / 2, sliderRect.localPosition.y, sliderRect.localPosition.z);
            sliderRect = Utils.GetChildBySubstring(slider, "EndTime").GetComponent<RectTransform>();
            sliderRect.localPosition = new Vector3(sliderWidth / 2, sliderRect.localPosition.y, sliderRect.localPosition.z);

            float buttonOffset = 50.0f;
            GameObject goToStartButtonGo = new GameObject("GoToStartButton");
            goToStartButtonGo.transform.SetParent(slider.transform, false);
            Button goToStartButton = goToStartButtonGo.AddComponent<Button>();
            goToStartButton.onClick.AddListener(() => NavigateToStart());
            Image startButtonImage = goToStartButton.AddComponent<Image>();
            startButtonImage.sprite = Resources.Load<Sprite>("Textures/go_to_beginning");
            goToStartButton.image = startButtonImage;
            RectTransform goToStartRectT = goToStartButtonGo.GetComponent<RectTransform>();
            goToStartRectT.localScale = 0.5f * Vector3.one;
            goToStartRectT.localPosition = new Vector3(-sliderWidth / 2 - buttonOffset, sliderRect.localPosition.y, sliderRect.localPosition.z);
            goToStartButtonGo.transform.SetParent(recordingUICanvas.transform, true);
            
            GameObject goToEndButtonGo = new GameObject("GoToEndButton");
            goToEndButtonGo.transform.SetParent(slider.transform, false);
            Button goToEndButton = goToEndButtonGo.AddComponent<Button>();
            goToEndButton.onClick.AddListener(() => NavigateToEnd());
            Image endButtonImage = goToEndButton.AddComponent<Image>();
            endButtonImage.sprite = Resources.Load<Sprite>("Textures/go_to_end");
            goToEndButton.image = endButtonImage;
            RectTransform goToEndRectT = goToEndButton.GetComponent<RectTransform>();
            goToEndRectT.localScale = 0.5f * Vector3.one;
            goToEndRectT.localPosition = new Vector3(sliderWidth / 2 + buttonOffset, sliderRect.localPosition.y, sliderRect.localPosition.z);
            goToEndButtonGo.transform.SetParent(recordingUICanvas.transform, true);
            
            slider.SetActive(false);
            
            openHand.SetActive(false);
            clutchedHand.SetActive(false);
            recordingUICanvas.SetActive(false);

            subtitle.enabled = false;
            
            _canvas = recordingUICanvas.GetComponent<Canvas>();
        }

        private void NavigateToStart()
        {
            if(_state.currentState == State.Replaying)
                _state.currentReplayTime = 0.1f;
        }
        
        private void NavigateToEnd()
        {
            if(_state.currentState == State.Replaying && _state.recordingDuration >= 0.0f)
                _state.currentReplayTime = _state.recordingDuration - 0.1f;
        }
        
        public void Update()
        {
            if (!photonView.IsMine)
                return;
            if (PhotonNetwork.InRoom && _isHmd && _localUser == null)
            {
                _localUser = NetworkUser.localNetworkUser.gameObject;
                Camera camera = null;
                if (_localUser != null)
                {
                    AvatarHMDAnatomy anatomy = _localUser.GetComponent<AvatarHMDAnatomy>();
                    if(anatomy != null)
                        _canvas.worldCamera =  anatomy.head.GetComponentInParent<Camera>();
                }
            }

            if (_state.currentState == State.Idle)
            {
                slider.SetActive(false);

                if(_timeLineCollabActive)
                    RemoveTimeLineCollaborators();
            }
            
            if (_state.currentState == State.Recording)
            {
                slider.SetActive(false);
            }
            
            
            if (_state.currentState == State.Replaying)
            {
                if (toggleUI.action.triggered && Time.time - _uiToggleTime > 0.5)
                {
                    ToggleUIVisibility();
                    _uiToggleTime = Time.time;
                }
                
                time.text = _state.currentReplayTime.ToString("F1");
                if(_state.currentMinSliderValue > 60.0f)
                    startTime.text = (_state.currentMinSliderValue / 60.0f).ToString("F2");
                else 
                    startTime.text = _state.currentMinSliderValue.ToString("F2");
                if(_state.currentMaxSliderValue > 60.0f)
                    endTime.text = (_state.currentMaxSliderValue / 60.0f).ToString("F2");
                else 
                    endTime.text = _state.currentMaxSliderValue.ToString("F2");
                slider.SetActive(true);
                
                //time.text = "";
                //startTime.text = "";
                //endTime.text = "";

                TimeLineCollaborators();

                sliderComponent.minValue = _state.currentMinSliderValue;
                sliderComponent.maxValue = _state.currentMaxSliderValue;
                sliderComponent.value = _state.currentReplayTime;
            }

            if (_state.currentState == State.PreviewReplay)
            {
                time.text = _state.currentPreviewTime.ToString("F1");
                startTime.text = _state.currentMinSliderValue.ToString("F2");
                endTime.text = _state.currentMaxSliderValue.ToString("F2");
                slider.SetActive(true);
                
                TimeLineCollaborators();
                
                sliderComponent.minValue = _state.currentMinSliderValue;
                sliderComponent.maxValue = _state.currentMaxSliderValue;
                sliderComponent.value = _state.currentPreviewTime;
            }
        }

        public void ToggleUIVisibility()
        {
            SetUIVisibility(!uiActive);
        }
        
        public bool GetUIVisibility()
        {
            return uiActive;
        }
        
        public void SetUIVisibility(bool state)
        {
            uiActive = state;
            recordingUICanvas.SetActive(uiActive);
            Debug.Log("UI turned on: " + uiActive);
        }
        
        public void LateUpdate()
        {
            if (!photonView.IsMine)
                return;
            if (PhotonNetwork.InRoom)
            {
                if (!_isHmd && _canvas.renderMode != RenderMode.ScreenSpaceCamera)
                {
                    _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
                else
                {
                    if(_canvas.renderMode != RenderMode.WorldSpace)
                        _canvas.renderMode = RenderMode.WorldSpace;

                    GameObject parent = null;
                    bool slowFixedMovement = false;
                    if (_uiParent == Left)
                    {
                        parent = ((ViewingSetupHMDAnatomy) NetworkUser.localNetworkUser.viewingSetupAnatomy).leftController;
                        hmdUIDistance = 0.4f;
                    } else if (_uiParent == Right)
                    {
                        parent = ((ViewingSetupHMDAnatomy) NetworkUser.localNetworkUser.viewingSetupAnatomy).rightController;
                        hmdUIDistance = 0.4f;
                    } else if (_uiParent == Camera)
                    {
                        parent = NetworkUser.localHead;
                        slowFixedMovement = true;
                        hmdUIDistance = 0.4f;
                    }
                    
                    if (parent != null)
                    {
                        GameObject uiParent = Utils.GetChildByName(gameObject, "UIParent");
                        Transform parentTransform = parent.transform;
                        Transform uiParentTransform = uiParent.transform;
                        if (!slowFixedMovement)
                        {
                            uiParentTransform.position = parentTransform.position;
                            uiParentTransform.rotation = parentTransform.rotation;
                            
                            Vector3 scaleFactor = uiParentTransform.parent.lossyScale;
                            Vector3 newLocalScale = new Vector3 (
                                parentTransform.lossyScale.x / scaleFactor.x,
                                parentTransform.lossyScale.y / scaleFactor.y,
                                parentTransform.lossyScale.z / scaleFactor.z
                            );
                            
                            uiParentTransform.localScale = newLocalScale;
                        }
                        else
                        {
                            Vector3 currentTargetPosition = parentTransform.position - new Vector3(0.0f, 0.15f, 0.0f);
                            Vector3 currentTargetRotation = new Vector3(0.0f, parentTransform.rotation.eulerAngles.y, 0.0f);

                            if (firstUI)
                            {
                                uiParentTransform.position = currentTargetPosition;
                                uiParentTransform.rotation = Quaternion.Euler(currentTargetRotation);
                                firstUI = false;
                            }
                            
                            Vector3 posDif = uiParentTransform.position - currentTargetPosition;
                            float teleportationMin = 2.0f;
                            float maxHeadDist = 0.3f;
                            bool teleportation = posDif.magnitude > teleportationMin;
                            bool validXRange = Mathf.Abs(posDif.x) > maxHeadDist && !teleportation;
                            bool validZRange = Mathf.Abs(posDif.z) > maxHeadDist && !teleportation;


                            if ((validXRange || validZRange) && positionStartTime < 0.0f)
                            {
                                initalUIParentPosition = uiParentTransform.position;
                                positionStartTime = Time.time;
                                positionEndTime = positionStartTime + 1.0f;
                            }

                            if (positionStartTime > 0.0f && Time.time <= positionEndTime)
                            {
                                float t = (Time.time - positionStartTime) / (positionEndTime - positionStartTime);
                                uiParentTransform.position = Vector3.Slerp(initalUIParentPosition, currentTargetPosition, t);;
                            } else if (Time.time > positionEndTime)
                            {
                                positionStartTime = -1.0f;
                                positionEndTime = -1.0f;
                            }

                            float angle = 60.0f;
                            
                            float diffR = (initalUIParentRotation.y - currentTargetRotation.y) % 360.0f;
                            if (diffR < 0.0f)
                                diffR += 360.0f;
                            float diffL = (currentTargetRotation.y - initalUIParentRotation.y) % 360.0f;
                            if (diffL < 0.0f)
                                diffL += 360.0f;
                            
                            bool diffLAct = diffL > angle && diffL < 180.0f && !teleportation;
                            bool diffRAct = diffR > angle && diffR < 180.0f && !teleportation;
                            
                            if ((diffLAct || diffRAct) && rotationStartTime < 0.0f)
                            {
                                initalUIParentRotation = uiParentTransform.rotation.eulerAngles;
                                targetUIParentRotation = currentTargetRotation;
                                
                                rotationStartTime = Time.time;
                                rotationEndTime = rotationStartTime + 1.0f;
                            }

                            if (rotationStartTime > 0.0f && Time.time <= rotationEndTime)
                            {
                                float t = (Time.time - rotationStartTime) / (rotationEndTime - rotationStartTime);
                                uiParentTransform.rotation = Quaternion.Slerp(Quaternion.Euler(initalUIParentRotation), Quaternion.Euler(currentTargetRotation), t);
                            } else if (Time.time > rotationEndTime)
                            {
                                rotationStartTime = -1.0f;
                                rotationEndTime = -1.0f;
                                initalUIParentRotation = targetUIParentRotation;
                            }

                            if (teleportation)
                            {
                                Matrix4x4 trs = (lastDiff * parentTransform.worldToLocalMatrix).inverse;
                                if (trs.ValidTRS())
                                {
                                    uiParentTransform.position = trs * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                                    uiParentTransform.rotation = trs.rotation;
                                    uiParentTransform.localScale = Vector3.one;
                                }
                            }

                            lastDiff = uiParentTransform.worldToLocalMatrix * parentTransform.localToWorldMatrix;
                            uiParentTransform.localScale = Vector3.one;
                        }
                    }
                    else
                    {
                        Debug.LogError("Error! Current camera not known");
                    }

            
                    RectTransform rectTransform = recordingUICanvas.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.localPosition = new Vector3(0.0f, 0.0f, hmdUIDistance);
                    rectTransform.localRotation = Quaternion.identity;
                    rectTransform.localScale = Vector3.one * 0.5f / sliderWidth * 0.6f;
                    
                }
            }   
        }
        
        // see: https://forum.unity.com/threads/any-good-way-to-draw-lines-between-ui-elements.317902/
        private void MakeLine(float ax, float ay, float bx, float by, Color col, int index, GameObject parent, string name) {
           
            name += index;
            if (!_lineElements.ContainsKey(name))
            {
                GameObject line = new GameObject();
                line.name = name;

                Image image = line.AddComponent<Image>();
                image.sprite = Resources.Load<Sprite>("Record&Replay/Sprite");
                image.color = col;
                
                RectTransform new_rect = line.GetComponent<RectTransform>();
                new_rect.SetParent(parent.transform);
                new_rect.localScale = Vector3.one;
                _lineElements.Add(name, new_rect);
            }
            
            RectTransform rect = _lineElements[name];
            if(!rect.gameObject.activeSelf)
                rect.gameObject.SetActive(true);
            
            Vector3 a = new Vector3(ax, ay, 0.0f);
            Vector3 b = new Vector3(bx, by, 0.0f);
            
            rect.localPosition = (a + b) / 2;
            Vector3 dif = a - b;
            rect.sizeDelta = new Vector3(dif.magnitude, 10.0f);
            rect.localRotation = Quaternion.Euler(new Vector3(0, 0, 180 * Mathf.Atan(dif.y / dif.x) / Mathf.PI));
        }

        private void MakeText(float x, float y, int index, string inputText, GameObject parent, string name) {
           
            name += index;
            if (!_textElements.ContainsKey(name))
            {
                GameObject textGo = new GameObject();
                textGo.name = name;

                Text text = textGo.AddComponent<Text>();
                text.text = inputText;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 30;
                text.color = Color.black;
                text.alignment = TextAnchor.MiddleCenter;
                
                RectTransform new_rect = textGo.GetComponent<RectTransform>();
                new_rect.SetParent(parent.transform);
                new_rect.localScale = Vector3.one;
                _textElements.Add(name, new_rect);
                
                _textElementTexts.Add(name, text);
            }
            
            RectTransform rect = _textElements[name];
            if(!rect.gameObject.activeSelf)
                rect.gameObject.SetActive(true);
            rect.localPosition = new Vector3(x, y, 0.0f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 90.0f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40.0f);
            rect.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            Text t = _textElementTexts[name];
            t.text = inputText;
        }
        
        private void DeactivateLines(GameObject parent)
        {
            if(parent != null)
                parent.SetActive(false);
        }
        
        private void ActivateLines(GameObject parent)
        {
            if(parent != null)
                parent.SetActive(true);
        }

        private void DeactivateTimeLines()
        {
            if (!photonView.IsMine)
                return;
            foreach (var kv in _lineElements)
            {
                if (kv.Key.Contains("Time"))
                {
                    kv.Value.gameObject.SetActive(false);
                }
            }    
        }
        
        private void DeactivateTimeLineText()
        {
            if (!photonView.IsMine)
                return;
            foreach (var kv in _textElements)
            {
                kv.Value.gameObject.SetActive(false);
            }    
        }

        private void DrawPreviewArc(string name, Color color, RectTransform sliderRect, Slider slider, GameObject currentTimeHandle, RectTransform currentTimeHandleRect, GameObject curveParent, float playbackTime, float previewTime)
        {
            if (!photonView.IsMine)
                return;
            Vector3 rectPos = currentTimeHandleRect.localPosition;
            float width = sliderWidth;
            float range = _state.currentMaxSliderValue - _state.currentMinSliderValue;
            
            float t = (playbackTime - _state.currentMinSliderValue) / range;
            float d = (previewTime - _state.currentMinSliderValue) / range;
            
            if (d >= 0.0f && d <= 1.0f)
            {
                ActivateLines(curveParent);
                        
                if(!currentTimeHandle.activeSelf)
                    currentTimeHandle.SetActive(true);
                        
                rectPos = new Vector3(t * width - width/2, rectPos.y, rectPos.z);
                currentTimeHandleRect.localPosition = rectPos;
                Vector3 startPos = new Vector3(t * width - width/2, rectPos.y, rectPos.z);
                Vector3 endPos = new Vector3(d * width - width/2, rectPos.y, rectPos.z);

                float mid = (0.5f * startPos.x + 0.5f * endPos.x);
                float height = 200.0f * Math.Abs(d - t);
                float m = height / ((startPos.x - mid) * (startPos.x - mid));
                    
                for (int i = 0; i < 100; ++i)
                {
                    Vector3 curPos = (1 - i / 100.0f) * startPos + i / 100.0f * endPos;
                    Vector3 nextPos = (1 - (i+1) / 100.0f) * startPos + (i+1) / 100.0f * endPos;

                    float x = curPos.x - mid;
                    float xn = nextPos.x - mid;
                      
                    curPos.y += - m * (x * x) + height;
                    nextPos.y += - m * (xn * xn) + height;
                        
                    MakeLine(curPos.x, curPos.y, nextPos.x, nextPos.y, color, i, curveParent, name);
                    //curvePoints.SetValue(new Vector3(curPos.x, curPos.y - (i - 50), curPos.z), i);
                }
            }
            else
            {
                DeactivateLines(curveParent);
                currentTimeHandle.SetActive(false);
            }
        }
       
        private void DrawTimeLines()
        {
            if (!photonView.IsMine)
                return;
            if(drawnOnce)
                return;
            
            Vector3 rectPos = currentTimeHandleRect.localPosition;

            float width = sliderWidth; //sliderRect.rect.width;
            float range = sliderComponent.maxValue - sliderComponent.minValue;
            
            int desiredSegmentCount = 5;
            float lineHeight = 30.0f;
            
            float segmentLength = range / (float)desiredSegmentCount;
            
            if (segmentLength >= 300.0f)
                segmentLength = 300.0f;
            else if (segmentLength >= 60.0f)
                segmentLength = 60.0f;
            else if (segmentLength >= 30.0f)
                segmentLength = 30.0f;
            else if (segmentLength >= 15.0f)
                segmentLength = 15.0f;
            else if (segmentLength >= 10.0f)
                segmentLength = 10.0f;
            else if (segmentLength >= 5.0f)
                segmentLength = 5.0f;
            else if (segmentLength >= 2.0f)
                segmentLength = 2.0f;
            else
                segmentLength = 1.0f;

            float segmentCount = Mathf.Floor(range / segmentLength) + 1;
            
            Vector3 curPos;
            Vector3 startPos = new Vector3( - width/2, rectPos.y, rectPos.z);
            Vector3 endPos = new Vector3( width/2, rectPos.y, rectPos.z);

            float firstTime = Mathf.Ceil(sliderComponent.minValue / segmentLength) * segmentLength;

            DeactivateTimeLines();
            DeactivateTimeLineText();
            
            for (int i = 0; i < segmentCount; ++i)
            {
                float currentTime = firstTime + i * segmentLength;
                if(currentTime > sliderComponent.maxValue)
                    break;
                float t = (currentTime - sliderComponent.minValue) / range;
                curPos = (1.0f - t) * startPos + t * endPos;

                MakeLine(curPos.x, curPos.y - 30.0f, curPos.x, curPos.y - 30.0f - lineHeight, Color.gray, i, timeLinesParent, "Time");
               
                string timeText;
                if (segmentLength >= 60.0f)
                    timeText = (currentTime / 60.0f).ToString("F0") + "m";
                else 
                    timeText = currentTime.ToString("F0") + "s";

                MakeText(curPos.x, curPos.y - 45.0f - lineHeight, i, timeText, timeLinesParent, "Text");
                //curvePoints.SetValue(new Vector3(curPos.x, curPos.y - (i - 50), curPos.z), i);
                
                drawnOnce = true;
            }
        }
        
        private void SetSliderStatus(float value)
        {
            if (value <= 0.1f)
                value = 0.1f;
            
            if (!photonView.IsMine)
                return;
            _timeInteractor.ProcessValue(value);
            
            if (_state.currentState == State.PreviewReplay)
            {
                float output = _timeInteractor.Filter(value);
                sliderComponent.value = output;

                _state.lastPreviewTime = _state.currentPreviewTime;
                _state.currentPreviewTime = output;

                if (Math.Abs(_state.currentReplayTime - _state.currentPreviewTime) > 0.005f)
                {
                    if(!curveParent.activeSelf)
                        curveParent.SetActive(true);
                    DrawPreviewArc("Local", Color.blue, sliderRect, sliderComponent, currentTimeHandle, currentTimeHandleRect, curveParent, _state.currentReplayTime, _state.currentPreviewTime);
                }
            }
            else
            {
                DeactivateLines(curveParent);
                currentTimeHandle.SetActive(false);
                _state.currentReplayTime = value;
            }
            
            DrawTimeLines();
        }

        public void NavigateToTime(Slider userSliderComponent)
        {
            Debug.LogError("Listener called");

            if(_state.currentState == State.Replaying && 0 <= userSliderComponent.value && userSliderComponent.value <= _state.recordingDuration)
                _state.currentReplayTime = userSliderComponent.value;
            else 
                Debug.LogWarning("Cannot navigate to target ime. Incorrect state or target time.");
        }
        
        private void TimeLineCollaborators()
        {
            if (!photonView.IsMine)
                return;
            _timeLineCollabActive = true;
            
            // update slider position for all other users currently present in the replay
            foreach (var key in _networkController._userReplayTimes.Keys.ToList())
            {
                if (key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (PhotonNetwork.CurrentRoom.Players.ContainsKey(key))
                    {
                        float userTime = _networkController._userReplayTimes[key];
                        float userPreviewTime = -1.0f;//_networkController._userPreviewTimes[key];
                        string userName = PhotonNetwork.CurrentRoom.Players[key].NickName;

                        GameObject tSlider = Utils.GetChildByName(recordingUICanvas, "TimeSlider" + key);
                        GameObject tSliderButton = Utils.GetChildByName(recordingUICanvas, "TimeSliderButton" + key);
                        GameObject user = Utils.GetGameObjectBySubstring(userName);

                        // create time slider for user if the slider does not exist yet
                        if (tSlider == null)
                        {
                            tSlider = Instantiate(slider);
                            Slider userSliderComponent = tSlider.GetComponent<Slider>();
                            tSlider.name = "TimeSlider" + key;
                            tSlider.transform.parent = slider.transform.parent;
                            tSlider.transform.localPosition = slider.transform.localPosition;
                            tSlider.transform.localRotation = slider.transform.localRotation;
                            RectTransform rectTransform = tSlider.GetComponent<RectTransform>();
                            RectTransform sliderTransform = slider.GetComponent<RectTransform>();
                            rectTransform.localPosition = sliderTransform.localPosition;
                            rectTransform.localScale = sliderTransform.localScale;
                            rectTransform.localRotation = sliderTransform.localRotation;
                            
                            //float offset = 10.0f;
                            //Vector3 position = rectTransform.localPosition;
                            //rectTransform.localPosition = new Vector3(position.x, position.y + offset, position.z);
                            
                            GameObject background = Utils.GetChildBySubstring(tSlider, "Background");
                            GameObject fill = Utils.GetChildBySubstring(tSlider, "Fill");
                            GameObject startT = Utils.GetChildBySubstring(tSlider, "StartTime");
                            GameObject endT = Utils.GetChildBySubstring(tSlider, "EndTime");
                            GameObject handleArea = Utils.GetChildBySubstring(tSlider, "Handle Slide Area");
                            GameObject timeLines = Utils.GetChildBySubstring(handleArea, "TimeLines");
                            GameObject handle = Utils.GetChildBySubstring(handleArea, "Handle");

                            tSliderButton = new GameObject("TimeSliderButton" + key);
                            tSliderButton.transform.SetParent(tSlider.transform.parent, false);
                            tSliderButton.transform.localPosition = Vector3.zero;
                            Button timeSliderButton = tSliderButton.AddComponent<Button>();
                            Image image = tSliderButton.AddComponent<Image>();
                            image.material.mainTexture = handle.GetComponent<Image>().mainTexture;
                            timeSliderButton.image = image;
                           
                            
                            if (timeSliderButton != null)
                            {
                                timeSliderButton.onClick.AddListener(() => NavigateToTime(userSliderComponent));
                                userSliderComponent.interactable = true;
                                Debug.Log("Listener set");
                            }

                            Image i = handle.GetComponent<Image>();
                            if (i != null)
                            {
                                AvatarAnatomy anatomy = user.GetComponent<AvatarAnatomy>();
                                if (anatomy != null)
                                {
                                    i.color = anatomy.GetColor().Value;
                                    if (!_networkController._userColors.ContainsKey(key))
                                        _networkController._userColors[key] = i.color;
                                }
                                else
                                    i.color = Color.green;
                            }
                            
                            ColorBlock colors = new ColorBlock();
                            colors.normalColor = timeSliderButton.image.color;
                            timeSliderButton.colors = colors;
                            
                            background.SetActive(false);
                            fill.SetActive(false);
                            startT.SetActive(false);
                            endT.SetActive(false);
                            timeLines.SetActive(false);
                            Debug.Log("New slider created for user: " + key);
                        }

                        Slider userSlider = tSlider.GetComponent<Slider>();
                        userSlider.minValue = _state.currentMinSliderValue;
                        userSlider.maxValue = _state.currentMaxSliderValue;
                        userSlider.value = userTime;
                        userSlider.interactable = false;
                        userSlider.transition = Selectable.Transition.None;

                        GameObject tHandleArea = Utils.GetChildBySubstring(tSlider, "Handle Slide Area");
                        GameObject tSliderHandle = Utils.GetChildBySubstring(tHandleArea, "Handle");
                        tSliderButton.transform.position = tSliderHandle.transform.position;

                        GameObject userCurTimeHandle = Utils.GetChildBySubstring(tSlider, "CurrentTimeHandle");
                        RectTransform userCurTimeHandleRec = userCurTimeHandle.GetComponent<RectTransform>();
                        GameObject curve = Utils.GetChildByName(tSlider, "Curve");
                        
                        if (userPreviewTime >= 0.0f && Mathf.Abs(userTime - userPreviewTime) >= 0.5f)
                        {
                            if (curve != null)
                            {
                                userSlider.value = userPreviewTime;
                                GameObject curT = Utils.GetChildBySubstring(userCurTimeHandle, "CurrentTime");
                                curT.GetComponent<Text>().text = userTime.ToString("F1");
                                DrawPreviewArc("External"+key,_networkController._userColors[key], sliderRect, userSlider, userCurTimeHandle, userCurTimeHandleRec, curve, userTime, userPreviewTime);
                            }
                            else
                            {
                                Debug.LogError("Error! Preview arc cannot be created for user: " + key);
                            }
                        }
                        else
                        {
                            if(curve != null)
                                curve.SetActive(false);
                            if(userCurTimeHandle != null)
                                userCurTimeHandle.SetActive(false);
                        }
         

                        Utils.GetChildBySubstring(tSlider, "Time").GetComponent<Text>().text = "#" + key;
                        
                        float deltaT = Mathf.Abs(_state.currentReplayTime - userTime);
                        if (_state.currentPreviewTime > 0.0f)
                            deltaT = Mathf.Abs(_state.currentPreviewTime - userTime);
                        
                        if (deltaT >= 0.05f || true)
                            tSlider.SetActive(true);
                        
                        // display time of the user if time difference is greater than a certain threshold
                        if (Mathf.Abs(_state.currentReplayTime - userTime) <= 1.0f)
                            tSlider.SetActive(false);
                    }
                    else
                    {
                        GameObject tSlider = Utils.GetChildByName(recordingUICanvas, "TimeSlider" + key);
                        if(tSlider != null)
                            Destroy(tSlider);
                        GameObject tSliderButton = Utils.GetChildByName(recordingUICanvas, "TimeSliderButton" + key);
                        if(tSliderButton != null)
                            Destroy(tSliderButton);
                    }
                }
            }
        }

        private void RemoveTimeLineCollaborators()
        {
            if (!photonView.IsMine)
                return;
            // update slider position for all other users currently present in the replay
            foreach (var key in _networkController._userReplayTimes.Keys.ToList())
            {
                if (key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    GameObject tSlider = Utils.GetChildByName(recordingUICanvas, "TimeSlider" + key);
                    if(tSlider != null)
                        Destroy(tSlider);
                }
            }

            _timeLineCollabActive = false;
        }
    }
}