using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vrsys;
using VRSYS.Recording.Scripts;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(RecorderState))]
    [RequireComponent(typeof(PluginConfigurator))]
    [RequireComponent(typeof(NetworkController))]
    public class RecorderController : MonoBehaviourPun
    {
        [DllImport("RecordingPlugin")]
        private static extern bool StopRecording(int recorderId);

        [DllImport("RecordingPlugin")]
        private static extern bool StopReplay(int recorderId);

        [DllImport("RecordingPlugin")]
        private static extern bool OpenExistingRecordingFile(int recorderId, string recordingDir, int recordingDirNameLength, string recordingName, int recordingNameLength);
        
        [DllImport("RecordingPlugin")]
        private static extern float GetRecordingDuration(int recorderId);

        [DllImport("RecordingPlugin")]
        private static extern bool CreateNewRecordingFile(int recorderId, string recordingDir, int recordingDirNameLength, string recordingName, int recordingNameLength);
        
        [DllImport("RecordingPlugin")]
        private static extern bool CreateCSVFile(int recorderId, string recordingName, int recordingNameLength);

        [DllImport("RecordingPlugin")]
        private static extern bool CreateWAVFile(int recorderId, string recordingName, int recordingNameLength);

        public SubtitleVisualizer subtitleVisualizer;

        public GameObject desktopExternalUserNotNetworked;
        public GameObject desktopUserNotNetworked;
        public GameObject hmdExternalUserNotNetworked;
        public GameObject hmdUserNotNetworked;

        public int transformRecordingStepsPerSecond = 20;
        public int audioRecordingStepsPerSecond = 10;

        public bool attachTransformRecorderToAll = true;
        public bool replayAudio = false;

        public bool createWAV = false;
        public bool createCSV = false;
        public bool synchronizedPlayback = false;

        private ScenePreparator _scenePreparator;
        private FollowRecordedUserController _followRecordedUserController;
        private RecorderState _state;
        private PluginConfigurator _configurator;
        private NetworkController _networkController;

        private float _lastTransformRecordTime;
        private Dictionary<int, Recorder> _transformRecorder = new Dictionary<int, Recorder>();
        private Dictionary<int, Recorder> _audioRecorder = new Dictionary<int, Recorder>();
        private Dictionary<int, Recorder> _genericRecorder = new Dictionary<int, Recorder>();
        private Dictionary<int, Recorder> _portalTransformRecorder = new Dictionary<int, Recorder>();
        private Dictionary<int, Recorder> _portalAudioRecorder = new Dictionary<int, Recorder>();
        private Dictionary<int, Recorder> _portalGenericRecorder = new Dictionary<int, Recorder>();

        private List<float> playbackTimes = new List<float>();
        private List<float> playbackUserTimes = new List<float>();
        private List<float> portalTimes = new List<float>();
        private float lastPlaybackTimeWrite;
        private string playbackStartDate;
        private float playbackStartTime;
        
        private float _lastReplayListRefresh;
        private bool localPlayback = false;

        [HideInInspector]
        public int RecorderID
        {
            get
            {
                if (_state != null)
                    return _state.recorderID;

                return -1;
            }
        }

        [HideInInspector]
        public string RecordingDirectory
        {
            get
            {
                if (_state != null)
                    return _state.recordingDirectory;

                return "-1";
            }
        }
        
        [HideInInspector]
        public string PartnerReplayFile
        {
            get
            {
                if (_state != null)
                    return _state.selectedPartnerReplayFile;

                return "";
            }
        }
        
        [HideInInspector]
        public Dictionary<string, bool> RecordedObjectPresent
        {
            get { return _state.recordedObjectPresent; }
        }

        [HideInInspector]
        public GameObject LocalUserHead
        {
            get { return _state.localUserHead; }
        }

        public GameObject LocalRecordedUserHead;

        public GameObject ExternalRecordedUserHead;

        [HideInInspector]
        public State CurrentState
        {
            get { return _state.currentState; }
        }

        private TooltipHandler tooltipHandler;
        private Tooltip pauseTooltip;
        private Tooltip temporalNavigationTooltip;
        
        public void Start()
        {
            _state = GetComponent<RecorderState>();
            if (!photonView.IsMine)
                return;
            _configurator = GetComponent<PluginConfigurator>();
            _networkController = GetComponent<NetworkController>();
            
            if (_state.recordingDirectory == "")
                _state.recordingDirectory = Application.persistentDataPath + "/";

            _scenePreparator = new ScenePreparator(this, desktopExternalUserNotNetworked, desktopUserNotNetworked,
                hmdExternalUserNotNetworked, hmdUserNotNetworked);
            
            tooltipHandler = NetworkUser.localGameObject.GetComponent<TooltipHandler>();
            
            pauseTooltip = new Tooltip();
            pauseTooltip.hand = TooltipHand.Left;
            pauseTooltip.tooltipName = "Play/Pause";
            pauseTooltip.tooltipText = "Play/Pause";
            pauseTooltip.actionButtonReference = Tooltip.ActionButton.SecondaryButton;
            tooltipHandler.AddTooltip(pauseTooltip);
            //tooltipHandler.HideTooltip(pauseTooltip);
       
            temporalNavigationTooltip = new Tooltip();
            temporalNavigationTooltip.hand = TooltipHand.Left;
            temporalNavigationTooltip.tooltipName = "Temporal navigation";
            temporalNavigationTooltip.tooltipText = "Temporal navigation";
            temporalNavigationTooltip.actionButtonReference = Tooltip.ActionButton.Thumbstick;
            tooltipHandler.AddTooltip(temporalNavigationTooltip);
            tooltipHandler.HideTooltip(temporalNavigationTooltip);
        }

        private void AttachGenericRecorder()
        {
            //LatencyRecorder latencyRecorder = gameObject.AddComponent<LatencyRecorder>();
            //latencyRecorder.SetId(1);
            //latencyRecorder.controller = this;
        }

        private void AttachSoundRecorder()
        {
            if (replayAudio)
            {
                Photon.Voice.Unity.Recorder rec = GameObject.Find("Voice Manager")
                    .GetComponent<Photon.Voice.Unity.Recorder>();
                if (rec != null)
                {
                    MicWrapper wrapper = (MicWrapper)rec.inputSource;
                    if (wrapper != null)
                    {
                        AudioClip microphoneClip = wrapper.mic;
                        string microphone = wrapper.device;
                        MicrophoneClipReader reader = new MicrophoneClipReader(microphoneClip, microphone);
                        MicrophoneRecorder microphoneRecorder = gameObject.AddComponent<MicrophoneRecorder>();
                        microphoneRecorder.SetId(0);
                        microphoneRecorder.Controller = this;
                        microphoneRecorder.SetMicrophoneReader(reader);
                    }
                }

                GameObject listener = FindObjectOfType<AudioListener>().gameObject;
                AudioListenerRecorder audioListenerRecorder = listener.AddComponent<AudioListenerRecorder>();
                audioListenerRecorder.SetId(1);
                audioListenerRecorder.Controller = this;
            }
        }

        private void AttachTransformRecorder()
        {
            if (attachTransformRecorderToAll)
            {
                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                foreach (var rootObject in rootObjects)
                {
                    bool isRecordingSetup = rootObject.name.Contains("__RECORDING__");
                    bool isNetworkSetup = rootObject.name.Contains("__NETWORKING__");
                    bool isEnvironment = rootObject.name.Contains("__ENVIRONMENT__");
                    if (!isRecordingSetup && !isNetworkSetup && !isEnvironment)
                        AttachTransformRecorderRecursively(rootObject);
                }

                if (DontDestroySceneAccessor.Instance != null)
                {
                    rootObjects = DontDestroySceneAccessor.Instance.GetAllRootsOfDontDestroyOnLoad();

                    foreach (var rootObject in rootObjects)
                    {
                        bool isRecordingSetup = rootObject.name.Contains("__RECORDING__");
                        bool isNetworkSetup = rootObject.name.Contains("__NETWORKING__");
                        bool isEnvironment = rootObject.name.Contains("__ENVIRONMENT__");
                        if (!isRecordingSetup && !isNetworkSetup && !isEnvironment)
                            AttachTransformRecorderRecursively(rootObject);
                    }
                }
            }
        }

        private void AttachTransformRecorderRecursively(GameObject root)
        {
            if (_state.currentState == State.PreparingReplay)
                if ((root.name.Contains("[Local User]") || root.name.Contains("[External User]")) &&
                    !root.name.Contains("[Rec]"))
                    return;

            foreach (Transform childTransform in root.transform)
            {
                AttachTransformRecorderRecursively(childTransform.gameObject);
            }

            TransformRecorder[] transformRecorders = root.GetComponents<TransformRecorder>();
            bool found = false;

            foreach (var transformRecorder in transformRecorders)
                if (transformRecorder.controller == this)
                    found = true;

            if (!found)
            {
                TransformRecorder recorder = root.AddComponent<TransformRecorder>();
                recorder.controller = this;
                recorder.SetId(root.GetInstanceID());
                recorder.RegisterRecorder();
            }
        }

        private void CleanDestroy()
        {
            if (!photonView.IsMine)
                return;
            if (_state.currentState == State.Recording || _state.currentState == State.PrepareRecording)
            {
                EndRecording();
            }

            if (_state.currentState == State.Replaying || _state.currentState == State.PreviewReplay ||
                _state.currentState == State.PreparingReplay)
            {
                bool result = StopReplay(_state.recorderID);
                if (!result)
                    Debug.LogError("Could not stop the replay!");
            }

            DestroyPreviewObjects();
        }

        private void OnDestroy()
        {
            if (!photonView.IsMine)
                return;
            
            if (playbackTimes.Count > 0)
            {
                string[] lines = new string[playbackTimes.Count];
                for (int i = 0; i < playbackTimes.Count; ++i)
                    lines[i] = playbackUserTimes[i].ToString("F2") + ";" + playbackTimes[i].ToString("F2") + ";" + portalTimes[i].ToString("F2");
                // Write the lines into the file
                File.AppendAllLines(Application.persistentDataPath + "/playbackTimes_" + playbackStartDate + "_.csv", lines);
            }
            
            CleanDestroy();
        }

        private void OnDisable()
        {
            //throw new NotImplementedException();
        }

        public void OnApplicationQuit()
        {
            if (!photonView.IsMine)
                return;
            CleanDestroy();
        }

        public void PrepareAndStartDistributedReplay()
        {
            if (!photonView.IsMine)
                return;
            if (_state.currentState == State.Idle)
            {
                _networkController.StartDownloadOnAllClientsEvent();
            }
        }

        public void PrepareLocalReplay()
        {
            if (!photonView.IsMine)
                return;
            Debug.Log("Preparing local replay.");
            _state.currentState = State.PreparingReplay;
            localPlayback = true;
            _networkController.StartDownloads();
        }

        public void StartReplay()
        {
            if (!photonView.IsMine)
                return;

            Debug.Log("Starting replay for recorder with id: " + _state.recorderID);

            bool result = OpenExistingRecordingFile(_state.recorderID, _state.recordingDirectory, _state.recordingDirectory.Length, _state.selectedReplayFile, _state.selectedReplayFile.Length);

            if (!result)
            {
                Debug.LogError("Playback file existence check: Failed " + Time.time);
                return;
            }

            Debug.Log("Playback file existence check: Successful " + Time.time);

            _state.recordingDuration = GetRecordingDuration(_state.recorderID);
            _state.currentMinSliderValue = 0.0f;
            _state.currentMaxSliderValue = _state.recordingDuration;

            _scenePreparator.PrepareReplayScene();

            AttachTransformRecorder();
            AttachSoundRecorder();
            AttachGenericRecorder();

            playbackStartDate = DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("es-ES")).Replace(" ", "_")
                .Replace(":", "_").Replace("/", "_");

            playbackStartTime = Time.time;
            if (subtitleVisualizer != null)
                subtitleVisualizer.Activate();

            _state.selectableUsers = _scenePreparator.GetSelectableUserPerspectives();
            _state.recordedObjectPresent = _scenePreparator.GetNamePresent();
            _followRecordedUserController = new FollowRecordedUserController(_state);

            DisableAllAudioSources();

            _state.currentState = State.Replaying;
            _state.currentReplayTime = 0.0f;
            _state.currentPreviewTime = -1.0f;
            _state.currentRecordingTime = -1.0f;
            _state.recordingStartTime = -1.0f;
            
            tooltipHandler.ShowTooltip(pauseTooltip);
            tooltipHandler.ShowTooltip(temporalNavigationTooltip);
            tooltipHandler.playback = true;
        }

        public void SendEndReplayEvent()
        {
            if (!photonView.IsMine)
                return;
            _networkController.EndReplayOnAllClientsEvent();
        }

        public void EndReplay()
        {
            if (!photonView.IsMine)
                return;

            _state.currentState = State.Idle;
            _state.currentReplayTime = -1.0f;
            _state.currentPreviewTime = -1.0f;
            _state.currentRecordingTime = -1.0f;
            _state.recordingStartTime = -1.0f;

            tooltipHandler.HideTooltip(pauseTooltip);
            tooltipHandler.HideTooltip(temporalNavigationTooltip);
            tooltipHandler.playback = false;
            
            if (subtitleVisualizer != null)
                subtitleVisualizer.Deactivate();

            Debug.Log("Stopping replay for recorder with id: " + _state.recorderID);


            bool result = StopReplay(_state.recorderID);

            if (result)
                Debug.Log("Playback stopped successful! " + Time.time);
            else
                Debug.LogError("Playback not stopped successful! " + Time.time);

            _state.recordingDuration = -1.0f;
            _state.currentMinSliderValue = -1.0f;
            _state.currentMaxSliderValue = -1.0f;

            DestroyRecorder();
            EnableAllAudioSources();

            foreach (var kv in _state.selectableUsers)
            {
                if (kv.Value != null && kv.Value.name.Contains("[Rec]"))
                    Destroy(kv.Value);
            }
        }

        public void SendStartRecordingEvents()
        {
            if (!photonView.IsMine)
                return;
            _networkController.PrepareRecordingOnAllClientsEvent();

            _networkController.StartRecordingOnAllClientsEvent();
        }

        public void StartRecording()
        {
            if (!photonView.IsMine)
                return;
            _state.currentState = State.Recording;
            _state.currentReplayTime = -1.0f;
            _state.currentPreviewTime = -1.0f;
            _state.currentRecordingTime = 0.0f;
            _state.recordingStartTime = Time.time;

            Debug.Log("Starting recording for recorder with id: " + _state.recorderID);
        }

        public void PrepareRecording()
        {
            if (!photonView.IsMine)
                return;

            Debug.Log("Preparing recording for recorder with id: " + _state.recorderID);
            _state.currentState = State.PrepareRecording;

            AttachTransformRecorder();
            AttachSoundRecorder();
            AttachGenericRecorder();


            bool result = CreateNewRecordingFile(_state.recorderID, _state.recordingDirectory, _state.recordingDirectory.Length, _state.recordingFile, _state.recordingFile.Length);
            
            if (!result)
                Debug.LogError("Recording file creation: Failed for recorder with id: " + _state.recorderID);
            else
                Debug.Log("Recording file creation: Successful for recorder with id: " + _state.recorderID);
        }

        public void SendEndRecordingEvent()
        {
            if (!photonView.IsMine)
                return;
            _networkController.EndRecordingOnAllClientsEvent();
        }

        public void EndRecording()
        {
            if (!photonView.IsMine)
                return;

            _state.currentState = State.Idle;

            Debug.Log("Stopping recording for recorder with id: " + _state.recorderID);

            bool result = StopRecording(_state.recorderID);

            if (!result)
                Debug.LogError("Recording stopped: Failed");
            else
                Debug.Log("Recording stopped: Successful");

            string transformFile = RecordingDirectory + "/" + _state.recordingFile + ".txt";
            string soundFile = RecordingDirectory + "/" + _state.recordingFile + "_sound.txt";
            string arbFile = RecordingDirectory + "/" + _state.recordingFile + "_arb.txt";
            string metaFile = RecordingDirectory + "/" + _state.recordingFile + ".recordmeta";
            string date = DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("es-ES")).Replace(" ", "_")
                .Replace(":", "_").Replace("/", "_");

            Debug.Log("Trying to transmit transform file: " + transformFile);
            if (System.IO.File.Exists(transformFile))
            {
                _networkController.Upload(transformFile, _state.recordingFile + "_" + date + ".txt",
                    _state.selectedServer);
            }

            Debug.Log("Trying to transmit sound file: " + soundFile);
            if (System.IO.File.Exists(soundFile))
            {
                _networkController.Upload(soundFile, _state.recordingFile + "_" + date + "_sound.txt",
                    _state.selectedServer);
            }

            Debug.Log("Trying to transmit meta file: " + metaFile);
            if (System.IO.File.Exists(metaFile))
            {
                _networkController.Upload(metaFile, _state.recordingFile + "_" + date + ".recordmeta",
                    _state.selectedServer);
            }

            Debug.Log("Trying to transmit generic file: " + arbFile);
            if (System.IO.File.Exists(arbFile))
            {
                _networkController.Upload(arbFile, _state.recordingFile + "_" + date + "_arb.txt",
                    _state.selectedServer);
            }

            if (createWAV)
                Invoke(nameof(WAVCreationCoroutine), 5.0f);

            if (createCSV)
                Invoke(nameof(CSVCreationCoroutine), 5.0f);

            Debug.Log("Finished transmitting all files.");

            _networkController.UpdateReplayList();

            DestroyRecorder();
        }

        public void WAVCreationCoroutine()
        {
            StartCoroutine(CreateAndUploadWAVFiles());
        }

        public void CSVCreationCoroutine()
        {
            StartCoroutine(CreateCSVFile());
        }

        public void RegisterRecorder(int id, Recorder recorder)
        {
            if (!photonView.IsMine)
                return;

            if (recorder.portal)
            {
                if (recorder is TransformRecorder && !_portalTransformRecorder.ContainsKey(id))
                    _portalTransformRecorder.Add(id, recorder);
                if (recorder is AudioRecorder && !_portalAudioRecorder.ContainsKey(id))
                    _portalAudioRecorder.Add(id, recorder);
                if (recorder is GenericRecorder && !_portalGenericRecorder.ContainsKey(id))
                    _portalGenericRecorder.Add(id, recorder);
            }
            else
            {
                if (recorder is TransformRecorder && !_transformRecorder.ContainsKey(id))
                    _transformRecorder.Add(id, recorder);
                if (recorder is AudioRecorder && !_audioRecorder.ContainsKey(id))
                    _audioRecorder.Add(id, recorder);
                if (recorder is GenericRecorder && !_genericRecorder.ContainsKey(id))
                    _genericRecorder.Add(id, recorder);
            }
        }

        public void DeregisterRecorder(int id, Recorder recorder)
        {
            if (!photonView.IsMine)
                return;

            if (recorder.portal)
            {
                if (recorder is TransformRecorder && _portalTransformRecorder.ContainsKey(id))
                    _portalTransformRecorder.Remove(id);
                if (recorder is AudioRecorder && _portalAudioRecorder.ContainsKey(id))
                    _portalAudioRecorder.Remove(id);
                if (recorder is GenericRecorder && _portalGenericRecorder.ContainsKey(id))
                    _portalGenericRecorder.Remove(id);
            }
            else
            {
                if (recorder is TransformRecorder && _transformRecorder.ContainsKey(id))
                    _transformRecorder.Remove(id);
                if (recorder is AudioRecorder && _audioRecorder.ContainsKey(id))
                    _audioRecorder.Remove(id);
                if (recorder is GenericRecorder && _genericRecorder.ContainsKey(id))
                    _genericRecorder.Remove(id);
            }
        }

        public Dictionary<int, Recorder> GetAudioRecorder()
        {
            return _audioRecorder;
        }

        public Recorder GetTransformRecorder(int id)
        {
            if (_transformRecorder.ContainsKey(id))
                return _transformRecorder[id];
            return null;
        }

        private void DestroyRecorder()
        {
            foreach (var kv in _transformRecorder)
                if (kv.Value != null)
                    Destroy(kv.Value);

            foreach (var kv in _audioRecorder)
                if (kv.Value != null)
                    Destroy(kv.Value);

            foreach (var kv in _portalTransformRecorder)
                if (kv.Value != null)
                    Destroy(kv.Value);

            foreach (var kv in _portalAudioRecorder)
                if (kv.Value != null)
                    Destroy(kv.Value);

            //foreach (var kv in _genericRecorder)
            //    if (kv.Value != null)
            //       Destroy(kv.Value);

            _transformRecorder.Clear();
            _audioRecorder.Clear();
            //_genericRecorder.Clear();
        }

        public void FixedUpdate()
        {
            if (!photonView.IsMine)
                return;
            if (_state.currentState == State.PreparingReplay && !localPlayback)
                _networkController.UpdateDownloadStatusEvent();
        }

        private void SetFixedPlaybackRecordingFileIfSet()
        {
            if (_state.fixedPlaybackRecordingName.Length > 0 &&
                _state.selectedReplayFile != _state.fixedPlaybackRecordingName)
            {
                _state.selectedReplayFile = _state.fixedPlaybackRecordingName;
                _state.selectedReplayFileUpdated = true;
            }
        }

        private void LateJoinLocalPlaybackStart()
        {
            // make sure playback is started for late joining users
            if (_state.currentState == State.Idle && _networkController._userReplayTimes.Count > 0)
            {
                Photon.Voice.Unity.Recorder rec = GameObject.Find("Voice Manager")
                    .GetComponent<Photon.Voice.Unity.Recorder>();
                if (rec.IsCurrentlyTransmitting)
                {
                    if (_networkController._userReplayTimes.Count == 1 &&
                        _networkController._userReplayTimes.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
                    {
                    }
                    else if (!_networkController._userReplayTimes.ContainsKey(PhotonNetwork.LocalPlayer.ActorNumber))
                    {
                        PrepareLocalReplay();
                    }
                }
            }

            if (_state.currentState == State.PreparingReplay && localPlayback && !_networkController.IsDownloading())
                StartReplay();
        }

        private void SynchronizePlayback()
        {
            // make sure all users are close in time to each other in the playback
            // Note: this means that individual temporal navigation is not possible
            if (_state.currentState == State.Replaying && _networkController._userReplayTimes.Count > 0 &&
                synchronizedPlayback)
            {
                Debug.Log("Checking for synchronized time");
                List<int> playerTimesList = _networkController._userReplayTimes.Keys.ToList();
                playerTimesList.Sort();

                foreach (var key in playerTimesList)
                {
                    //Debug.Log("Checking for user with key: " + key);
                    if (key != PhotonNetwork.LocalPlayer.ActorNumber &&
                        PhotonNetwork.CurrentRoom.Players.ContainsKey(key))
                    {
                        float time = _networkController._userReplayTimes[key];
                        //Debug.Log("User key: " + key + ", user time: " + time);
                        if (Mathf.Abs(time - _state.currentReplayTime) > 2.0f &&
                            key < PhotonNetwork.LocalPlayer.ActorNumber)
                        {
                            float timeAdvance = 5.0f;
                            int it = 3;
                            int i = 0;
                            while (Mathf.Abs(time - _state.currentReplayTime) > timeAdvance & i < it)
                            {
                                i++;
                                Debug.Log("Current playback time: " + _state.currentReplayTime + ", target time: " +
                                          time);

                                if (_state.currentReplayTime < 1.0f)
                                    _state.currentReplayTime = 1.0f;

                                foreach (var kv in _transformRecorder)
                                {
                                    if (kv.Value != null)
                                    {
                                        bool playback = kv.Value.Replay(_state.currentReplayTime);

                                        Debug.Log("Playback state: " + playback);

                                        if (_state.currentReplayTime + timeAdvance < _state.recordingDuration &&
                                            _state.currentReplayTime + timeAdvance < time)
                                        {
                                            _state.currentReplayTime += timeAdvance;
                                        }
                                        else if (_state.currentReplayTime + timeAdvance > _state.recordingDuration &&
                                                 time < _state.currentReplayTime)
                                        {
                                            _state.currentReplayTime = 0.0f;
                                        }

                                        break;
                                    }
                                }

                                foreach (var kv in _audioRecorder)
                                {
                                    if (kv.Value != null && !kv.Value.Replay(_state.currentReplayTime))
                                    {
                                        break;
                                    }
                                }

                                foreach (var kv in _genericRecorder)
                                {
                                    if (kv.Value != null && !kv.Value.Replay(_state.currentReplayTime))
                                    {
                                        break;
                                    }
                                }
                            }

                            Debug.Log("Adjusting playback time from: " + _state.currentReplayTime + " to: " + time);
                            _state.currentReplayTime = time;
                            break;
                        }
                    }
                }
            }
        }

        public void Update()
        {
            if (!photonView.IsMine)
                return;

            SetFixedPlaybackRecordingFileIfSet();

            LateJoinLocalPlaybackStart();

            SynchronizePlayback();
        }

        public void LateUpdate()
        {
            if (!photonView.IsMine)
                return;

            if (_state.currentState == State.Idle)
                Idle();

            if (_state.currentState == State.Recording)
                Recording();

            if (_state.currentState == State.Replaying && _state.currentReplayTime < _state.recordingDuration)
                Replay();

            if (_state.currentState == State.PreviewReplay && _state.currentPreviewTime > 0.0f && _state.currentPreviewTime <= _state.recordingDuration)
                Preview();
        }

        private void Idle()
        {
            if (Time.time - _lastReplayListRefresh >= 2.0f)
            {
                _networkController.UpdateReplayList();
                _lastReplayListRefresh = Time.time;
            }
        }

        private void Recording()
        {
            if (Time.time - _lastTransformRecordTime > 1.0f / (float)transformRecordingStepsPerSecond)
            {
                foreach (var kv in _transformRecorder)
                {
                    if (!kv.Value.Record(_state.currentRecordingTime))
                    {
                        EndRecording();
                        break;
                    }
                }

                foreach (var kv in _audioRecorder)
                {
                    if (!kv.Value.Record(_state.currentRecordingTime))
                    {
                        EndRecording();
                        break;
                    }
                }

                foreach (var kv in _genericRecorder)
                {
                    if (!kv.Value.Record(_state.currentRecordingTime))
                    {
                        EndRecording();
                        break;
                    }
                }

                _lastTransformRecordTime = Time.time;
            }

            _state.currentRecordingTime += Time.deltaTime;
        }

        private void Replay()
        {
            DestroyPreviewObjects();

            if (subtitleVisualizer != null && !subtitleVisualizer.TranscriptsSet())
                subtitleVisualizer.SetTranscripts(_networkController.text);
            
            foreach (var kv in _transformRecorder)
            {
                if (kv.Value != null && !kv.Value.Replay(_state.currentReplayTime))
                {
                    //Debug.Log("Could not replay transform for object: " + kv.Value.gameObject.name);
                    //EndReplay();
                    //break;
                }
            }


            foreach (var kv in _portalTransformRecorder)
            {
                if (kv.Value != null && _state.currentPortalTime >= 0.0f &&
                    !kv.Value.Replay(_state.currentPortalTime))
                {
                    //Debug.Log("Could not replay transform for object: " + kv.Value.gameObject.name);
                    //EndReplay();
                    //break;
                }
            }

            foreach (var kv in _audioRecorder)
            {
                if (kv.Value != null && !kv.Value.Replay(_state.currentReplayTime))
                {
                    //EndReplay();
                    //break;
                }
            }

            foreach (var kv in _portalAudioRecorder)
            {
                if (kv.Value != null && _state.currentPortalTime >= 0.0f &&
                    !kv.Value.Replay(_state.currentPortalTime))
                {
                    //EndReplay();
                    //break;
                }
            }

            foreach (var kv in _genericRecorder)
            {
                if (kv.Value != null && !kv.Value.Replay(_state.currentReplayTime))
                {
                    //EndReplay();
                    //break;
                }
            }

            foreach (var kv in _portalGenericRecorder)
            {
                if (kv.Value != null && _state.currentPortalTime >= 0.0f &&
                    !kv.Value.Replay(_state.currentPortalTime))
                {
                    //EndReplay();
                    //break;
                }
            }

            //if (subtitleVisualizer != null)
            //    subtitleVisualizer.SubtitleGeneration(_state.currentReplayTime);

            //if (_followRecordedUserController != null)
            //    _followRecordedUserController.SetReplayUserView();

            //_networkController.UpdateUserReplayTimeOnAllClientsEvent(_state.currentReplayTime);
            //_networkController.UpdateUserPreviewTimeOnAllClientsEvent(_state.currentPreviewTime);

            if (_state.currentReplayTime > _state.recordingDuration)
                EndReplay();
        }

        private void Preview()
        {
            CreatePreviewObjects();

            foreach (var kv in _transformRecorder)
            {
                if (kv.Value != null && !kv.Value.Preview(_state.currentPreviewTime))
                {
                    Debug.Log("Could not preview transform for object: " + kv.Value.gameObject.name);
                    //EndReplay();
                    //break;
                }
            }

            foreach (var kv in _audioRecorder)
            {
                if (kv.Value != null && !kv.Value.Preview(_state.currentPreviewTime))
                {
                    Debug.Log("Could not preview audio for object: " + kv.Value.gameObject.name);
                    //EndReplay();
                    //break;
                }
            }

            if (_followRecordedUserController != null)
                _followRecordedUserController.SetReplayUserView();

            _networkController.UpdateUserReplayTimeOnAllClientsEvent(_state.currentReplayTime);
            _networkController.UpdateUserPreviewTimeOnAllClientsEvent(_state.currentPreviewTime);
        }

        private void DisableAllAudioSources()
        {
            if (!photonView.IsMine)
                return;
            AudioSource[] audioSources = (AudioSource[])FindObjectsOfType(typeof(AudioSource));

            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource.gameObject.name != "UserSpeaker" &&
                    !audioSource.gameObject.name.Contains("Speaker for Player"))
                {
                    audioSource.Pause();
                }
            }
        }

        private void EnableAllAudioSources()
        {
            if (!photonView.IsMine)
                return;
            AudioSource[] audioSources = (AudioSource[])FindObjectsOfType(typeof(AudioSource));

            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.UnPause();
            }
        }

        private void CreatePreviewObjects()
        {
            if (!photonView.IsMine)
                return;
            float alpha = 0.3f;

            if (_state.previewNode.transform.childCount == 0)
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
                        if (!isRecordingSetup && !isNetworkSetup && !isLocalUser && !isExternalUser)
                        {
                            GameObject preview = Instantiate(rootObjects[i]);
                            preview.transform.parent = _state.previewNode.transform;
                            preview.name = preview.name.Replace("(Clone)", "");
                            Utils.DestroyNetworkAndCameraComponentsRecursive(preview);
                            Utils.MakeAllChildrenTransparent(rootObjects[i], alpha);
                            Utils.MarkPreviewRecorder(preview);
                            Utils.DisableLight(rootObjects[i]);

                            GameObject nameCanvas = Utils.GetChildBySubstring(rootObjects[i], "UserInfoCanvas");
                            if (nameCanvas != null)
                                nameCanvas.GetComponent<Canvas>().enabled = false;
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
                        if (!isRecordingSetup && !isNetworkSetup && !isLocalUser && !isExternalUser)
                        {
                            GameObject preview = Instantiate(rootObjects[i]);
                            preview.transform.parent = _state.previewNode.transform;
                            preview.name = preview.name.Replace("(Clone)", "");
                            Utils.DestroyNetworkAndCameraComponentsRecursive(preview);
                            Utils.MakeAllChildrenTransparent(rootObjects[i], alpha);
                            Utils.MarkPreviewRecorder(preview);
                            Utils.DisableLight(rootObjects[i]);

                            GameObject nameCanvas = Utils.GetChildBySubstring(rootObjects[i], "UserInfoCanvas");
                            if (nameCanvas != null)
                                nameCanvas.GetComponent<Canvas>().enabled = false;
                        }
                    }
                }
            }
        }

        private void DestroyPreviewObjects()
        {
            if (!photonView.IsMine)
                return;
            if (_state.previewNode != null && _state.previewNode.transform.childCount > 0)
            {
                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

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
                        if (!isRecordingSetup && !isNetworkSetup && !isLocalUser && !isExternalUser)
                        {
                            Utils.MakeAllChildrenNonTransparent(rootObjects[i]);
                            Utils.EnableLight(rootObjects[i]);

                            GameObject nameCanvas = Utils.GetChildBySubstring(rootObjects[i], "UserInfoCanvas");
                            if (nameCanvas != null)
                                nameCanvas.GetComponent<Canvas>().enabled = true;
                        }
                    }
                }

                Utils.DestroyChildren(_state.previewNode);
            }
        }

        IEnumerator CreateAndUploadWAVFiles()
        {
            bool finished = CreateWAVFile(_state.recorderID, _state.recordingFile, _state.recordingFile.Length);

            if (finished)
            {
                string date = DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("es-ES")).Replace(" ", "_")
                    .Replace(":", "_").Replace("/", "_");

                for (int i = 0; i < 2; i++)
                {
                    string audioFile = _configurator.recordingDirectory + "/" + _state.recordingFile + "_" + i + ".wav";
                    Debug.Log("Trying to transmit audio file: " + audioFile);
                    if (System.IO.File.Exists(audioFile))
                    {
                        _networkController.Upload(audioFile, _state.recordingFile + "_" + date + "_" + i + ".wav",
                            _state.selectedServer);
                    }
                }
            }
            else
            {
                Debug.LogError("Could not create WAV files!");
            }

            yield return null;
        }

        IEnumerator CreateCSVFile()
        {
            bool finished = CreateCSVFile(_state.recorderID, _state.recordingFile, _state.recordingFile.Length);

            if (!finished)
                Debug.LogError("Could not create CSV file!");
            yield return null;
        }

        public void AddOriginalIdGameobject(int originalId, int newId, GameObject go)
        {
            if (!photonView.IsMine)
                return;
            if (!_state.originalIdGameObjects.ContainsKey(originalId))
            {
                _state.originalIdGameObjects.Add(originalId, go);
                if (!_state.newIdOriginalId.ContainsKey(newId))
                    _state.newIdOriginalId.Add(newId, originalId);
            }
        }

        public void SetSelectedRecordingFileText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            bool found = false;
            for (int i = 0; i < _state.replayList.replayNames.Length; ++i)
            {
                if (_state.replayList.replayNames[i] == _state.selectedReplayFile)
                {
                    found = true;
                    break;
                }
            }

            if (!found && _state.replayList.replayNames.Length > 0)
            {
                _state.selectedReplayFile = _state.replayList.replayNames[0];
            }


            text.text = "Selected file: " + _state.selectedReplayFile;
        }

        public float GetRecordingDuration()
        {
            return _state.recordingDuration;
        }

        public void SetRadialSliderMax(UnityEngine.UI.Extensions.RadialSlider slider)
        {
            Debug.Log("Set radial slider max!");
            if (!photonView.IsMine)
                return;
            slider.maxValue = _state.recordingDuration;
        }

        public void SetRadialSliderMin(UnityEngine.UI.Extensions.RadialSlider slider)
        {
            if (!photonView.IsMine)
                return;
            slider.minValue = 0.0f;
        }
    }
}