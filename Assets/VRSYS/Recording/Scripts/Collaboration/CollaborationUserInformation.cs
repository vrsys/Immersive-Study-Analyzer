using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Vrsys.Scripts.Recording;
using VRSYS.Scripts.Recording;

namespace VRSYS.Recording.Scripts
{
    [RequireComponent(typeof(RecorderState))]
    [RequireComponent(typeof(NetworkController))]
    public class CollaborationUserInformation : MonoBehaviourPun
    {
        [Tooltip("Visualisation to indicate whether a collaborator is located at a different point in time")]
        public bool timeDifferenceVisualization = false;
        [Tooltip("Visualisation to indicate whether a collaborator can see the user")]
        public bool visibilityVisualization = false;
        [Tooltip("Visualisation to indicate whether a collaborator currently hears audio above a certain level")]
        public bool audioPerceptionVisualization = false;
        
        private RecorderState _state;
        private NetworkController _networkController;
        private AmplitudeMeasurement _amplitudeMeasurement;
        private Dictionary<int, float> _userAlpha;
        private Dictionary<int, GameObject> _userGameobject;
        private Dictionary<int, bool> _userVisibility;
        private Dictionary<string, CameraVisibilityInformation> _userCameraVisibilityInformation;
        private bool _timeDifferenceVisualisationActive;
        private bool _visibilityVisualisationActive;
        private bool _audioPerceptionVisualisationActive;
        private float _lastAudioShare = 0.0f;
        private float _lastVisibilityShare = 0.0f;
        private float _lastVisibilityVisualisationUpdate;

        private void Start()
        {
            if (!photonView.IsMine)
                return;
            
            _state = GetComponent<RecorderState>();
            _networkController = GetComponent<NetworkController>();

            AudioListener listener = FindObjectOfType<AudioListener>();
            if(listener != null)
                _amplitudeMeasurement = listener.gameObject.AddComponent<AmplitudeMeasurement>();

            _userVisibility = new Dictionary<int, bool>();
            _userCameraVisibilityInformation = new Dictionary<string, CameraVisibilityInformation>();
            _userGameobject = new Dictionary<int, GameObject>();
            _userAlpha = new Dictionary<int, float>();
        }

        public void ToggleAudioVisualization()
        {
            if (!photonView.IsMine)
                return;
            audioPerceptionVisualization = !audioPerceptionVisualization;
        }
        
        public void ToggleVisibilityVisualization()
        {
            if (!photonView.IsMine)
                return;
            visibilityVisualization = !visibilityVisualization;
        }
        
        public void ToggleTimeDifferenceVisualization()
        {
            if (!photonView.IsMine)
                return;
            timeDifferenceVisualization = !timeDifferenceVisualization;
        }
        
        public void Update()
        {
            if (!photonView.IsMine)
                return;
            
            if (_state.currentState == State.Replaying || _state.currentState == State.PreviewReplay)
            {
                VisualiseCollaborationInformation();
                ShareUserAudioLevel();
                ShareUserVisibility();
            }

        }

        private void ShareUserVisibility()
        {
            if (!photonView.IsMine)
                return;
            if (_networkController != null)
            {
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Players != null)
                {
                    if (Time.time - _lastVisibilityShare >= 0.1f)
                    {
                        foreach (var kv in PhotonNetwork.CurrentRoom.Players)
                        {
                            if (kv.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                            {
                                string userName = PhotonNetwork.CurrentRoom.Players[kv.Key].NickName;
                                if (!_userCameraVisibilityInformation.ContainsKey(userName))
                                {
                                    if(!_userGameobject.ContainsKey(kv.Key))
                                        _userGameobject.Add(kv.Key, Utils.GetGameObjectBySubstring(userName + " [External User]"));
                                    GameObject user = _userGameobject[kv.Key];
                                    
                                    CameraVisibilityInformation visibilityInformation = user.GetComponent<CameraVisibilityInformation>();
                                    if (visibilityInformation != null)
                                        _userCameraVisibilityInformation[userName] = visibilityInformation;
                                    else
                                        Debug.LogError("Error! Camera visibility information not set");
                                }
                                else
                                {
                                    bool visibility = _userCameraVisibilityInformation[userName].visible;
                                    if (!_userVisibility.ContainsKey(kv.Key))
                                        _userVisibility[kv.Key] = visibility;

                                    if (visibility != _userVisibility[kv.Key])
                                    {
                                        _userVisibility[kv.Key] = visibility;
                                        _networkController.UpdateUserVisibilityOnAllClientsEvent(kv.Key,
                                            _userVisibility[kv.Key]);
                                    }
                                }
                            }
                        }
                        _lastVisibilityShare = Time.time;
                    }
                }
            }
        }
        
        private void ShareUserAudioLevel()
        {
            if (!photonView.IsMine)
                return;
            if (_networkController != null)
            {
                if (_amplitudeMeasurement != null)
                {
                    if (Time.time - _lastAudioShare >= 0.1f)
                    {
                        _networkController.UpdateUserAudioLevelOnAllClientsEvent(_amplitudeMeasurement
                            .averageAmplitude);
                        _lastAudioShare = Time.time;
                    }
                }
                else
                {
                    GameObject listener = FindObjectOfType<AudioListener>().gameObject;
                    _amplitudeMeasurement = listener.AddComponent<AmplitudeMeasurement>();
                }
            }
            else
                _networkController = GetComponent<NetworkController>();
        }
        
        private void VisualiseCollaborationInformation()
        {
            if (!photonView.IsMine)
                return;
            if(timeDifferenceVisualization)
                TimeDifferenceVisualisation();
            else if (_timeDifferenceVisualisationActive)
                ResetTimeDifferenceVisualisation();
            

            if(visibilityVisualization)
                VisibilityVisualisation();
            else if (_visibilityVisualisationActive)
                ResetVisibilityVisualisation();
            
            if(audioPerceptionVisualization)
                AudioPerceptionVisualisation();
            else if (_audioPerceptionVisualisationActive)
                ResetAudioPerceptionVisualisation();
            
        }
        
        private void ResetTimeDifferenceVisualisation()
        {
            if (!photonView.IsMine)
                return;
            foreach (var kv in _networkController._userReplayTimes)
            {
                if (kv.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (PhotonNetwork.CurrentRoom.Players.ContainsKey(kv.Key))
                    {
                        string userName = PhotonNetwork.CurrentRoom.Players[kv.Key].NickName;
                        if(!_userGameobject.ContainsKey(kv.Key))
                            _userGameobject.Add(kv.Key, Utils.GetGameObjectBySubstring(userName + " [External User]"));
                        GameObject user = _userGameobject[kv.Key];

                        GameObject nameTag = Utils.GetChildBySubstring(user, "NameTag");
                        TextMeshProUGUI t = null;
                        if (nameTag != null)
                        {
                            GameObject text = Utils.GetChildBySubstring(nameTag, "Text");
                            if(text != null)
                                 t = text.GetComponent<TextMeshProUGUI>();
                        }
                        
                        Utils.MakeAllChildrenNonTransparent(user);

                        if (t != null && _networkController._userNames.ContainsKey(kv.Key))
                            t.text = _networkController._userNames[kv.Key];
                    }
                }
            }
            
            _timeDifferenceVisualisationActive = false;
        }

        private void TimeDifferenceVisualisation()
        {
            if (!photonView.IsMine)
                return;

            foreach (var kv in _networkController._userReplayTimes)
            {
                if (kv.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (PhotonNetwork.CurrentRoom.Players.ContainsKey(kv.Key))
                    {
                        float userTime = _networkController._userReplayTimes[kv.Key] + (Time.time - _networkController._userReplayTimesUpdateTime[kv.Key]);
                        string userName = PhotonNetwork.CurrentRoom.Players[kv.Key].NickName;

                        GameObject user = null;
                        if (!_userGameobject.ContainsKey(kv.Key))
                        {
                            user = Utils.GetGameObjectBySubstring(userName + " [External User]");
                            if (user != null)
                                _userGameobject[kv.Key] = user;
                        }
                        else
                            user = _userGameobject[kv.Key];

                        float deltaT = Mathf.Abs(_state.currentReplayTime - userTime);
                        if (_state.currentPreviewTime > 0.0f)
                            deltaT = Mathf.Abs(_state.currentPreviewTime - userTime);
                        
                        //GameObject nameTag = Utils.GetChildBySubstring(user, "NameTag");
                        //TextMeshProUGUI t = null;
                        //if (nameTag != null)
                        //{
                        //    GameObject text = Utils.GetChildBySubstring(nameTag, "Text");
                        //    if(text != null)
                        //         t = text.GetComponent<TextMeshProUGUI>();
                        //}

                        // make other user transparent depending on the current time difference
                        if (deltaT >= 0.05f || true)
                        {
                            // simple sigmoid derivative multiplied to be within [0,1] for all x
                            float x = 0.7f * deltaT;
                            float sigmoid = 1.0f / (1.0f + Mathf.Exp(-x));
                            float sigmoidDer = sigmoid * (1.0f - sigmoid);
                            float alpha = 3.0f * sigmoidDer + 0.25f;

                            if (!_userAlpha.ContainsKey(kv.Key))
                                _userAlpha[kv.Key] = alpha;

                            if (Mathf.Abs(_userAlpha[kv.Key] - alpha) >= 0.1f)
                            {
                                Utils.MakeAllChildrenTransparent(user, alpha);
                                _userAlpha[kv.Key] = alpha;
                            }
                            //Debug.Log("User: " + userName + " currently at a different time in the replay");
                        }

                        // display time of the user if time difference is greater than a certain threshold
                        if (Mathf.Abs(_state.currentReplayTime - userTime) >= 1.0f)
                        {
                            //if (t != null)
                            //{
                            //    if (!_networkController._userNames.ContainsKey(kv.Key))
                            //        _networkController._userNames[kv.Key] = t.text;
                            //    t.text = _networkController._userNames[kv.Key] + "\n" + userTime.ToString("F1");
                            //}
                        }
                        else
                        {
                            Utils.MakeAllChildrenNonTransparent(user);

                            //if (t != null && _networkController._userNames.ContainsKey(kv.Key))
                            //    t.text = _networkController._userNames[kv.Key];
                        }
                    }
                }
            }

            _timeDifferenceVisualisationActive = true;
        }

        private void ResetVisibilityVisualisation()
        {
            if (!photonView.IsMine)
                return;
            foreach (var kv in _networkController._userVisibility)
            {
                if (kv.Key == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    foreach (var userKV in PhotonNetwork.CurrentRoom.Players)
                    {
                        if(userKV.Key == PhotonNetwork.LocalPlayer.ActorNumber)
                            continue;
                        
                        string userName = userKV.Value.NickName;
                       
                        if(!_userGameobject.ContainsKey(kv.Key))
                            _userGameobject.Add(kv.Key, Utils.GetGameObjectBySubstring(userName + " [External User]"));
                        GameObject user = _userGameobject[kv.Key];
                        GameObject head = Utils.GetChildByName(user, "HMDMesh");

                        Outline outline = head.GetComponent<Outline>();
                        if(outline != null)
                            Destroy(outline);
                    }
                    break;
                }
            }
            
            _visibilityVisualisationActive = false;
        }
        
        private void VisibilityVisualisation()
        {
            if (!photonView.IsMine)
                return;
            
            if (Time.time - _lastVisibilityVisualisationUpdate >= 0.1f)
            {
                //Debug.LogError("Visibility  Visualisation");
                foreach (var kv in _networkController._userVisibility)
                {
                    if (kv.Key == PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        foreach (var userKV in PhotonNetwork.CurrentRoom.Players)
                        {
                            if (userKV.Key == PhotonNetwork.LocalPlayer.ActorNumber)
                                continue;

                            string userName = userKV.Value.NickName;

                            if (!_userGameobject.ContainsKey(userKV.Key))
                                _userGameobject.Add(userKV.Key,
                                    Utils.GetGameObjectBySubstring(userName + " [External User]"));
                            GameObject user = _userGameobject[userKV.Key];
                            GameObject head = Utils.GetChildByName(user, "HMDMesh");

                            Renderer r = head.GetComponent<Renderer>();
                            Material material = r.material;
                            Color color = material.color;

                            if (kv.Value.Contains(userKV.Key))
                            {
                                Outline outline = head.GetComponent<Outline>();
                                Destroy(outline);

                                float g = 0.0f;
                                float b = 0.0f;
                                //material.color = new Color(color.r, g, b, color.a);
                                //Debug.LogError("User is visible for user: " + userName);
                            }
                            else
                            {
                                Outline outline = head.GetComponent<Outline>();
                                if (outline == null)
                                    outline = head.AddComponent<Outline>();
                                outline.OutlineColor = Color.red;

                                //material.color = new Color(color.r, color.g, color.b, color.a);
                                //Debug.LogError("User is not visible for user: " + userName);
                            }
                        }

                        break;
                    }
                }

                _lastVisibilityVisualisationUpdate = Time.time;
            }

            _visibilityVisualisationActive = true;
        }

        private void ResetAudioPerceptionVisualisation()
        {
            if (!photonView.IsMine)
                return;
            foreach (var kv in _networkController._userAudioLevel)
            {
                if (kv.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (PhotonNetwork.CurrentRoom.Players.ContainsKey(kv.Key))
                    {
                        string userName = PhotonNetwork.CurrentRoom.Players[kv.Key].NickName;
                        if(!_userGameobject.ContainsKey(kv.Key))
                            _userGameobject.Add(kv.Key, Utils.GetGameObjectBySubstring(userName + " [External User]"));
                        GameObject user = _userGameobject[kv.Key];
                        GameObject head = Utils.GetChildByName(user, "HeadMesh");

                        Renderer r = head.GetComponent<Renderer>();
                        Material material = r.material;
                        material.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                    }
                }
            }
            _audioPerceptionVisualisationActive = false;
        }
        
        private void AudioPerceptionVisualisation()
        {
            if (!photonView.IsMine)
                return;
            
            foreach (var kv in _networkController._userAudioLevel)
            {
                if (kv.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (PhotonNetwork.CurrentRoom.Players.ContainsKey(kv.Key))
                    {
                        float userAudioLevel = _networkController._userAudioLevel[kv.Key] * 50.0f;

                        float deltaT = 0.0f;
                        if (_networkController._userReplayTimes.ContainsKey(kv.Key))
                        {
                            float userTime = _networkController._userReplayTimes[kv.Key];
                            deltaT = Mathf.Abs(_state.currentReplayTime - userTime);
                        }

                        float timeDiffMax = 10.0f;
                        if (deltaT >= timeDiffMax)
                            deltaT = timeDiffMax;

                        userAudioLevel *= deltaT / timeDiffMax;
                        
                        if (PhotonNetwork.CurrentRoom.Players.ContainsKey(kv.Key))
                        {
                            string userName = PhotonNetwork.CurrentRoom.Players[kv.Key].NickName;
                            if(!_userGameobject.ContainsKey(kv.Key))
                                _userGameobject.Add(kv.Key, Utils.GetGameObjectBySubstring(userName + " [External User]"));
                            GameObject user = _userGameobject[kv.Key];
                            GameObject head = Utils.GetChildByName(user, "HeadMesh");
                            

                            Renderer r = head.GetComponent<Renderer>();
                            if (r != null)
                            {
                                Material material = r.material;
                                Color color = material.color;
                                float g = Mathf.Max(0.0f, 1.0f - userAudioLevel);
                                float b = Mathf.Max(0.0f, 1.0f - userAudioLevel);
                                material.color = new Color(color.r, g, b, color.a);
                            }
                        }
                    }
                }
            }

            _audioPerceptionVisualisationActive = true;
        }
    }
}