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