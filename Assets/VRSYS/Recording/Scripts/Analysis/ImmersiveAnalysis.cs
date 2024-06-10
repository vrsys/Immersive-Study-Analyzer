using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Vrsys;
using VRSYS.Recording.Scripts.Analysis;
using Vrsys.Scripts.Recording;
using VRSYS.Scripts.Recording;
using State = VRSYS.Scripts.Recording.State;

namespace VRSYS.Recording.Scripts
{
    [RequireComponent(typeof(RecorderState))]
    [RequireComponent(typeof(ImmersiveAnnotation))]
    public class ImmersiveAnalysis : MonoBehaviourPun
    {
        [DllImport("RecordingPlugin")]
        private static extern void ClearAnalysisRequests(int analysisId);

        [DllImport("RecordingPlugin")]
        private static extern void ClearAnalysisFilePaths(int analysisId);

        
        [DllImport("RecordingPlugin")]
        private static extern bool AddAnalysisRecordingPath(string path, int pathLength);

        [DllImport("RecordingPlugin")]
        private static extern int ProcessAnalysisRequests(int analysisId, IntPtr intervalData);

        [DllImport("RecordingPlugin")]
        private static extern int ProcessAnalysisRequestsForAllFiles();

        [DllImport("RecordingPlugin")]
        private static extern int GetObjectPositions(int analysisId, int objectId, float startTime, float endTime,
            IntPtr positions, int maxPositions);

        [DllImport("RecordingPlugin")]
        protected static extern int GetOriginalID(int recorderId, string objectName, int objectNameLenght,
            int newObjectId);

        private float[] intervalData = new float[5000];

        private static int maxPositions = 1000;

        // 3 floats for position, 1 float for time
        private float[] positionData = new float[4 * maxPositions];

        public GameObject intervalButtonPrefab;
        public GameObject recordingUICanvas;
        public GameObject objectPositionParent;
        public InputActionProperty containmentPointSelectionAction;

        [Tooltip("The maximum number of individual analysis queries allowed for processing.")]
        public int maxAnalysisQueryCount = 3;


        public List<AnalysisRequestType> analysisRequestTypes;

        public Camera selectedGO1Camera;
        public Camera selectedGO2Camera;

        public Material transparentMaterial;

        public List<Color> positionPreviewColors = new List<Color>();

        private GameObject timeVisualisationParent;
        private int currentAnalysisQueryId = 0;
        private RecorderState state;
        private ImmersiveAnnotation immersiveAnnotation;
        private Dictionary<int, List<AnalysisRequest>> requests;
        private Dictionary<int, List<TimeInterval>> intervals;
        
        private GameObject slider,
            selectedGo1,
            selectedGo2,
            analysisUIParent,
            analysisRequestUIParent,
            annotationsUIParent;

        private ViewingSetupHMDAnatomy hmd;
        private XRRayInteractor rayInteractor;
        private LineRenderer lineRenderer;

        private Dictionary<int, float> analysisIdUIPos;

        private List<GameObject> intervalButtons;
        private Dictionary<int, bool> idActive;
        private List<Color> colors;

        private const byte AnnotationEventCode = 50;
        private const byte PreviewAnnotationEventCode = 51;
        private const byte DeleteAnnotationEventCode = 52;
        
        private int _currentSoundId = 0;
        private float _currentTSearchInterval = 0.5f;
        private float _currentSoundThreshold = 0.0f;
        private float _currentVelocityThreshold = 0.0f;
        private float _currentThreshold = 0.4f;
        private float _currentThreshold2 = 10.0f;
        private float _currentMovementThreshold = 0.0f;
        private float _currentDistanceThreshold = 0.0f;
        private float _currentGazeDistanceThreshold = 0.0f;
        private float _currentConeAngleThreshold = 0.0f;
        private float _positionRetrievalStartTime = 0.0f;
        private float _positionRetrievalEndTime = 0.0f;
        private Vector3 _containmentMin = Vector3.zero;
        private Vector3 _containmentMax = Vector3.zero;
        private bool _requestsVisualized;
        private bool _go1Selection, _go2Selection, _boxMinSelection, _boxMaxSelection, _selectionActivated;
        private TextMeshProUGUI _textGO1, _textGO2;
        private bool _processingActive;

        
        private GameObject _distancePreview1, _distancePreview2;
        private GameObject _gazePreview1, _gazePreview2;
        private GameObject _containmentMinSphere, _containmentMaxSphere, _containmentCube;

        private List<GameObject> _positionPreviewObjects = new List<GameObject>();
        
        public void Start()
        {
            state = GetComponent<RecorderState>();
            immersiveAnnotation = GetComponent<ImmersiveAnnotation>();
            slider = Utils.GetChildByName(recordingUICanvas, "TimeSlider");
            analysisUIParent = Utils.GetChildByName(recordingUICanvas, "Analysis");
            analysisRequestUIParent = Utils.GetChildByName(analysisUIParent, "AnalysisRequests");
            annotationsUIParent = Utils.GetChildByName(analysisUIParent, "Annotations");

            colors = new List<Color>();
            intervalButtons = new List<GameObject>();
            requests = new Dictionary<int, List<AnalysisRequest>>();
            intervals = new Dictionary<int, List<TimeInterval>>();
            analysisIdUIPos = new Dictionary<int, float>();

            colors.Add(new Color(215 / 255.0f, 25 / 255.0f, 28 / 255.0f));
            colors.Add(new Color(253 / 255.0f, 174 / 255.0f, 97 / 255.0f));
            colors.Add(new Color(255 / 255.0f, 255 / 255.0f, 191 / 255.0f));
            colors.Add(new Color(171 / 255.0f, 217 / 255.0f, 233 / 255.0f));
            colors.Add(new Color(44 / 255.0f, 123 / 255.0f, 182 / 255.0f));

            timeVisualisationParent = new GameObject("TimeVisualisation");
            
            for (int i = 0; i < maxAnalysisQueryCount; ++i)
            {
                ClearAnalysisRequests(i);
                ClearAnalysisFilePaths(i);
            }

            //Button button = Utils.CreateButton(intervalButtonPrefab, "ModifyQueryID", Color.white, analysisUIParent.transform, -200.0f, 0.0f, -1,-1,new Vector2(2.0f, 1.0f), 48);
            //button.onClick.AddListener(() => IncreaseAnalysisQueryId());
            //
            //button = Utils.CreateButton(intervalButtonPrefab, "ProcessQueries", Color.white, analysisUIParent.transform, -200.0f, 250.0f, -1,-1,new Vector2(2.0f, 1.0f), 48);
            //button.onClick.AddListener(() => ProcessRequests());
            //
            //button = Utils.CreateButton(intervalButtonPrefab, "AddRequest", Color.white, analysisUIParent.transform, -200.0f, 500.0f, -1,-1,new Vector2(2.0f, 1.0f), 48);
            //button.onClick.AddListener(() => VisualizeRequestTypes(button));

         


            if (selectedGO1Camera != null)
                selectedGO1Camera.enabled = false;
            if (selectedGO2Camera != null)
                selectedGO1Camera.enabled = false;
            //VisualizeRequests();
        }

        public void Update()
        {
            if (!photonView.IsMine)
                return;
            HandleInput();
        }

        private void VisualizeRequestTypes(Button parentButton)
        {
            float xOffset = 0.0f;
            float yOffset = 250.0f;
            List<Button> buttons = new List<Button>();
            for (int i = 0; i < analysisRequestTypes.Count; ++i)
            {
                Button button = Utils.CreateButton(intervalButtonPrefab, analysisRequestTypes[i].ToString(),
                    Color.white, parentButton.transform, xOffset, yOffset, -1, -1, new Vector2(2.0f, 1.0f), 48);
                AnalysisRequestType type = analysisRequestTypes[i];
                button.onClick.AddListener(() => VisualizeRequestTypeDetail(button, type, 100.0f, 450.0f));
                buttons.Add(button);
                xOffset += 450.0f;
            }

            parentButton.onClick.RemoveAllListeners();
            parentButton.GetComponent<UnityEngine.UI.Outline>().enabled = true;
            parentButton.onClick.AddListener(() =>
                Utils.DestroyButtons(buttons, parentButton, Color.white, () => VisualizeRequestTypes(parentButton)));
        }

        private void VisualizeRequestTypeDetail(Button parentButton, AnalysisRequestType requestType, float xOffset,
            float yOffset)
        {
            Button button = Utils.CreateButton(intervalButtonPrefab, "Details", Color.white, parentButton.transform,
                xOffset, yOffset, -1, -1, new Vector2(3.0f, 3.0f), 48);

            switch (requestType)
            {
                case AnalysisRequestType.DistanceAnalysis:
                {
                    button.onClick.AddListener(() =>
                        AddDistanceRequest(currentAnalysisQueryId, selectedGo1, selectedGo2,
                            _currentDistanceThreshold, LogicalOperator.And));
                    break;
                }
                case AnalysisRequestType.ContainmentAnalysis:
                {
                    button.onClick.AddListener(() =>
                        AddContainmentRequest(currentAnalysisQueryId, selectedGo1, _containmentMin, _containmentMax, LogicalOperator.And));
                    break;
                }
                case AnalysisRequestType.GazeAnalysis:
                {
                    button.onClick.AddListener(() => AddGazeRequest(currentAnalysisQueryId, selectedGo1, selectedGo2,
                        _currentConeAngleThreshold, _currentGazeDistanceThreshold, LogicalOperator.And));
                    break;
                }
                case AnalysisRequestType.MovementAnalysis:
                {
                    button.onClick.AddListener(() =>
                        AddMovementRequest(currentAnalysisQueryId, selectedGo1, 1.0f, _currentMovementThreshold, LogicalOperator.And));
                    break;
                }
                case AnalysisRequestType.VelocityAnalysis:
                {
                    button.onClick.AddListener(() =>
                        AddVelocityRequest(currentAnalysisQueryId, selectedGo1, 0.3f, _currentVelocityThreshold, LogicalOperator.And));
                    break;
                }
                case AnalysisRequestType.SoundActivationAnalysis:
                {
                    button.onClick.AddListener(() =>
                        AddSoundActivationRequest(currentAnalysisQueryId, _currentSoundId, 1.0f,
                            _currentSoundThreshold, LogicalOperator.And));
                    break;
                }
            }

            button.onClick.AddListener(() => Utils.DestroyButton(button, parentButton,
                () => VisualizeRequestTypeDetail(parentButton, requestType, xOffset, yOffset)));
            button.onClick.AddListener(() => VisualizeRequests());

            parentButton.onClick.RemoveAllListeners();
            parentButton.GetComponent<UnityEngine.UI.Outline>().enabled = true;
            parentButton.onClick.AddListener(() => Utils.DestroyButton(button, parentButton,
                () => VisualizeRequestTypeDetail(parentButton, requestType, xOffset, yOffset)));
        }

        public void ProcessRequests()
        {
            ProcessRequestsAsync();
        }
        public async void ProcessRequestsAsync()
        {
            if (!photonView.IsMine)
                return;
            //Utils.DestroyChildren(annotationsUIParent);

            if (!_processingActive)
            {
                _processingActive = true;
                Debug.Log("Processing all requests.");
                
                foreach (var kv in requests)
                {
                    int queryId = kv.Key;
                    List<TimeInterval> intervals = await Task.Run(() => ProcessRequest(queryId));
                    CreateIntervalButtons(intervals);
                }
                
                //ProcessAnalysisRequestsForAllFiles();
                
                _processingActive = false;
                Debug.Log("Finished processing all requests.");
            }
        }

        public void ClearAnalysisRequestsForCurrentID()
        {
            if (!photonView.IsMine)
                return;
            
            
            ClearAnalysisRequests(currentAnalysisQueryId);

            if (requests.ContainsKey(currentAnalysisQueryId) && requests[currentAnalysisQueryId] != null)
            {
                foreach (var request in requests[currentAnalysisQueryId])
                {
                    request.ClearVisualisations();
                }
                
                requests[currentAnalysisQueryId].Clear();
            }

            if (_requestsVisualized)
                VisualizeRequests();
        }

        private unsafe List<TimeInterval> ProcessRequest(int analysisQueryId)
        {
            int count = 0;

            fixed (float* i = intervalData)
            {
                count = ProcessAnalysisRequests(analysisQueryId, (IntPtr)i);
            }

            intervals.Clear();

            TimeInterval currentInterval = new TimeInterval();

            if (!intervals.ContainsKey(analysisQueryId))
                intervals[analysisQueryId] = new List<TimeInterval>();

            Debug.Log("Retrieved " + count + " intervals for analysis query with id: " + analysisQueryId);
            for (int i = 0; i < count * 2; i += 2)
            {
                currentInterval.startTime = intervalData[i];
                currentInterval.startTime = Mathf.Max(currentInterval.startTime, 0.0f);
                currentInterval.endTime = intervalData[i + 1];
                currentInterval.endTime = Mathf.Min(currentInterval.endTime, state.recordingDuration);
                currentInterval.analysisId = analysisQueryId;
                intervals[analysisQueryId].Add(currentInterval);
                //Debug.Log("Start: " + currentInterval.startTime + ", End: " + currentInterval.endTime);
            }

            return intervals[analysisQueryId];
        }

        public void SetRecordingPath(int analysisId, string path)
        {
            AddAnalysisRecordingPath(path, path.Length);
            
        }

        private void IncreaseAnalysisQueryId()
        {
            currentAnalysisQueryId = (currentAnalysisQueryId + 1) % maxAnalysisQueryCount;
            if (!requests.ContainsKey(currentAnalysisQueryId))
                requests[currentAnalysisQueryId] = new List<AnalysisRequest>();
        }

        public void SetAnalysisQueryId(int id)
        {
            if (!photonView.IsMine)
                return;
            currentAnalysisQueryId = id % maxAnalysisQueryCount;
            
            Debug.Log("Current analysis query id: " + currentAnalysisQueryId);
        }

        public void UpdateAnalysisQueryText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "current Analysis Query: " + currentAnalysisQueryId;
        }

        public void SetSoundAnalysisId(int id)
        {
            if (!photonView.IsMine)
                return;
            _currentSoundId = id;
            Debug.Log("Current sound id: " + _currentSoundId);
        }

        public void UpdateSoundAnalysisIdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            if(_currentSoundId == 0)
                text.text = "Current sound analysis user: Adam";
            else 
                text.text = "Current sound analysis user: Bob";
        }

        public void SetDistanceAnalysisThreshold(float t)
        {
            if (!photonView.IsMine)
                return;
            _currentDistanceThreshold = t;

            if (selectedGo1 != null)
            {
                if (_distancePreview1 == null)
                {
                    _distancePreview1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _distancePreview1.name = "AnalysisPreview";
                    _distancePreview1.GetComponent<Collider>().enabled = false;
                    _distancePreview1.transform.SetParent(selectedGo1.transform, false);
                    Renderer r = _distancePreview1.GetComponent<Renderer>();
                    r.material = transparentMaterial;
                    r.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g, colors[currentAnalysisQueryId].b, 0.2f);
                }

                Utils.SetLocalScale(_distancePreview1.transform, _currentDistanceThreshold * Vector3.one);
            }

            if (selectedGo2 != null)
            {
                if (_distancePreview2 == null)
                {
                    _distancePreview2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _distancePreview2.name = "AnalysisPreview";
                    _distancePreview2.GetComponent<Collider>().enabled = false;
                    _distancePreview2.transform.SetParent(selectedGo2.transform, false);
                    Renderer r = _distancePreview2.GetComponent<Renderer>();
                    r.material = transparentMaterial;
                    r.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g, colors[currentAnalysisQueryId].b, 0.2f);
                }

                Utils.SetLocalScale(_distancePreview2.transform, _currentDistanceThreshold * Vector3.one);
            }
        }

        public void UpdateDistanceAnalysisThresholdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current distance threshold: " + _currentDistanceThreshold;
        }

        public void SetPositionRetrievalStartTime(float t)
        {
            if (!photonView.IsMine)
                return;
            _positionRetrievalStartTime = t;
        }

        public void UpdatePositionRetrievalStartTimeText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current start time: " + _positionRetrievalStartTime;
        }

        public void SetPositionRetrievalEndTime(float t)
        {
            if (!photonView.IsMine)
                return;
            _positionRetrievalEndTime = t;
        }

        public void UpdatePositionRetrievalEndTimeText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current end time: " + _positionRetrievalEndTime;
        }

        public void SetGazeDistanceAnalysisThreshold(float t)
        {
            if (!photonView.IsMine)
                return;
            _currentGazeDistanceThreshold = t;
            
            if (selectedGo1 != null)
            {
                if (_gazePreview1 == null)
                {
                    _gazePreview1 = new GameObject();
                    _gazePreview1.name = "AnalysisPreview";
                    DynamicConicalFrustum frustum = _gazePreview1.AddComponent<DynamicConicalFrustum>();
                    float radius = Mathf.Tan(_currentConeAngleThreshold * Mathf.Deg2Rad) * _currentGazeDistanceThreshold;
                    frustum.RecreateFrustum(_currentGazeDistanceThreshold,0.001f, radius);
                    _gazePreview1.transform.SetParent(selectedGo1.transform, false);
                    Renderer r = _gazePreview1.GetComponent<Renderer>();
                    r.material = transparentMaterial;
                    r.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g, colors[currentAnalysisQueryId].b, 0.2f);

                    _gazePreview1.transform.localRotation = Quaternion.Euler(-90.0f,0.0f,0.0f);
                }

                DynamicConicalFrustum f = _gazePreview1.GetComponent<DynamicConicalFrustum>();
                float rad = Mathf.Tan(_currentConeAngleThreshold * Mathf.Deg2Rad) * _currentGazeDistanceThreshold;
                f.RecreateFrustum(_currentGazeDistanceThreshold,0.001f, rad);
            }
        }

        public void UpdateGazeDistanceAnalysisThresholdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current gaze distance threshold: " + _currentGazeDistanceThreshold;
        }

        public void SetConeAngleAnalysisThreshold(float t)
        {
            if (!photonView.IsMine)
                return;
            _currentConeAngleThreshold = t;
            
            if (selectedGo1 != null)
            {
                if (_gazePreview1 == null)
                {
                    _gazePreview1 = new GameObject();
                    DynamicConicalFrustum frustum = _gazePreview1.AddComponent<DynamicConicalFrustum>();
                    float radius = Mathf.Tan(_currentConeAngleThreshold * Mathf.Deg2Rad) * _currentGazeDistanceThreshold;
                    frustum.RecreateFrustum(_currentGazeDistanceThreshold,0.001f, radius);
                    _gazePreview1.transform.SetParent(selectedGo1.transform, false);
                    Renderer r = _gazePreview1.GetComponent<Renderer>();
                    r.material = transparentMaterial;
                    r.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g, colors[currentAnalysisQueryId].b, 0.2f);

                    _gazePreview1.transform.localRotation = Quaternion.Euler(-90.0f,0.0f,0.0f);
                }

                DynamicConicalFrustum f = _gazePreview1.GetComponent<DynamicConicalFrustum>();
                float rad = Mathf.Tan(_currentConeAngleThreshold * Mathf.Deg2Rad) * _currentGazeDistanceThreshold;
                f.RecreateFrustum(_currentGazeDistanceThreshold,0.001f, rad);
            }

            if (selectedGo2 != null)
            {
                if (_gazePreview2 != null)
                {
                    _gazePreview2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _gazePreview2.name = "AnalysisPreview";
                    _gazePreview2.GetComponent<Collider>().enabled = false;
                    _gazePreview2.transform.SetParent(selectedGo2.transform, false);
                    Renderer r = _gazePreview2.GetComponent<Renderer>();
                    r.material = transparentMaterial;
                    r.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g,
                        colors[currentAnalysisQueryId].b, 0.2f);
                    _gazePreview2.transform.localScale = 1.4f * Vector3.one;
                }
            }
        }

        public void UpdateConeAngleAnalysisThresholdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current cone angle threshold: " + _currentConeAngleThreshold;
        }

        public void SetSoundAnalysisThreshold(float t)
        {
            if (!photonView.IsMine)
                return;
            _currentSoundThreshold = t;
        }

        public void UpdateSoundAnalysisThresholdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current sound threshold: " + _currentSoundThreshold;
        }

        public void SetVelocityAnalysisThreshold(float t)
        {
            if (!photonView.IsMine)
                return;
            _currentVelocityThreshold = t;
        }

        public void UpdateVelocityAnalysisThresholdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current velocity threshold: " + _currentVelocityThreshold;
        }

        public void SetMovementAnalysisThreshold(float t)
        {
            if (!photonView.IsMine)
                return;
            _currentMovementThreshold = t;
        }

        public void UpdateMovementAnalysisThresholdText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Current movement threshold: " + _currentMovementThreshold;
        }

        public void ActivateGameObject1Selection()
        {
            if (!photonView.IsMine)
                return;
            _go1Selection = true;
        }

        public void ActivateGameObject2Selection()
        {
            if (!photonView.IsMine)
                return;
            _go2Selection = true;
        }

        public void ActivateBoxMinSelection()
        {
            if (!photonView.IsMine)
                return;
            _boxMinSelection = true;
            containmentPointSelectionAction.action.Enable();
        }

        public void ActivateBoxMaxSelection()
        {
            if (!photonView.IsMine)
                return;
            _boxMaxSelection = true;
            containmentPointSelectionAction.action.Enable();
        }

        public void UpdateSelectedGO1Text(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            if (text != null)
            {
                if (selectedGo1 != null)
                    text.text = "Selected Gameobject: " + selectedGo1.name;
                else
                    text.text = "Selected Gameobject: None";

                text.enabled = true;
                if (_textGO1 == null)
                    _textGO1 = text;
            }
        }

        public void UpdateSelectedGO2Text(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            if (text != null)
            {
                if (selectedGo1 != null)
                    text.text = "Selected Gameobject: " + selectedGo2.name;
                else
                    text.text = "Selected Gameobject: None";

                text.enabled = true;
                if (_textGO2 == null)
                    _textGO2 = text;
            }
        }

        private void DisableGO1Camera()
        {
            if (!photonView.IsMine)
                return;
            selectedGO1Camera.enabled = false;
        }

        private void DisableGO2Camera()
        {
            if (!photonView.IsMine)
                return;
            selectedGO2Camera.enabled = false;
        }

        private void HandleInput()
        {
            if (!photonView.IsMine)
                return;
            if (state.currentState == State.Replaying)
            {
                if (EnsureViewingSetup())
                {
                    if (rayInteractor == null && hmd != null)
                        rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();

                    if ((_go1Selection || _go2Selection) && !_selectionActivated)
                    {
                        rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("Objects");
                        rayInteractor.enableUIInteraction = false;
                        _selectionActivated = true;
                    }
                    else if (!_go1Selection && !_go2Selection && _selectionActivated)
                    {
                        rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("UI");
                        rayInteractor.enableUIInteraction = true;
                        _selectionActivated = false;
                    }

                    List<IXRSelectInteractable> interactables = rayInteractor.interactablesSelected;

                    if (interactables.Count > 0 && _go1Selection)
                    {
                        selectedGo1 = interactables[0].transform.gameObject;
                        _go1Selection = false;
                        UpdateSelectedGO1Text(_textGO1);
                        _textGO1.text = selectedGo1.name;
                        Debug.Log("Selected object: " + selectedGo1.name);

                        if (selectedGO1Camera != null)
                        {
                            // see: https://forum.unity.com/threads/fit-object-exactly-into-perspective-cameras-field-of-view-focus-the-object.496472/
                            Renderer[] renderers = selectedGo1.GetComponentsInChildren<Renderer>();
                            Bounds bounds = new Bounds(selectedGo1.transform.position, 0.1f * Vector3.one);
                            foreach (var renderer in renderers)
                                if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                                    if ((renderer.transform.position - selectedGo1.transform.position).magnitude < 1.0f)
                                        bounds.Encapsulate(renderer.bounds);

                            Vector3 extend = bounds.max - bounds.center;
                            float virtualSphereRadius = Mathf.Max(Mathf.Max(Mathf.Abs(extend.x), Mathf.Abs(extend.y)),
                                Mathf.Abs(extend.z));
                            var marginPercentage = 1.5f;
                            var minDistance = (virtualSphereRadius * marginPercentage) /
                                              Mathf.Tan(selectedGO1Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                            selectedGO1Camera.transform.position = bounds.center + minDistance *
                                (selectedGO1Camera.transform.position - bounds.center).normalized;
                            selectedGO1Camera.transform.LookAt(bounds.center);

                            selectedGO1Camera.enabled = true;
                            //selectedGO1Camera.transform.position = selectedGo1.transform.position + 0.5f * Vector3.back;
                            //selectedGO1Camera.transform.LookAt(selectedGo1.transform.position);
                            Invoke(nameof(DisableGO1Camera), 0.1f);
                        }
                    }

                    if (interactables.Count > 0 && _go2Selection)
                    {
                        selectedGo2 = interactables[0].transform.gameObject;
                        _go2Selection = false;
                        UpdateSelectedGO2Text(_textGO2);
                        Debug.Log("Selected object: " + selectedGo2.name);

                        if (selectedGO2Camera != null)
                        {
                            // see: https://forum.unity.com/threads/fit-object-exactly-into-perspective-cameras-field-of-view-focus-the-object.496472/
                            Renderer[] renderers = selectedGo2.GetComponentsInChildren<Renderer>();
                            Bounds bounds = new Bounds(selectedGo2.transform.position, 0.1f * Vector3.one);
                            foreach (var renderer in renderers)
                                if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                                    if ((renderer.transform.position - selectedGo2.transform.position).magnitude < 1.0f)
                                        bounds.Encapsulate(renderer.bounds);

                            Vector3 extend = bounds.max - bounds.center;
                            float virtualSphereRadius = Mathf.Max(Mathf.Max(Mathf.Abs(extend.x), Mathf.Abs(extend.y)),
                                Mathf.Abs(extend.z));
                            var marginPercentage = 1.1f;
                            var minDistance = (virtualSphereRadius * marginPercentage) /
                                              Mathf.Tan(selectedGO2Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                            selectedGO2Camera.transform.position = bounds.center + minDistance *
                                (selectedGO2Camera.transform.position - bounds.center).normalized;
                            selectedGO2Camera.transform.LookAt(bounds.center);
                            selectedGO2Camera.enabled = true;
                            Invoke(nameof(DisableGO2Camera), 0.1f);
                        }
                    }

                    if (_boxMinSelection)
                    {
                        if (containmentPointSelectionAction.action.IsPressed())
                        {
                            _containmentMin = hmd.rightController.transform.position;
                            _boxMinSelection = false;
                            containmentPointSelectionAction.action.Disable();
                            if (_containmentMinSphere == null)
                            {
                                _containmentMinSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                _containmentMinSphere.transform.localScale = 0.05f * Vector3.one;
                            }

                            _containmentMinSphere.transform.position = _containmentMin;

                            if (_containmentMaxSphere != null)
                            {
                                if (_containmentCube == null)
                                {
                                    _containmentCube = Instantiate(Resources.Load<GameObject>("Models/ContainmentBox"));
                                    Collider[] colliders = _containmentCube.GetComponentsInChildren<Collider>();
                                    foreach (var collider in colliders)
                                    {
                                        collider.enabled = false;
                                    }
                                    
                                    Renderer[] renderers = _containmentCube.GetComponentsInChildren<Renderer>();
                                    foreach (var renderer in renderers)
                                    {
                                        renderer.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g, colors[currentAnalysisQueryId].b, 0.2f);
                                    }
                                }

                                Vector3 diff = _containmentMax - _containmentMin;
                                _containmentCube.transform.localScale = diff;
                                _containmentCube.transform.position = _containmentMin + 0.5f * diff;
                            }
                        }
                    }

                    if (_boxMaxSelection)
                    {
                        if (containmentPointSelectionAction.action.IsPressed())
                        {
                            _containmentMax = hmd.rightController.transform.position;
                            _boxMaxSelection = false;
                            containmentPointSelectionAction.action.Disable();

                            if (_containmentMaxSphere == null)
                            {
                                _containmentMaxSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                _containmentMaxSphere.transform.localScale = 0.05f * Vector3.one;
                            }

                            _containmentMaxSphere.transform.position = _containmentMax;

                            if (_containmentMinSphere != null)
                            {
                                if (_containmentCube == null)
                                {
                                    _containmentCube = Instantiate(Resources.Load<GameObject>("Models/ContainmentBox"));
                                    Collider[] colliders = _containmentCube.GetComponentsInChildren<Collider>();
                                    foreach (var collider in colliders)
                                    {
                                        collider.enabled = false;
                                    }
                                    
                                    Renderer[] renderers = _containmentCube.GetComponentsInChildren<Renderer>();
                                    foreach (var renderer in renderers)
                                    {
                                        renderer.material.color = new Color(colors[currentAnalysisQueryId].r, colors[currentAnalysisQueryId].g, colors[currentAnalysisQueryId].b, 0.2f);
                                    }
                                    
                                }

                                Vector3 diff = _containmentMax - _containmentMin;
                                _containmentCube.transform.localScale = diff;
                                _containmentCube.transform.position = _containmentMin + 0.5f * diff;
                            }
                        }
                    }
                }
            }
            else
            {
                if (objectPositionParent != null && objectPositionParent.transform.childCount > 0)
                {
                    ClearPositionPreviews();
                }
                
                if(_containmentCube != null)
                    Destroy(_containmentCube);
            }
        }

        private void CreateIntervalButtons(List<TimeInterval> intervals)
        {
            if (intervals.Count == 0)
                return;

            //GameObject intervalButton = Instantiate(intervalButtonPrefab);
            //intervalButton.transform.SetParent(annotationsUIParent.transform, false);
            //Button button = intervalButton.GetComponent<Button>();
            //button.onClick.AddListener(() => VisualizeQuery(intervals[0].analysisId));
            //RectTransform rectTransform = intervalButton.GetComponent<RectTransform>();
            //RectTransform sliderRectTransform = slider.GetComponent<RectTransform>();
            //float buttonPosXStart = -70;
            //float buttonPosXEnd = 70;
            //float buttonWidth = buttonPosXEnd - buttonPosXStart;
            //float padding = 30.0f;
            //float yOffset = intervals[0].analysisId * (sliderRectTransform.rect.height + padding) + 100.0f;
            //rectTransform.sizeDelta = new Vector2(buttonWidth, rectTransform.sizeDelta.y);
            //rectTransform.localPosition = new Vector3(sliderRectTransform.localPosition.x - sliderRectTransform.rect.width / 2 - buttonPosXStart - buttonWidth, yOffset, 0.0f);
            //rectTransform.localScale = Vector3.one;
            //rectTransform.localEulerAngles = Vector3.zero;
            //Image buttonBackground = intervalButton.GetComponent<Image>();
            //buttonBackground.color = Color.clear;
            //TextMeshProUGUI text = intervalButton.GetComponentInChildren<TextMeshProUGUI>();
            //text.text = "ID: " + intervals[0].analysisId;
            //text.fontSize = 48.0f;

            int authorId = PhotonNetwork.LocalPlayer.ActorNumber;
            string author = PhotonNetwork.LocalPlayer.NickName;

            for (int j = 0; j < intervals.Count; ++j)
            {
                if (intervals[j].endTime - intervals[j].startTime > 0.01f)
                {
                    string title = ""; //intervals[j].analysisId.ToString();
                    float startTime = intervals[j].startTime;
                    float endTime = intervals[j].endTime;
                    int originalId = -1;
                    int annotationTypeId = intervals[j].analysisId;
                    immersiveAnnotation.Annotate(null, annotationTypeId, startTime, endTime, PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.LocalPlayer.NickName, true);
                    photonView.RPC(nameof(CreateAnalysisAnnotation), RpcTarget.AllBuffered, title, startTime, endTime,
                        authorId, author, originalId, annotationTypeId);
                    //CreateIntervalButton(intervals[j]);
                }
            }
        }

        [PunRPC]
        public void CreateAnalysisAnnotation(string title, float startTime, float endTime, int authorId, string author,
            int objectId, int annotationTypeId)
        {
            if (!photonView.IsMine)
                return;
            Annotation annotation = new Annotation();
            annotation.title = title;
            annotation.annotationTypeId = annotationTypeId;
            annotation.startTime = startTime;
            annotation.endTime = endTime;
            annotation.authorId = authorId;
            annotation.author = author;
            annotation.objectId = objectId;
            annotation.automaticAnnotation = true;

            immersiveAnnotation.CreateAnnotationButton(annotation);
        }

        public void DeleteAutomaticAnnotationsForCurrentId()
        {
            immersiveAnnotation.DeleteAutomaticAnnotations(currentAnalysisQueryId);
        }

        public void ToggleRequestsVisualization()
        {
            if (!photonView.IsMine)
                return;
            if (!_requestsVisualized)
            {
                VisualizeRequests();
                _requestsVisualized = true;
            }
            else
            {
                Utils.DestroyChildren(analysisRequestUIParent);
                _requestsVisualized = false;
            }
        }

        private void VisualizeRequests()
        {
            if (!photonView.IsMine)
                return;
            Utils.DestroyChildren(analysisRequestUIParent);
            analysisIdUIPos.Clear();

            RectTransform sliderRectTransform = slider.GetComponent<RectTransform>();
            float xPos = sliderRectTransform.localPosition.x -
                         1.5f * (sliderRectTransform.rect.width / 2) * sliderRectTransform.localScale.x;

            RectTransform t = analysisRequestUIParent.GetComponent<RectTransform>();
            t.localPosition = new Vector3(xPos, t.localPosition.y, t.localPosition.z);


            Vector2 scale = new Vector2(3.0f, 1.0f);
            float fontSize = 75.0f;

            foreach (var kv in requests)
            {
                if (!analysisIdUIPos.ContainsKey(kv.Key))
                    analysisIdUIPos[kv.Key] = 0;

                int analysisId = kv.Key;
                float xOffset = 0.0f;
                float yOffset = 50.0f;
                float offset = 10.0f;
                float width = 100.0f;
                float height = 100.0f;

                if (analysisId >= 0)
                {
                    xOffset = analysisIdUIPos[analysisId];
                    yOffset = analysisId * (height + offset + 5.0f) * 2 * scale.y;
                    analysisIdUIPos[analysisId] += (width + offset) * scale.x;
                }

                Button idButton = Utils.CreateButton(intervalButtonPrefab, "ID: " + kv.Key, Color.clear,
                    analysisRequestUIParent.transform, xOffset, yOffset, -1, -1, scale, fontSize);
                TextMeshProUGUI text = idButton.GetComponentInChildren<TextMeshProUGUI>();
                if (kv.Key == currentAnalysisQueryId)
                {
                    text.outlineColor = new Color32(255, 0, 0, 255);
                    text.outlineWidth = 0.1f;
                }
                else
                {
                    text.outlineWidth = 0.0f;
                }

                foreach (var request in kv.Value)
                {
                    analysisId = request.getAnalysisId();
                    xOffset = 50.0f;
                    yOffset = 50.0f;
                    offset = 10.0f;
                    width = 100.0f;
                    height = 100.0f;

                    if (analysisId >= 0)
                    {
                        xOffset = analysisIdUIPos[analysisId] + 60;
                        yOffset = analysisId * (height + offset + 5.0f) * 2 * scale.y;
                        analysisIdUIPos[analysisId] += (width + offset) * 2 * scale.x;
                    }

                    Button button = Utils.CreateButton(intervalButtonPrefab, request.ShortText(),
                        request.GetButtonColor(), analysisRequestUIParent.transform, xOffset, yOffset, -1, -1, scale,
                        fontSize);
                    button.onClick.AddListener(() => VisualizeAnalysisRequestDetails(request, button));
                }
            }
        }

        private void VisualizeAnalysisRequestDetails(AnalysisRequest request, Button invokingButton)
        {
            if (!photonView.IsMine)
                return;
            Vector2 scale = new Vector2(10.0f, 4.0f);
            float fontSize = 48.0f;
            Button detailButton = null;
            UnityEngine.UI.Outline outline = invokingButton.gameObject.GetComponent<UnityEngine.UI.Outline>();
            outline.enabled = true;

            string text = request.Text();

            if (Utils.GetChildBySubstring(analysisRequestUIParent, text) == null)
                detailButton = Utils.CreateButton(intervalButtonPrefab, text, Color.white,
                    invokingButton.transform, 0.0f, 600.0f, -1, -1, scale, fontSize);

            if (detailButton != null && invokingButton != null)
            {
                invokingButton.onClick.RemoveAllListeners();
                invokingButton.onClick.AddListener(() => Utils.DestroyButton(detailButton, invokingButton,
                    () => VisualizeAnalysisRequestDetails(request, invokingButton)));
            }
        }

        private void VisualizeQuery(int analysisQueryId)
        {
            if (!photonView.IsMine)
                return;
            Debug.LogError("Test! Analysis Query ID: " + analysisQueryId);
        }

        public void ClearPositionPreviews()
        {
            if (!photonView.IsMine)
                return;
            photonView.RPC(nameof(ClearPositionPreviewsRPC), RpcTarget.All);
        }

        [PunRPC]
        private void ClearPositionPreviewsRPC()
        {
            if (objectPositionParent != null)
            {
                foreach (Transform child in objectPositionParent.transform)
                {
                    Destroy(child.gameObject);
                }

                foreach (var go in _positionPreviewObjects)
                {
                    if(go != null)
                        Utils.ResetColor(go);
                }
            }

            if (timeVisualisationParent != null)
            {
                foreach (Transform child in timeVisualisationParent.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void AddPositionRetrievalRequest()
        {
            if (!photonView.IsMine)
                return;
            string name = Utils.GetObjectName(selectedGo1).Replace("[Rec]", "");
            int originalId = GetOriginalID(state.recorderID, name, name.Length, selectedGo1.GetInstanceID());

            int count = 0;

            if (_positionRetrievalEndTime < _positionRetrievalStartTime)
            {
                (_positionRetrievalEndTime, _positionRetrievalStartTime) =
                    (_positionRetrievalStartTime, _positionRetrievalEndTime);
            }

            photonView.RPC(nameof(AddPositionRetrievalRequestRPC), RpcTarget.All, name, currentAnalysisQueryId, state.selectedReplayFile, originalId, _positionRetrievalStartTime, _positionRetrievalEndTime);
        }

        [PunRPC]
        private unsafe void AddPositionRetrievalRequestRPC(string name, int queryId, string fileName, int originalId,
            float positionRetrievalStartTime, float positionRetrievalEndTime)
        {
          

            if (state.recordingDirectory.Length < 3)
            {
                state.recordingDirectory = Application.persistentDataPath;
            }

            SetRecordingPath(queryId, state.recordingDirectory + "/" + fileName);

            PositionRetrieval(name,queryId,fileName,originalId, positionRetrievalStartTime, positionRetrievalEndTime);
        }

        private unsafe int ProcessPositionRetrieval(int queryId, int originalId, float positionRetrievalStartTime, float positionRetrievalEndTime)
        {
            int count = 0;
            fixed (float* i = positionData)
            {
                count = GetObjectPositions(queryId, originalId, positionRetrievalStartTime, positionRetrievalEndTime,
                    (IntPtr)i, maxPositions);
            }

            return count;
        }
        
        private async void PositionRetrieval(string name, int queryId, string fileName, int originalId,
            float positionRetrievalStartTime, float positionRetrievalEndTime)
        {
            int count = await Task.Run(() => ProcessPositionRetrieval(queryId, originalId, positionRetrievalStartTime, positionRetrievalEndTime));
            
            if (count > 0 && objectPositionParent != null)
            {
                GameObject positionPreview = new GameObject("Preview: " + originalId);
                int colorId = objectPositionParent.transform.childCount;
                Color baseColor = positionPreviewColors.Count > colorId ? positionPreviewColors[colorId] : Color.red;

                GameObject go = Utils.GetObjectByHierarchyName(name);
                if (go != null)
                {
                    Utils.SetColor(go, baseColor);
                    _positionPreviewObjects.Add(go);
                }

                positionPreview.transform.SetParent(objectPositionParent.transform);
                
                LineRenderer lineRenderer = positionPreview.AddComponent<LineRenderer>();
                List<float> recordedTimes = new List<float>();

                List<LineRendererPositionParams> recordedPositionParam = new List<LineRendererPositionParams>();
                Color32 previousColor = new Color32();
                float nextVisualisationTime = 0.0f;
                for (int i = 0; i < count; ++i)
                {
                    Vector3 position = new Vector3(positionData[i * 4], positionData[i * 4 + 1], positionData[i * 4 + 2]);
                    float time = positionData[i * 4 + 3];
                    float totalVelocity = 0.0f;
                    float nextTime = 0.0f;
                    Vector3 nextPosition = new Vector3();
                    if (i < count - 1)
                    {
                        nextPosition = new Vector3(positionData[(i+1) * 4], positionData[(i+1) * 4 + 1], positionData[(i+1) * 4 + 2]);
                        nextTime = positionData[(i+1) * 4 + 3];
                        float posDif = (nextPosition - position).magnitude;
                        float timeDif = Mathf.Abs(nextTime - time);
                        totalVelocity = posDif / timeDif;
                    }

                    float colorInfluence = Mathf.Clamp01(totalVelocity) / 2.0f + 0.5f;
                    Color32 color = baseColor * new Color(colorInfluence, colorInfluence, colorInfluence);

                    LineRendererPositionParams param = new LineRendererPositionParams(position, previousColor, color);
                    previousColor = color;

                    if (nextTime > nextVisualisationTime)
                    {
                        float t = (nextVisualisationTime - time)/ (nextTime - time);
                        Vector3 pos = position + t * (nextPosition - position);
                        GameObject timeVisualisation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        timeVisualisation.transform.SetParent(timeVisualisationParent.transform);
                        timeVisualisation.transform.localScale = 0.02f * Vector3.one;
                        timeVisualisation.transform.position = pos;
                        Color sphereColor = Color.white * (1.0f - (nextTime - positionRetrievalStartTime) / (positionRetrievalEndTime - positionRetrievalStartTime));
                        MeshRenderer sphereMaterial = timeVisualisation.GetComponent<MeshRenderer>();
                        sphereMaterial.material.color = sphereColor;
                        GameObject timeText = new GameObject();
                        timeText.transform.SetParent(timeVisualisation.transform, false);
                        timeText.transform.localScale = 0.3f * Vector3.one;
                        TextMeshPro textMeshPro = timeText.AddComponent<TextMeshPro>();
                        textMeshPro.text = nextVisualisationTime.ToString("0");
                        nextVisualisationTime += 2.0f;
                        timeVisualisation.AddComponent<HoverInformation>();
                        Rigidbody rigidbody = timeVisualisation.AddComponent<Rigidbody>();
                        rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                        timeText.SetActive(false);
                    }

                    recordedPositionParam.Add(param);
                    recordedTimes.Add(positionData[i * 4 + 3]);
                }

                lineRenderer.numCornerVertices = 3;
                //lineRenderer.positionCount = count;
                //lineRenderer.SetPositions(recordedPositions.ToArray());
                lineRenderer.material = Resources.Load<Material>("Materials/PositionLineMaterial");
                lineRenderer.startWidth = 0.03f;
                lineRenderer.endWidth = 0.03f;
                lineRenderer.useWorldSpace = true;
                //lineRenderer.startColor = Color.blue;
                //lineRenderer.endColor = Color.red;
                lineRenderer.receiveShadows = true;
                lineRenderer.shadowCastingMode = ShadowCastingMode.On;

                Gradient colorGradient = new Gradient();
                GradientColorKey[] colorKey = new GradientColorKey[count];

                //for (int i = 0; i < count - 1; ++i)
                //{
                //    float posDif = (recordedPositions[i + 1] - recordedPositions[i]).magnitude;
                //    float timeDif = Mathf.Abs(recordedTimes[i + 1] - recordedTimes[i]);
                //    float totalVelocity = posDif / timeDif;
                //    float relativeVelocity = totalVelocity;
                //    Color color = new Color(relativeVelocity, 0.0f, 0.0f);
                //    colorKey[i].color = color;
                //    colorKey[i].time = (recordedTimes[i] - _positionRetrievalStartTime) /
                //                       (_positionRetrievalEndTime - _positionRetrievalStartTime);
                //    if (i == 0)
                //        colorKey[i].time = 0.0f;
                //}

                colorKey[count - 1].time = 1.0f;
                colorKey[count - 1].color = Color.white;
                //colorGradient.colorKeys = colorKey;

                //lineRenderer.colorGradient = colorGradient;
                List<Color32> colors = LineRendererExtensions.SetPositionsWithColors(lineRenderer,recordedPositionParam);
                Texture2D texture = new Texture2D(1024, 1024);
                LineRendererExtensions.SetVertexColorMap(lineRenderer, colors.ToArray(), texture);
                lineRenderer.numCapVertices = 10;
                lineRenderer.numCornerVertices = 10;
            }
        }
        public void AddAndSoundActivationRequest()
        {
            if (!photonView.IsMine)
                return;
            SoundActivationAnalysisRequest request = new SoundActivationAnalysisRequest(currentAnalysisQueryId,
                _currentSoundId, _currentTSearchInterval, _currentSoundThreshold, LogicalOperator.And);
            AddRequest(currentAnalysisQueryId, request);

            if (_requestsVisualized)
                VisualizeRequests();
        }
        
        public void AddOrSoundActivationRequest()
        {
            if (!photonView.IsMine)
                return;
            SoundActivationAnalysisRequest request = new SoundActivationAnalysisRequest(currentAnalysisQueryId,
                _currentSoundId, _currentTSearchInterval, _currentSoundThreshold, LogicalOperator.Or);
            AddRequest(currentAnalysisQueryId, request);

            if (_requestsVisualized)
                VisualizeRequests();
        }
        
        public void AddAndNegSoundActivationRequest()
        {
            if (!photonView.IsMine)
                return;
            SoundActivationAnalysisRequest request = new SoundActivationAnalysisRequest(currentAnalysisQueryId,
                _currentSoundId, _currentTSearchInterval, _currentSoundThreshold, LogicalOperator.AndNegated);
            AddRequest(currentAnalysisQueryId, request);

            if (_requestsVisualized)
                VisualizeRequests();
        }
        
        public void AddOrNegSoundActivationRequest()
        {
            if (!photonView.IsMine)
                return;
            SoundActivationAnalysisRequest request = new SoundActivationAnalysisRequest(currentAnalysisQueryId,
                _currentSoundId, _currentTSearchInterval, _currentSoundThreshold, LogicalOperator.OrNegated);
            AddRequest(currentAnalysisQueryId, request);

            if (_requestsVisualized)
                VisualizeRequests();
        }

        public void AddAndMovementRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                MovementAnalysisRequest request = new MovementAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentMovementThreshold, LogicalOperator.And);
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddOrMovementRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                MovementAnalysisRequest request = new MovementAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentMovementThreshold, LogicalOperator.Or);
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddAndNegMovementRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                MovementAnalysisRequest request = new MovementAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentMovementThreshold, LogicalOperator.AndNegated);
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddOrNegMovementRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                MovementAnalysisRequest request = new MovementAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentMovementThreshold, LogicalOperator.OrNegated);
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }

        public void AddAndVelocityRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                VelocityAnalysisRequest request = new VelocityAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentVelocityThreshold, LogicalOperator.And);
                AddRequest(currentAnalysisQueryId, request);
     
                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddOrVelocityRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                VelocityAnalysisRequest request = new VelocityAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentVelocityThreshold, LogicalOperator.Or);
                AddRequest(currentAnalysisQueryId, request);
     
                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddAndNegVelocityRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                VelocityAnalysisRequest request = new VelocityAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentVelocityThreshold, LogicalOperator.AndNegated);
                AddRequest(currentAnalysisQueryId, request);
     
                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddOrNegVelocityRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                VelocityAnalysisRequest request = new VelocityAnalysisRequest(currentAnalysisQueryId, state.recorderID,
                    selectedGo1, _currentTSearchInterval, _currentVelocityThreshold, LogicalOperator.OrNegated);
                AddRequest(currentAnalysisQueryId, request);
     
                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        public void AddAndGazeRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                GazeAnalysisRequest request = new GazeAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentConeAngleThreshold, _currentGazeDistanceThreshold, LogicalOperator.And);
                request.SetGazePreviewGOs(_gazePreview1, _gazePreview2);
                _gazePreview1 = null;
                _gazePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddOrGazeRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                GazeAnalysisRequest request = new GazeAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentConeAngleThreshold, _currentGazeDistanceThreshold, LogicalOperator.Or);
                request.SetGazePreviewGOs(_gazePreview1, _gazePreview2);
                _gazePreview1 = null;
                _gazePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddAndNegGazeRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                GazeAnalysisRequest request = new GazeAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentConeAngleThreshold, _currentGazeDistanceThreshold, LogicalOperator.AndNegated);
                request.SetGazePreviewGOs(_gazePreview1, _gazePreview2);
                _gazePreview1 = null;
                _gazePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }

        public void AddOrNegGazeRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                GazeAnalysisRequest request = new GazeAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentConeAngleThreshold, _currentGazeDistanceThreshold, LogicalOperator.OrNegated);
                request.SetGazePreviewGOs(_gazePreview1, _gazePreview2);
                _gazePreview1 = null;
                _gazePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddAndContainmentRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null)
            {
                ContainmentAnalysisRequest request = new ContainmentAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, _containmentMin, _containmentMax, LogicalOperator.And);
                
                request.SetContainmentCube(_containmentCube);
                Destroy(_containmentMinSphere);
                Destroy(_containmentMaxSphere);
                _containmentCube = null;
                
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }

        public void AddAndDistanceRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                DistanceAnalysisRequest request = new DistanceAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentDistanceThreshold, LogicalOperator.And);
                request.SetDistancePreviewGOs(_distancePreview1, _distancePreview2);
                _distancePreview1 = null;
                _distancePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        public void AddOrDistanceRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                DistanceAnalysisRequest request = new DistanceAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentDistanceThreshold, LogicalOperator.Or);
                request.SetDistancePreviewGOs(_distancePreview1, _distancePreview2);
                _distancePreview1 = null;
                _distancePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddAndNegDistanceRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                DistanceAnalysisRequest request = new DistanceAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentDistanceThreshold, LogicalOperator.AndNegated);
                request.SetDistancePreviewGOs(_distancePreview1, _distancePreview2);
                _distancePreview1 = null;
                _distancePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }
        
        public void AddOrNegDistanceRequest()
        {
            if (!photonView.IsMine)
                return;
            if (selectedGo1 != null && selectedGo2 != null)
            {
                DistanceAnalysisRequest request = new DistanceAnalysisRequest(currentAnalysisQueryId, state.recorderID, selectedGo1, selectedGo2, _currentDistanceThreshold, LogicalOperator.OrNegated);
                request.SetDistancePreviewGOs(_distancePreview1, _distancePreview2);
                _distancePreview1 = null;
                _distancePreview2 = null;
                AddRequest(currentAnalysisQueryId, request);

                if (_requestsVisualized)
                    VisualizeRequests();
            }
        }

        private void AddSoundActivationRequest(int analysisQueryId, int soundId, float tSearchInterval, float actLevel, LogicalOperator logicalOperator)
        {
            if (!photonView.IsMine)
                return;
            SoundActivationAnalysisRequest request =
                new SoundActivationAnalysisRequest(analysisQueryId, soundId, tSearchInterval, actLevel, logicalOperator);
            AddRequest(analysisQueryId, request);
        }

        
        private void AddMovementRequest(int analysisQueryId, GameObject go, float tSearchInterval, float rotThreshold, LogicalOperator logicalOperator)
        {
            if (!photonView.IsMine)
                return;
            if (go != null)
            {
                MovementAnalysisRequest request = new MovementAnalysisRequest(analysisQueryId, state.recorderID, go,
                    tSearchInterval, rotThreshold, logicalOperator);
                AddRequest(analysisQueryId, request);
            }
        }

        private void AddVelocityRequest(int analysisQueryId, GameObject go, float tSearchInterval, float velThreshold, LogicalOperator logicalOperator)
        {
            if (!photonView.IsMine)
                return;
            if (go != null)
            {
                VelocityAnalysisRequest request = new VelocityAnalysisRequest(analysisQueryId, state.recorderID, go,
                    tSearchInterval, velThreshold, logicalOperator);
                AddRequest(analysisQueryId, request);
            }
        }

        private void AddGazeRequest(int analysisQueryId, GameObject go1, GameObject go2, float coneAngle, float maxDist, LogicalOperator logicalOperator)
        {
            if (!photonView.IsMine)
                return;
            if (go1 != null && go2 != null)
            {
                GazeAnalysisRequest request = new GazeAnalysisRequest(analysisQueryId, state.recorderID, go1, go2, coneAngle, maxDist, logicalOperator);
                request.SetGazePreviewGOs(_gazePreview1, _gazePreview2);
                _gazePreview1 = null;
                _gazePreview2 = null;
                AddRequest(analysisQueryId, request);
            }
        }

        private void AddContainmentRequest(int analysisQueryId, GameObject go, Vector3 min, Vector3 max, LogicalOperator logicalOperator)
        {
            if (!photonView.IsMine)
                return;
            if (go != null)
            {
                ContainmentAnalysisRequest request =
                    new ContainmentAnalysisRequest(analysisQueryId, state.recorderID, go, min, max, logicalOperator);
                AddRequest(analysisQueryId, request);
            }
        }

        private void AddDistanceRequest(int analysisQueryId, GameObject go1, GameObject go2, float dist, LogicalOperator logicalOperator)
        {
            if (!photonView.IsMine)
                return;
            if (go1 != null && go2 != null)
            {
                DistanceAnalysisRequest request = new DistanceAnalysisRequest(analysisQueryId, state.recorderID, go1, go2, dist, logicalOperator);
                request.SetDistancePreviewGOs(_distancePreview1, _distancePreview2);
                _distancePreview1 = null;
                _distancePreview2 = null;
                AddRequest(analysisQueryId, request);
            }
        }

        private void AddRequest(int analysisQueryId, AnalysisRequest request)
        {
            if (!photonView.IsMine)
                return;
            if (!requests.ContainsKey(analysisQueryId))
            {
                requests[analysisQueryId] = new List<AnalysisRequest>();
                intervals[analysisQueryId] = new List<TimeInterval>();
                //ClearAnalysisRequests(analysisQueryId);
                
                SetAnalysisPaths(analysisQueryId);
            }

            requests[analysisQueryId].Add(request);
        }

        private void SetAnalysisPaths(int analysisQueryId)
        {
            Debug.Log("Setting analysis path for id: " + analysisQueryId);
            SetRecordingPath(analysisQueryId, state.recordingDirectory + "/" + state.selectedReplayFile);
            // TODO: allow for the analysis of multiple recordings
            //SetRecordingPath(analysisQueryId, state.recordingDirectory + "/" + "APlausEMR_S2_dyad_11_partner_0_trial_1_date_03_02_2023_11_34");
        }
        
        private void CreateIntervalButton(TimeInterval interval)
        {
            if (!photonView.IsMine)
                return;
            GameObject intervalButton = Instantiate(intervalButtonPrefab);
            intervalButton.transform.SetParent(annotationsUIParent.transform, false);
            // set correct position on time slider
            RectTransform rectTransform = intervalButton.GetComponent<RectTransform>();
            RectTransform sliderRectTransform = slider.GetComponent<RectTransform>();
            float posPerS = sliderRectTransform.rect.width / state.recordingDuration;
            float buttonPosXStart = posPerS * interval.startTime;
            float buttonPosXEnd = posPerS * interval.endTime;
            float buttonWidth = buttonPosXEnd - buttonPosXStart;
            float padding = 30.0f;
            float yOffset = interval.analysisId * (sliderRectTransform.rect.height + padding) + 100.0f;
            rectTransform.sizeDelta = new Vector2(buttonWidth, rectTransform.sizeDelta.y);
            rectTransform.localPosition =
                new Vector3(
                    sliderRectTransform.localPosition.x - sliderRectTransform.rect.width / 2 + buttonPosXStart +
                    0.5f * buttonWidth, yOffset, 0.0f);
            rectTransform.localScale = new Vector3(1.0f, 0.2f, 1.0f);
            rectTransform.localEulerAngles = Vector3.zero;
            // set button background color
            if (interval.analysisId >= 0 && colors.Count > interval.analysisId)
            {
                Image buttonBackground = intervalButton.GetComponent<Image>();
                buttonBackground.color = colors[interval.analysisId];
            }

            TextMeshProUGUI text = intervalButton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = "";
            intervalButtons.Add(intervalButton);
        }

        bool EnsureViewingSetup()
        {
            if (!photonView.IsMine)
                return false;

            if (hmd != null) return true;

            if (NetworkUser.localNetworkUser.viewingSetupAnatomy == null) return false;

            if (!(NetworkUser.localNetworkUser.viewingSetupAnatomy is ViewingSetupHMDAnatomy)) return false;

            hmd = NetworkUser.localNetworkUser.viewingSetupAnatomy as ViewingSetupHMDAnatomy;

            //rayInteractor = hmd.leftController.AddComponent<XRRayInteractor>();

            return true;
        }
    }
}