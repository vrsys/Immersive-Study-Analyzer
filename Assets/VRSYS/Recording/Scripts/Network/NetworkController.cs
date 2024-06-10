using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(PhotonView))]
    public class NetworkController : MonoBehaviourPun, IOnEventCallback
    {
        [DllImport("RecordingPlugin")]
        private static extern bool RegisterRecordingStartGlobalTimeOffset(int recorderId, float globalTimeOffset);

        public float maxSynchronizationTimeMS;
        [HideInInspector] public Transcripts text;

        [HideInInspector] public Dictionary<int, float> _userReplayTimes = new Dictionary<int, float>();
        [HideInInspector] public Dictionary<int, float> _userReplayTimesUpdateTime = new Dictionary<int, float>();
        [HideInInspector] public Dictionary<int, float> _userAudioLevel = new Dictionary<int, float>();
        [HideInInspector] public Dictionary<int, List<int>> _userVisibility = new Dictionary<int, List<int>>();
        [HideInInspector] public Dictionary<int, float> _userPreviewTimes = new Dictionary<int, float>();
        [HideInInspector] public Dictionary<int, Color> _userColors = new Dictionary<int, Color>();
        [HideInInspector] public Dictionary<int, string> _userNames = new Dictionary<int, string>();
        [HideInInspector] public Dictionary<int, bool> _userDownloadStatus = new Dictionary<int, bool>();
        [HideInInspector] public float currentLatency;

        private RecorderState _state;
        private RecorderController _controller;

        private bool _replayStarted = false;
        private bool _transformsDownloaded = false;
        private bool _soundsDownloaded = false;
        private bool _metaInformationDownloaded = false;
        private bool _arbDownloaded = false;
        private bool _transformsDownloadFailed = false;
        private bool _soundsDownloadFailed = false;
        private bool _metaInformationDownloadFailed = false;
        private bool _arbDownloadFailed = false;
        private bool _allUsersFinishedLoading = false;
        private bool _startReplayEventSent = false;
        
        private DateTime _globalSynchronizationTime;
        private TimeSpan _globalRecordStartDifference;
        private float _internalSynchronizationTime;
        private float _currentPhotonPing;

        private int _selectedServerId = 0;
        private TextMeshProUGUI _serverText;
        
        // -----------------------------------------------------------------------------------------------------------------
        // Start Photon Network Code
        // -----------------------------------------------------------------------------------------------------------------
        private const byte StartRecordingOnAllClientsEventCode = 1;
        private const byte EndRecordingOnAllClientsEventCode = 2;
        private const byte StartReplayOnAllClientsEventCode = 3;
        private const byte EndReplayOnAllClientsEventCode = 4;
        private const byte StartDownloadOnAllClientsEventCode = 5;
        private const byte RoundTripAllClientsEventCode = 6;
        private const byte UpdateUserReplayTimeEventCode = 7;
        private const byte UpdateDownloadStatusEventCode = 8;
        private const byte UpdateUserPreviewTimeEventCode = 9;
        private const byte TogglePlayPauseReplayEventCode = 10;
        private const byte PrepareRecordingOnAllClientsEventCode = 11;
        private const byte UpdateUserAudioLevelEventCode = 12;
        private const byte UpdateUserVisibilityEventCode = 13;
        private const byte SwitchSelectedServerEventCode = 14;
        
        public void Start()
        {
            _state = GetComponent<RecorderState>();
            _controller = GetComponent<RecorderController>();
        }

        public void OnEvent(EventData photonEvent)
        {
            if (!photonView.IsMine)
                return;
            
            switch (photonEvent.Code)
            {
                case PrepareRecordingOnAllClientsEventCode:
                    PrepareRecordingOnClient(photonEvent);
                    break;
                case StartRecordingOnAllClientsEventCode:
                    StartCoroutine(StartRecordingOnClient(photonEvent));
                    break;
                case EndRecordingOnAllClientsEventCode:
                    StartCoroutine(EndRecordingOnClient(photonEvent));
                    break;
                case StartReplayOnAllClientsEventCode:
                    StartCoroutine(StartReplayOnClient(photonEvent));
                    break;
                case EndReplayOnAllClientsEventCode:
                    StartCoroutine(EndReplayOnClient(photonEvent));
                    break;
                case StartDownloadOnAllClientsEventCode:
                    StartDownloads(photonEvent);
                    break;
                case RoundTripAllClientsEventCode:
                    RoundTrip(photonEvent);
                    break;
                case UpdateUserReplayTimeEventCode:
                    UpdateUserReplayTime(photonEvent);
                    break;
                case UpdateUserPreviewTimeEventCode:
                    UpdateUserPreviewTime(photonEvent);
                    break;
                case UpdateUserAudioLevelEventCode:
                    UpdateUserAudioLevel(photonEvent);
                    break;
                case UpdateUserVisibilityEventCode:
                    UpdateUserVisibility(photonEvent);
                    break;
                case UpdateDownloadStatusEventCode:
                    UpdateDownloadStatus(photonEvent);
                    break;
                case TogglePlayPauseReplayEventCode:
                    TogglePlayPauseReplay(photonEvent);
                    break;
                case SwitchSelectedServerEventCode:
                    SwitchSelectedServer(photonEvent);
                    break;
            }
        }
        
        public void SwitchSelectedServerEvent()
        {
            if (!photonView.IsMine)
                return;
            
            int newSelectedServerId = (_selectedServerId + 1) % _state.serverList.Count;
            
            object[] serverData = new object[] { newSelectedServerId, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(SwitchSelectedServerEventCode, serverData, raiseEventOptions, SendOptions.SendReliable);
        }
        
        private void SwitchSelectedServer(EventData photonEvent)
        {
            if(!photonView.IsMine)
                return;
            
            object[] data = (object[])photonEvent.CustomData;
            
            _selectedServerId = (int)data[0];
            int recorderId = (int)data[1];
            if(_state.recorderID != recorderId)
                return;
            
            Debug.Log("Switch selected server event received. Recorder id: " + _state.recorderID);
            
            _state.selectedServer = _state.serverList[_selectedServerId];
            _state.replayList = new ReplayList();
            if(_serverText != null)
                _serverText.text = "Server: " + _state.selectedServer;
            
            UpdateReplayList();
        }
        
        public void PrepareRecordingOnAllClientsEvent()
        {
            if (!photonView.IsMine)
                return;
            object[] data = new object[] { _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(PrepareRecordingOnAllClientsEventCode, data, raiseEventOptions, SendOptions.SendReliable);
        }

        private void PrepareRecordingOnClient(EventData photonEvent)
        {
            if(!photonView.IsMine)
                return;
            
            object[] data = (object[])photonEvent.CustomData;
            int recorderId = (int)data[0];
            if(_state.recorderID != recorderId)
                return;
            
            Debug.Log("Preparing for recording. Recorder id: " + _state.recorderID);
            
            _controller.PrepareRecording();
        }
        
        public void StartRecordingOnAllClientsEvent()
        {
            if (!photonView.IsMine)
                return;
            
            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            Debug.Log("Global time before offset: " + _globalSynchronizationTime);
            DateTime startRecordingTime = _globalSynchronizationTime.AddMilliseconds(maxSynchronizationTimeMS);
            Debug.Log("Start recording time: " + _globalSynchronizationTime);
            object[] recordingData = new object[] { startRecordingTime.ToFileTime(), _state.recorderID};
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(StartRecordingOnAllClientsEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }
        
        private IEnumerator StartRecordingOnClient(EventData photonEvent)
        {
            if (!photonView.IsMine)
                yield return null;
            
            //Debug.Log("Start recording event code received at time " + Time.time);
            object[] data = (object[])photonEvent.CustomData;
            DateTime startRecordingTime = DateTime.FromFileTime((long)data[0]);
            int recorderId = (int)data[1];

            if (_state.recorderID != recorderId)
                yield return null;
            
            Debug.Log("Received start recording time: " + startRecordingTime + ". Recorder id: " + _state.recorderID);

            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            if (_globalSynchronizationTime > startRecordingTime)
            {
                TimeSpan difference = _globalSynchronizationTime - startRecordingTime;
                Debug.LogError("The recording should have started already! Time difference: " + difference.TotalMilliseconds + " ms.  Potential fix: increase the  maxSynchronizationTime!");
                if (_state.currentState == State.PrepareRecording)
                {
                    _controller.StartRecording();
                }
            }
            else
            {
                while (_globalSynchronizationTime < startRecordingTime)
                {
                    TimeSpan difference = _globalSynchronizationTime - startRecordingTime;
                    _globalRecordStartDifference = difference;
                    Debug.Log("Total difference to global start recording time: " + difference.TotalMilliseconds);
                    yield return new WaitForFixedUpdate();
                    _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
                }

                if (_state.currentState == State.PrepareRecording)
                {
                    TimeSpan diff = _globalSynchronizationTime - startRecordingTime;
                    RegisterRecordingStartGlobalTimeOffset(_state.recorderID, (float)diff.TotalMilliseconds);
                    _controller.StartRecording();
                }
                else
                {
                    Debug.LogWarning("A request to start a recording was sent but the current state does not allow starting new recording.");
                }
            }
        }

        public void EndRecordingOnAllClientsEvent()
        {
            if (!photonView.IsMine)
                return;
            
            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            DateTime endRecordingTime = _globalSynchronizationTime.AddMilliseconds(maxSynchronizationTimeMS);
            object[] recordingData = new object[] { endRecordingTime.ToFileTime(), _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(EndRecordingOnAllClientsEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }

        private IEnumerator EndRecordingOnClient(EventData photonEvent)
        {
            if (!photonView.IsMine)
                yield return null;
            
            //Debug.Log("End recording event code received at time " + Time.time);
            object[] data = (object[])photonEvent.CustomData;
            DateTime stopRecordingTime = DateTime.FromFileTime((long)data[0]);
            int recorderId = (int)data[1];

            if (recorderId != _state.recorderID)
                yield return null;
            
            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            if (_globalSynchronizationTime > stopRecordingTime)
            {
                TimeSpan difference = _globalSynchronizationTime - stopRecordingTime;
                Debug.LogError("The recording should have stopped already! Time difference: " +
                               difference.TotalMilliseconds +
                               " ms.  Potential fix: increase the  maxSynchronizationTime!");
            }
            else
            {
                while (_globalSynchronizationTime < stopRecordingTime)
                {
                    TimeSpan difference = _globalSynchronizationTime - stopRecordingTime;
                    Debug.Log("Total difference to global stop recording time: " + difference.TotalMilliseconds);
                    yield return new WaitForFixedUpdate();
                    _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
                }
            }

            _controller.EndRecording();
            
            _allUsersFinishedLoading = false;
        }
        
        public void StartReplayOnAllClientsEvent()
        {
            if (!photonView.IsMine)
                return;
            Debug.Log("Sending event to start replay on all clients.");
            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            DateTime startReplayTime = _globalSynchronizationTime.AddMilliseconds(maxSynchronizationTimeMS);
            object[] recordingData = new object[] { startReplayTime.ToFileTime(), _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(StartReplayOnAllClientsEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
            _startReplayEventSent = true;
        }

        private IEnumerator StartReplayOnClient(EventData photonEvent)
        {
            if (!photonView.IsMine)
                yield return null;
            
            object[] data = (object[])photonEvent.CustomData;
            DateTime startReplayTime = DateTime.FromFileTime((long)data[0]);
            int recorderId = (int)data[1];

            if (recorderId != _state.recorderID)
                yield return null;
            
            Debug.Log("Start replay event received for recorder id: " + _state.recorderID);
            
            if (!_replayStarted)
                Debug.Log("Replay not yet started.");

            if (!IsDownloading())
                Debug.Log("Not downloading files.");

            if (!_replayStarted && !IsDownloading() && _state.currentState == State.PreparingReplay)
            {
                _replayStarted = true;

                _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
                if (_globalSynchronizationTime > startReplayTime)
                {
                    TimeSpan difference = _globalSynchronizationTime - startReplayTime;
                    Debug.LogError("The replay should have started already! Time difference: " +
                                   difference.TotalMilliseconds +
                                   " ms.  Potential fix: increase the  maxSynchronizationTime!");
                }
                else
                {
                    while (_globalSynchronizationTime < startReplayTime)
                    {
                        TimeSpan difference = _globalSynchronizationTime - startReplayTime;
                        Debug.Log("Total difference to global start replay time: " + difference.TotalMilliseconds);
                        yield return new WaitForFixedUpdate();
                        _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
                    }
                }

                _controller.StartReplay();
            }
        }

        public void EndReplayOnAllClientsEvent()
        {
            if (!photonView.IsMine)
                return;
            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            DateTime stopReplayTime = _globalSynchronizationTime.AddMilliseconds(maxSynchronizationTimeMS);
            object[] recordingData = new object[] { stopReplayTime.ToFileTime(), _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(EndReplayOnAllClientsEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }

        private IEnumerator EndReplayOnClient(EventData photonEvent)
        {
            if (!photonView.IsMine)
                yield return null;
            
            //Debug.Log("End replay event code received at time " + Time.time);
            object[] data = (object[])photonEvent.CustomData;
            DateTime stopReplayTime = DateTime.FromFileTime((long)data[0]);
            int recorderId = (int)data[1];

            if (recorderId != _state.recorderID)
                yield return null;
            
            _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
            if (_globalSynchronizationTime > stopReplayTime)
            {
                TimeSpan difference = _globalSynchronizationTime - stopReplayTime;
                Debug.LogError("The replay should have stopped already! Time difference: " +
                               difference.TotalMilliseconds +
                               " ms.  Potential fix: increase the  maxSynchronizationTime!");
            }
            else
            {
                while (_globalSynchronizationTime < stopReplayTime)
                {
                    TimeSpan difference = _globalSynchronizationTime - stopReplayTime;
                    Debug.Log("Total difference to global stop replay time: " + difference.TotalMilliseconds);
                    yield return new WaitForFixedUpdate();
                    _globalSynchronizationTime = NetworkUtils.SynchronizeViaNTP();
                }
            }
            
            _controller.EndReplay();
        }

        public void StartDownloadOnAllClientsEvent()
        {
            if (!photonView.IsMine)
                return;
            if (_state.selectedReplayFile == "")
            {
                Debug.LogError("No replay file selected!");
                return;
            }

            Debug.Log("Started download on all clients");
            
            object[] recordingData = new object[] { _state.selectedReplayFile, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(StartDownloadOnAllClientsEventCode, recordingData, raiseEventOptions, SendOptions.SendReliable);
        }

        private void StartDownloads(EventData photonEvent)
        {
            if(!photonView.IsMine)
                return;
            
            if (_state.currentState == State.Idle)
            {
             
                object[] data = (object[])photonEvent.CustomData;
                _state.selectedReplayFile = (string)data[0];
                int recorderId = (int)data[1];
                
                if(recorderId != _state.recorderID)
                    return;
                
                Debug.Log("Download started for recorder id: " + _state.recorderID);
                Debug.Log("Selected replay file: " + _state.selectedReplayFile);

                _soundsDownloaded = false;
                _transformsDownloaded = false;
                _metaInformationDownloaded = false;
                _arbDownloaded = false;
                _replayStarted = false;
                _allUsersFinishedLoading = false;
                _startReplayEventSent = false;

                if (_state.recordingDirectory == "" || true)
                {
                    _state.recordingDirectory = Application.persistentDataPath;
                }

                StartDownloadCoroutines();
                _userDownloadStatus.Clear();

                _state.currentState = State.PreparingReplay;
                
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    _userDownloadStatus.Add(player.ActorNumber, false);
                }
            }
        }
        
        public void StartDownloads()
        {
            if (!photonView.IsMine)
                return;
            
            if (_state.currentState == State.PreparingReplay)
            {
                Debug.Log("Local download started");
                Debug.Log("Selected replay file: " + _state.selectedReplayFile);

                _soundsDownloaded = false;
                _transformsDownloaded = false;
                _metaInformationDownloaded = false;
                _arbDownloaded = false;
                _replayStarted = false;
                _allUsersFinishedLoading = false;
                _startReplayEventSent = false;

                if (_state.recordingDirectory == "")
                {
                    _state.recordingDirectory = Application.persistentDataPath;
                }

                StartDownloadCoroutines();         }
        }
        
        public void UpdateDownloadStatusEvent()
        {
            if (!photonView.IsMine)
                return;
            bool downloadState = IsDownloading();
            object[] recordingData = new object[] { downloadState, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(UpdateDownloadStatusEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }

        private void UpdateDownloadStatus(EventData photonEvent)
        {
            if(!photonView.IsMine)
                return;
            
            int sender = photonEvent.Sender;
            object[] data = (object[])photonEvent.CustomData;
            bool downloadStatus = (bool)data[0];
            int recorderId = (int)data[1];

            if(recorderId != _state.recorderID)
                return;
            
            if (!_allUsersFinishedLoading)
            {
                //Debug.Log("Player: " + sender + ", Download state: " + downloadStatus);
                
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Keys.ToList())
                {
                    if (player == sender)
                    {
                        if (_userDownloadStatus.ContainsKey(player))
                        {
                            _userDownloadStatus[player] = downloadStatus;
                        }
                        else
                        {
                            _userDownloadStatus.Add(player, downloadStatus);
                        }
                    }
                }

                foreach (var player in _userDownloadStatus.Keys.ToList())
                {
                    if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(player))
                    {
                        _userDownloadStatus.Remove(player);
                    }
                }
                
                _allUsersFinishedLoading = true;
                foreach (var player in PhotonNetwork.CurrentRoom.Players.Keys.ToList())
                {
                    if (!_userDownloadStatus.ContainsKey(player))
                    {
                        //Debug.Log("User: " + player.ActorNumber + " not finished downloading.");
                        _userDownloadStatus[player] = false;
                        _allUsersFinishedLoading = false;
                    }
                    else if (_userDownloadStatus[player])
                    {
                        //Debug.Log("User: " + player.ActorNumber + " not finished downloading.");
                        _allUsersFinishedLoading = false;
                    }
                }
                
                _allUsersFinishedLoading = _allUsersFinishedLoading && !IsDownloading();

                //if (_allUsersFinishedLoading)
                //    Debug.Log("All user finished downloading the recording file.");

                //if (!_replayStarted)
                //    Debug.Log("Replay has not started yet.");

                //if (!IsDownloading())
                //    Debug.Log("Not downloading anymore.");
                //else 
                //    Debug.Log("Still downloading.");
            }

            if (_allUsersFinishedLoading && !_replayStarted && !_startReplayEventSent)
            {
                StartReplayOnAllClientsEvent();
            }
        }

        public void TogglePlayPauseReplayOnAllClients(bool paused)
        {
            if (!photonView.IsMine)
                return;
            object[] recordingData = new object[] { paused, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(TogglePlayPauseReplayEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }

        private void TogglePlayPauseReplay(EventData photonEvent)
        {
            if(!photonView.IsMine)
                return;
            
            object[] data = (object[])photonEvent.CustomData;
            bool isPaused = (bool)data[0];
            int recorderId = (int)data[1];
            
            if(recorderId != _state.recorderID)
                return;
            
            _state.replayPaused = isPaused;
        }

        private IEnumerator RoundTripEvent()
        {
            if (!photonView.IsMine)
                yield return true;
            float d = Time.time;
            DateTime t = _globalSynchronizationTime.AddSeconds(d);

            object[] eventData = new object[] { t.ToFileTime() };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(RoundTripAllClientsEventCode, eventData, raiseEventOptions,
                SendOptions.SendReliable);
            yield return true;
        }

        private void RoundTrip(EventData photonEvent)
        {
            int sender = photonEvent.Sender;
            Player sendPlayer = PhotonNetwork.CurrentRoom.GetPlayer(sender);
            if (PhotonNetwork.LocalPlayer.ActorNumber == sendPlayer.ActorNumber)
            {
                UpdateLatencyInformation((object[])photonEvent.CustomData);
            }
        }

        public void UpdateUserReplayTimeOnAllClientsEvent(float currentUserTime)
        {
            if (!photonView.IsMine)
                return;
            object[] recordingData = new object[] { currentUserTime, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(UpdateUserReplayTimeEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }

        private void UpdateUserReplayTime(EventData photonEvent)
        {
            if (!photonView.IsMine)
                return;
            
            int sender = photonEvent.Sender;
            Player sendPlayer = PhotonNetwork.CurrentRoom.GetPlayer(sender);
            object[] data = (object[])photonEvent.CustomData;
            float userReplayTime = (float)data[0];
            int recorderId = (int)data[1];
            
            if(recorderId != _state.recorderID)
                return;
            
            if (!_userReplayTimes.ContainsKey(sendPlayer.ActorNumber))
            {
                _userReplayTimes.Add(sendPlayer.ActorNumber, userReplayTime);
                _userReplayTimesUpdateTime.Add(sendPlayer.ActorNumber, Time.time);
            }
            else
            {
                _userReplayTimes[sendPlayer.ActorNumber] = userReplayTime;
                _userReplayTimesUpdateTime[sendPlayer.ActorNumber] = Time.time;
            }

            foreach (var key in _userReplayTimes.Keys.ToList())
            {
                if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(key))
                {
                    _userReplayTimes.Remove(key);
                    _userReplayTimesUpdateTime.Remove(key);
                }
            }
        }
        
        public void UpdateUserPreviewTimeOnAllClientsEvent(float currentUserPreviewTime)
        {
            if (!photonView.IsMine)
                return;
            object[] recordingData = new object[] { currentUserPreviewTime, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(UpdateUserPreviewTimeEventCode, recordingData, raiseEventOptions,
                SendOptions.SendReliable);
        }

        private void UpdateUserPreviewTime(EventData photonEvent)
        {
            if (!photonView.IsMine)
                return;
            
            int sender = photonEvent.Sender;
            Player sendPlayer = PhotonNetwork.CurrentRoom.GetPlayer(sender);
            object[] data = (object[])photonEvent.CustomData;
            float userPreviewTime = (float)data[0];
            int recorderId = (int)data[1];
            
            if(recorderId != _state.recorderID)
                return;
            
            if (!_userPreviewTimes.ContainsKey(sendPlayer.ActorNumber))
            {
                _userPreviewTimes.Add(sendPlayer.ActorNumber, userPreviewTime);
            }
            else
            {
                _userPreviewTimes[sendPlayer.ActorNumber] = userPreviewTime;
            }
            
            foreach (var key in _userPreviewTimes.Keys.ToList())
            {
                if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(key))
                {
                    _userPreviewTimes.Remove(key);
                }
            }
        }

        public void UpdateUserVisibilityOnAllClientsEvent(int targetUser, bool visible)
        {
            if (!photonView.IsMine)
                return;
            object[] recordingData = new object[] { targetUser, visible, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(UpdateUserVisibilityEventCode, recordingData, raiseEventOptions, SendOptions.SendReliable);
        }

        private void UpdateUserVisibility(EventData photonEvent)
        {
            if (!photonView.IsMine)
                return;
            
            //Debug.LogError("Visibility information updated.");
            int sender = photonEvent.Sender;
            Player sendPlayer = PhotonNetwork.CurrentRoom.GetPlayer(sender);
            object[] data = (object[])photonEvent.CustomData;
            int targetUser = (int)data[0];
            bool visible = (bool)data[1];
            int recorderId = (int)data[2];
            
            if(recorderId != _state.recorderID)
                return;
            
            if (visible)
            {
                if (!_userVisibility.ContainsKey(targetUser))
                {
                    List<int> list = new List<int>();
                    _userVisibility.Add(targetUser, list);
                }
                
                if(!_userVisibility[targetUser].Contains(sendPlayer.ActorNumber))
                    _userVisibility[targetUser].Add(sendPlayer.ActorNumber);
            }
            else
            {
                if(_userVisibility.ContainsKey(targetUser) && _userVisibility[targetUser] != null && _userVisibility[targetUser].Contains(sendPlayer.ActorNumber))
                    _userVisibility[targetUser].RemoveAll(item => item == sendPlayer.ActorNumber);
            }
            
            foreach (var key in _userVisibility.Keys.ToList())
            {
                if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(key))
                {
                    _userVisibility.Remove(key);
                }
            }
        }
        
        public void UpdateUserAudioLevelOnAllClientsEvent(float currentAudioLevel)
        {
            if (!photonView.IsMine)
                return;
            
            object[] recordingData = new object[] { currentAudioLevel, _state.recorderID };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(UpdateUserAudioLevelEventCode, recordingData, raiseEventOptions, SendOptions.SendReliable);
        }

        private void UpdateUserAudioLevel(EventData photonEvent)
        {
            if (!photonView.IsMine)
                return;
            
            int sender = photonEvent.Sender;
            Player sendPlayer = PhotonNetwork.CurrentRoom.GetPlayer(sender);
            object[] data = (object[])photonEvent.CustomData;
            float userAudioLevel = (float)data[0];
            int recorderId = (int)data[1];
            
            if(recorderId != _state.recorderID)
                return;
            
            if (!_userAudioLevel.ContainsKey(sendPlayer.ActorNumber))
            {
                _userAudioLevel.Add(sendPlayer.ActorNumber, userAudioLevel);
            }
            else
            {
                _userAudioLevel[sendPlayer.ActorNumber] = userAudioLevel;
            }
            
            foreach (var key in _userAudioLevel.Keys.ToList())
            {
                if (!PhotonNetwork.CurrentRoom.Players.ContainsKey(key))
                {
                    _userAudioLevel.Remove(key);
                }
            }
        }
        
        private void UpdateLatencyInformation(object[] data)
        {
            DateTime t = DateTime.FromFileTime((long)data[0]);

            float dO = Time.time;
            DateTime tO = _globalSynchronizationTime.AddSeconds(dO);

            TimeSpan d = tO - t;
            currentLatency = d.Milliseconds;
            GameObject.Find("Latency").GetComponent<Text>().text = "Round Trip Latency: " + currentLatency + "ms";

            _currentPhotonPing = PhotonNetwork.GetPing();
            GameObject.Find("PLatency").GetComponent<Text>().text =
                "Photon Round Trip Latency: " + _currentPhotonPing + "ms";
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        // -----------------------------------------------------------------------------------------------------------------
        // End Photon Network Code
        // -----------------------------------------------------------------------------------------------------------------

        public void Upload(string filePath, string fileName, string serverAddress)
        {
            if (!photonView.IsMine)
                return;
            StartCoroutine(NetworkUtils.UploadToServer(filePath, fileName, serverAddress));
        }

        public void UpdateReplayList()
        {
            if (!photonView.IsMine)
                return;
            if (_state.selectedServer.Length == 0 && _state.serverList.Count > 0)
            {
                _selectedServerId = 0;
                _state.selectedServer = _state.serverList[_selectedServerId];
            }

            if (!_state.serverList.Contains(_state.selectedServer))
            {
                _selectedServerId = 0;
                _state.selectedServer = _state.serverList[_selectedServerId];
            }

            if (!_state.selectedServer.Contains("http"))
                _state.selectedServer = "http://" + _state.selectedServer;

            StartCoroutine(GetReplayList(_state.selectedServer));
        }

        public void UpdateSelectedServerText(TextMeshProUGUI text)
        {
            if (photonView == null || !photonView.IsMine)
                return;

            if(text == null)
                return;

            if (_serverText == null)
                _serverText = text;

            _serverText.text = "Server: " + _state.selectedServer;
        }

        private IEnumerator GetReplayList(string serverAddress)
        {
            string completeURL = serverAddress + "/all_recording_names";

            using (var uwr = new UnityWebRequest(completeURL, UnityWebRequest.kHttpVerbGET))
            {
                DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
                uwr.downloadHandler = dH;

                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                    Debug.LogError(uwr.error);
                else
                {
                    string response = uwr.downloadHandler.text;
                    ReplayList newReplayList = JsonUtility.FromJson<ReplayList>(response);
          
                    if (_state.replayList == null || _state.replayList.replayNames == null || newReplayList.replayNames.Length != _state.replayList.replayNames.Length)
                        _state.replayList = newReplayList;
                }
            }
        }

        public bool IsDownloading()
        {
            if (!_soundsDownloaded || !_transformsDownloaded || !_metaInformationDownloaded || !_arbDownloaded)
            {

                if (_transformsDownloadFailed)
                {
                    _transformsDownloadFailed = false;
                    StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_transform_recording", _state.selectedReplayFile));
                }

                if (_soundsDownloadFailed)
                {
                    _soundsDownloadFailed = false;
                    StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_sound_recording", _state.selectedReplayFile));
                }

                if (_metaInformationDownloadFailed)
                {
                    _metaInformationDownloadFailed = false;
                    StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_meta_recording", _state.selectedReplayFile));
                }

                if (_arbDownloadFailed)
                {
                    _arbDownloadFailed = false;
                    StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_arb_recording", _state.selectedReplayFile));
                }

                return true;
            }
            
            //Text downloadStatus = Utils.GetChildByName(gameObject,"DownloadStatus").GetComponent<Text>();
            //downloadStatus.text = "Waiting";

            return false;
        }
        
        private IEnumerator DownloadFileFromServer(string directory, string url, string fileName)
        {
            string completeURL = _state.selectedServer + "/" + url + "/" + fileName;

            string fileType = ".txt";
            if (url.Contains("get_meta_recording"))
            {
                fileType = ".recordmeta";
            }
            else if (url.Contains("get_sound_recording"))
            {
                fileType = "_sound.txt";
            }
            else if (url.Contains("get_arb_recording"))
            {
                fileType = "_arb.txt";
            } 
            else if (url.Contains("get_transform_recording"))
            {
               
            }
            
            using (var uwr = new UnityWebRequest(completeURL, UnityWebRequest.kHttpVerbGET))
            {
                string file = directory + "/" + fileName + fileType;
                if(!File.Exists(file)){
                    DownloadHandlerFile dH = new DownloadHandlerFile(file);
                    dH.removeFileOnAbort = true;
                    uwr.downloadHandler = dH;
                    uwr.timeout = 0;
                    //uwr.useHttpContinue = false;
                    //uwr.SetRequestHeader("Accept-Encoding", "gzip, deflate, sdch");
                    //uwr.SetRequestHeader("Connection","Keep-Alive");
                    //uwr.SetRequestHeader("Keep-Alive","timeout=15, max=1000");
                    //uwr.SetRequestHeader("Cache-Control", "no-cache");
                    
                    uwr.SendWebRequest();
                    
                    while (!uwr.isDone)
                    {
                        //Text downloadStatus = Utils.GetChildByName(gameObject,"DownloadStatus").GetComponent<Text>();
                        //downloadStatus.text = (uwr.downloadProgress * 100.0f).ToString("F0") + "%";

                        yield return null;
                    }

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError(uwr.error);
                        
                        if (url.Contains("get_meta_recording"))
                        {
                            _metaInformationDownloadFailed = true;
                        }
                        else if (url.Contains("get_sound_recording"))
                        {
                            _soundsDownloadFailed = true;
                        }
                        else if (url.Contains("get_arb_recording"))
                        {
                            _arbDownloadFailed = true;
                        }
                        else if (url.Contains("get_transform_recording"))
                        {
                            _transformsDownloadFailed = true;
                        }
                    }
                    else
                    {
                        if (url.Contains("get_meta_recording"))
                        {
                            _metaInformationDownloaded = true;
                        }
                        else if (url.Contains("get_sound_recording"))
                        {
                            _soundsDownloaded = true;
                        }
                        else if (url.Contains("get_arb_recording"))
                        {
                            _arbDownloaded = true;
                        }
                        else if (url.Contains("get_transform_recording"))
                        {
                            _transformsDownloaded = true;
                        }
                    }
                }
                else
                {
                    if (url.Contains("get_meta_recording"))
                    {
                        _metaInformationDownloaded = true;
                    }
                    else if (url.Contains("get_sound_recording"))
                    {
                        _soundsDownloaded = true;
                    }
                    else if (url.Contains("get_arb_recording"))
                    {
                        _arbDownloaded = true;
                    }
                    else if (url.Contains("get_transform_recording"))
                    {
                        _transformsDownloaded = true;
                    }
                }
            }
        }


        private IEnumerator GetTextFromSound(string fileName)
        {
            yield return null;
            
            string completeURL = _state.selectedServer + "/speech_2_text?filename=" + fileName;

            /*
            using (var uwr = new UnityWebRequest(completeURL, UnityWebRequest.kHttpVerbGET))
            {
                DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
                uwr.downloadHandler = dH;

                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning(uwr.error + " for text from sound!");
                else
                {
                    string response = uwr.downloadHandler.text;
                    text = JsonUtility.FromJson<Transcripts>(response);
                }
            }
            */
        }

        public void StartRoundTripTest()
        {
            if (!photonView.IsMine)
                return;
            if (PhotonNetwork.InRoom)
                StartCoroutine(RoundTripEvent());
        }
        
        private void StartDownloadCoroutines()
        {
            if (!photonView.IsMine)
                return;
            string replayFile = _state.selectedReplayFile;
            string replayFilePartner = "";
            
            if (replayFile.Contains("partner_0"))
                replayFilePartner = replayFile.Replace("partner_0", "partner_1");
            else if (replayFile.Contains("partner_1"))
                replayFilePartner = replayFile.Replace("partner_1", "partner_0");
            
            StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_transform_recording", replayFile));
            StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_sound_recording", replayFile));
            StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_meta_recording", replayFile));
            StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_arb_recording", replayFile));
            StartCoroutine(GetTextFromSound(replayFile + "_" + "0" + ".wav"));

            if (replayFilePartner.Length > 0)
            {
                StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_meta_recording", replayFilePartner));
                StartCoroutine(DownloadFileFromServer(_state.recordingDirectory, "get_sound_recording", replayFilePartner));
                _state.selectedPartnerReplayFile = replayFilePartner;
            }
        }
    }
}