using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Vrsys;
using Vrsys.Scripts.Recording;
using VRSYS.Scripts.Recording;

namespace VRSYS.Recording.Scripts
{
    [Serializable]
    public class Annotation
    {
        public string title = "";
        public int annotationTypeId = -1;
        public float startTime = -1.0f;
        public float endTime = -1.0f;
        public int objectId = 0;
        public int authorId = -1;
        public string author = "";
        public bool automaticAnnotation = false;
        public int id = 0;
    }

    [Serializable]
    public class Annotations
    {
        public List<Annotation> annotations = new List<Annotation>();
    }

    [Serializable]
    public class AnnotationButton
    {
        public GameObject buttonGo;
        public Button button;
        public Annotation annotation;
        public Button invokingButton;
        public List<Button> childButtons = new List<Button>();
    }

    [RequireComponent(typeof(RecorderState))]
    public class ImmersiveAnnotation : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public GameObject recordingUICanvas;
        public GameObject annotationButtonPrefab;
        public List<string> annotationTypes;
        public InputActionProperty switchAnnotationTypeAction;
        public InputActionProperty createGeneralAnnotationAction;
        public InputActionProperty UpdatedAnnotationIDAction;
        private GameObject previewAnnotationButton;

        private RecorderState state;
        private int currentAnnotationTypeId;
        private GameObject slider, selectedGo;
        private ViewingSetupHMDAnatomy hmd;
        private XRRayInteractor rayInteractor;
        private LineRenderer lineRenderer;
        private float startAnnotationTime, endAnnotationTime, lastAnnotationTypeSwitch;

        private GameObject annotationsUIParent, analysisUIParent;
        private RectTransform sliderRectTransform;
        private Annotations annotations;
        private List<AnnotationButton> annotationButtons;
        private Dictionary<int, Outline> idOutlines;
        private Dictionary<int, bool> idActive;
        private List<Color> colors;
        private RayHandler _rayHandler;
        private InteractionLayerMask _initialLayerState = new InteractionLayerMask();

        private float _lastIDSwitch = 0.0f;
        
        // are the initial annotations retrieved from the server and visualized?
        private bool init;
        private bool _retrievingAnnotationsStarted = false;
        private bool _objectAnnotationModeActivated = false;
        private bool _generalAnnotationModeActivated = false;
        private bool _creatingGeneralAnnotation = false;

        private const byte AnnotationEventCode = 50;
        private const byte PreviewAnnotationEventCode = 51;
        private const byte DeleteAnnotationEventCode = 52;

        private TooltipHandler tooltipHandler;
        private Tooltip annotationTooltip;
        private Tooltip annotationIDTooltip;
        private AnnotationButton activatedButton;
        private Gradient annotationGradient = new Gradient();
        private Gradient baseGradient = new Gradient();
        private LineRenderer lineVisual;
        private GameObject leftControllder, rightController;
        private MaterialHandler selectedGOMaterialHandler;
        public Material annotationMaterial;
        private Sprite deleteTex;
        private int annotationCounter = 0;
        
        public void Start()
        {
            state = GetComponent<RecorderState>();
            slider = Utils.GetChildByName(recordingUICanvas, "TimeSlider");
            annotationsUIParent = Utils.GetChildByName(recordingUICanvas, "Annotations");
            analysisUIParent = Utils.GetChildByName(recordingUICanvas, "Analysis");

            RectTransform rectTransform = annotationsUIParent.GetComponent<RectTransform>();
            sliderRectTransform = slider.GetComponent<RectTransform>();

            rectTransform.localScale = sliderRectTransform.localScale;
            rectTransform.position = sliderRectTransform.position;

            switchAnnotationTypeAction.action.Enable();
            createGeneralAnnotationAction.action.Enable();
            UpdatedAnnotationIDAction.action.Enable();
            
            annotations = new Annotations();
            annotations.annotations = new List<Annotation>();
            annotationButtons = new List<AnnotationButton>();
            idOutlines = new Dictionary<int, Outline>();
            idActive = new Dictionary<int, bool>();
            colors = new List<Color>();

            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;
                
            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            colorKey = new GradientColorKey[2];
            colorKey[0].color = Color.red;
            colorKey[0].time = 0.0f;
            colorKey[1].color = Color.blue;
            colorKey[1].time = 1.0f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 0.0f;
            alphaKey[1].time = 1.0f;

            annotationGradient.SetKeys(colorKey, alphaKey);
            
            colorKey = new GradientColorKey[1];
            colorKey[0].color = new Color(0.03426475f,0.990566f,0.2243f);
            colorKey[0].time = 0.0f;
            
            alphaKey = new GradientAlphaKey[1];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            baseGradient.SetKeys(colorKey, alphaKey);
            //Button exportButton = Utils.CreateButton(annotationButtonPrefab, "Export", Color.white,
            //    analysisUIParent.transform, 500, 500,
            //    400, 400, Vector2.one, 40);

            //exportButton.onClick.AddListener(() => ExportAnnotations());

            colors.Add(new Color(215 / 255.0f, 25 / 255.0f, 28 / 255.0f));
            colors.Add(new Color(253 / 255.0f, 174 / 255.0f, 97 / 255.0f));
            colors.Add(new Color(255 / 255.0f, 255 / 255.0f, 191 / 255.0f));
            colors.Add(new Color(171 / 255.0f, 217 / 255.0f, 233 / 255.0f));
            colors.Add(new Color(44 / 255.0f, 123 / 255.0f, 182 / 255.0f));

            if (photonView.IsMine)
            {
                tooltipHandler = NetworkUser.localGameObject.GetComponent<TooltipHandler>();

                annotationTooltip = new Tooltip();
                annotationTooltip.hand = TooltipHand.Right;
                annotationTooltip.tooltipName = "Annotate";
                annotationTooltip.tooltipText = "Annotate";
                annotationTooltip.actionButtonReference = Tooltip.ActionButton.Grip;
                tooltipHandler.AddTooltip(annotationTooltip);

                annotationIDTooltip = new Tooltip();
                annotationIDTooltip.hand = TooltipHand.Right;
                annotationIDTooltip.tooltipName = "AnnotationType";
                annotationIDTooltip.tooltipText = "AnnotationType";
                annotationIDTooltip.actionButtonReference = Tooltip.ActionButton.Thumbstick;
                tooltipHandler.AddTooltip(annotationIDTooltip);

                deleteTex = Resources.Load<Sprite>("Textures/delete-01");
            }

            _rayHandler = NetworkUser.localGameObject.GetComponent<RayHandler>();
            startAnnotationTime = -1.0f;
        }


        public void OnEvent(EventData photonEvent)
        {
            if (!photonView.IsMine)
                return;

            if (photonEvent.Code == AnnotationEventCode)
            {
                //Debug.Log("Start recording event code received at time " + Time.time);
                object[] data = (object[])photonEvent.CustomData;
                string title = (string)data[0];
                float startTime = (float)data[1];
                float endTime = (float)data[2];
                int authorId = (int)data[3];
                string author = (string)data[4];
                int originalId = (int)data[5];
                int annotationTypeId = (int)data[6];
                bool automaticAnnotation = (bool)data[7];

                if (startTime > endTime)
                {
                    (endTime, startTime) = (startTime, endTime);
                }
                CreateAnnotation(title, startTime, endTime, authorId, author, originalId, annotationTypeId, automaticAnnotation);
            } 
            else if (photonEvent.Code == DeleteAnnotationEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                int annotationTypeId = (int)data[0];
                float startTime = (float)data[1];
                float endTime = (float)data[2];

                if (startTime > endTime)
                {
                    (endTime, startTime) = (startTime, endTime);
                }
                DeleteAnnotationRPC(annotationTypeId, startTime, endTime);
            }
            else if (photonEvent.Code == PreviewAnnotationEventCode)
            {
                object[] data = (object[])photonEvent.CustomData;
                float startTime = (float)data[0];
                float endTime = (float)data[1];
                int id = (int)data[2];
                
                if (startTime > endTime)
                {
                    (endTime, startTime) = (startTime, endTime);
                }
                UpdatePreviewAnnotationButton(startTime, endTime, id);
            }
        }

        public void ExportAnnotations()
        {
            if (!photonView.IsMine)
                return;
            
            string date = DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("es-ES")).Replace(" ", "_")
                .Replace(":", "_").Replace("/", "_");
            string filePath = Application.persistentDataPath + "/export_" + date + ".csv";

            StreamWriter writer = new StreamWriter(filePath);

            writer.WriteLine("id;author;startTime;endTime;title;objectId;objectName;automaticAnnotation");

            for (int i = 0; i < annotations.annotations.Count; ++i)
            {
                int id = annotations.annotations[i].annotationTypeId;
                string author = annotations.annotations[i].author;
                float startTime = annotations.annotations[i].startTime;
                float endTime = annotations.annotations[i].endTime;
                string title = annotations.annotations[i].title;
                int objectId = annotations.annotations[i].objectId;
                bool automaticAnnotation = annotations.annotations[i].automaticAnnotation;
                string objectName = "";

                if (state.originalIdGameObjects.ContainsKey(objectId))
                    objectName = Utils.GetObjectName(state.originalIdGameObjects[objectId]);

                writer.WriteLine(id + ";" + author + ";" + startTime + ";" + endTime + ";" + title + ";" + objectId +
                                 ";" + objectName + ";" + automaticAnnotation);
            }

            writer.Flush();
            writer.Close();
        }

        public void SetActiveAnnotationID(int id)
        {
            if (!photonView.IsMine)
                return;
            currentAnnotationTypeId = id % annotationTypes.Count;

            Debug.Log("Current annotation id: " + currentAnnotationTypeId);
        }

        public void UpdateAnnotationText(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            text.text = "Annotation Type: " + annotationTypes[currentAnnotationTypeId];
        }

        public void Update()
        {
            if (!photonView.IsMine)
                return;
            HandleInput();

            if (!_objectAnnotationModeActivated && _rayHandler.GetRayState(RayHandler.Hand.Right) == 1)
            {
                _generalAnnotationModeActivated = false;
                _objectAnnotationModeActivated = true;
                
                
                if(hmd == null)
                    hmd = NetworkUser.localNetworkUser.viewingSetupAnatomy as ViewingSetupHMDAnatomy;
                
                if (rayInteractor == null && hmd != null)
                {
                    rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();
                    lineRenderer = hmd.rightController.GetComponentInChildren<LineRenderer>();
                    lineRenderer.enabled = true;
                    rightController = Utils.GetChildBySubstring(hmd.gameObject, "right_oculus_controller_mesh");
                }
                
                if(rayInteractor != null)
                    rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("Objects");
            }

            if (!_generalAnnotationModeActivated && _rayHandler.GetRayState(RayHandler.Hand.Right) == 0)
            {
                _generalAnnotationModeActivated = true;
                _objectAnnotationModeActivated = false;
                
                if(hmd == null)
                    hmd = NetworkUser.localNetworkUser.viewingSetupAnatomy as ViewingSetupHMDAnatomy;
                
                if (rayInteractor == null && hmd != null)
                {
                    rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();
                    lineRenderer = hmd.rightController.GetComponentInChildren<LineRenderer>();
                    lineRenderer.enabled = true;
                    rightController = Utils.GetChildBySubstring(hmd.gameObject, "right_oculus_controller_mesh");
                }
                
                if(rayInteractor != null)
                    rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("UI");
            }

            Vector2 input = UpdatedAnnotationIDAction.action.ReadValue<Vector2>();
            if (Time.time - _lastIDSwitch > 1.0f && input.y > 0.2f)
            {
                _lastIDSwitch = Time.time;
                currentAnnotationTypeId = (currentAnnotationTypeId + 1) % annotationTypes.Count;
                string text = "AnnotationType: "  + annotationTypes[currentAnnotationTypeId];
                annotationIDTooltip.tooltipText = text;
                tooltipHandler.UpdateTooltipText(annotationIDTooltip, text);
                tooltipHandler.UpdateTooltipColor(annotationIDTooltip, colors[currentAnnotationTypeId]);
            }

            if (Time.time - _lastIDSwitch > 1.0f && input.y < -0.2f)
            {
                _lastIDSwitch = Time.time;
                currentAnnotationTypeId = (currentAnnotationTypeId - 1) % annotationTypes.Count;
                while(currentAnnotationTypeId < 0)
                    currentAnnotationTypeId += annotationTypes.Count;
                string text = "AnnotationType: "  + annotationTypes[currentAnnotationTypeId];
                annotationIDTooltip.tooltipText = text;
                tooltipHandler.UpdateTooltipText(annotationIDTooltip, text);
                tooltipHandler.UpdateTooltipColor(annotationIDTooltip, colors[currentAnnotationTypeId]);
            }
            
            
            if (state.currentState == State.Idle && init)
                UploadAndClearAnnotations();
            else if (state.currentState == State.Replaying && !init && !_retrievingAnnotationsStarted)
            {
                StartCoroutine(RetrieveAnnotations());
                tooltipHandler.ShowTooltip(annotationTooltip);
                tooltipHandler.ShowTooltip(annotationIDTooltip);
                string text = "AnnotationType: "  + annotationTypes[currentAnnotationTypeId];
                annotationIDTooltip.tooltipText = text;
                tooltipHandler.UpdateTooltipText(annotationIDTooltip, text);
                tooltipHandler.UpdateTooltipColor(annotationIDTooltip, colors[currentAnnotationTypeId]);
            }
            else if (state.currentState == State.Replaying && init)
            {
                VisualizeAnnotations();

                //if (lineRenderer.enabled)
                //{
                //    _objectAnnotationModeActivated = true;
                //    _generalAnnotationModeActivated = false;
                //}
                //else
                //{
                //    _objectAnnotationModeActivated = false;
                //    _generalAnnotationModeActivated = true;
                //}
                AnnotationCreation();
            }
        }

        public void ToggleObjectAnnotationActivated()
        {
            if (!photonView.IsMine)
                return;
            _objectAnnotationModeActivated = !_objectAnnotationModeActivated;

            if (!_objectAnnotationModeActivated)
            {
                if (rayInteractor == null && hmd != null)
                {
                    rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();
                    lineRenderer = hmd.rightController.GetComponentInChildren<LineRenderer>();
                    lineRenderer.enabled = true;
                    rightController = Utils.GetChildBySubstring(hmd.gameObject, "right_oculus_controller_mesh");
                }
                
                rayInteractor.interactionLayers = _initialLayerState;
                rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("UI");
            }
            else
            {
                if (rayInteractor == null && hmd != null)
                {
                    rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();
                    lineRenderer = hmd.rightController.GetComponentInChildren<LineRenderer>();
                    lineRenderer.enabled = true;
                    rightController = Utils.GetChildBySubstring(hmd.gameObject, "right_oculus_controller_mesh");
                }
                
                _initialLayerState = rayInteractor.interactionLayers;
                rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("Objects");
            }
        }

        public void ToggleGeneralAnnotationActivated()
        {
            if (!photonView.IsMine)
                return;
            _generalAnnotationModeActivated = !_generalAnnotationModeActivated;
        }
        
        public void SetObjectAnnotationActivatedState(int state)
        {
            if (!photonView.IsMine)
                return;

            bool newState = state % 2 == 1;
            
            if (!_objectAnnotationModeActivated)
            {
                if (rayInteractor == null && hmd != null)
                {
                    rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();
                    lineRenderer = hmd.rightController.GetComponentInChildren<LineRenderer>();
                    lineRenderer.enabled = true;
                    lineVisual = rayInteractor.gameObject.GetComponent<LineRenderer>();
                    rightController = Utils.GetChildBySubstring(hmd.gameObject, "right_oculus_controller_mesh");
                }
                
                _initialLayerState = rayInteractor.interactionLayers;
            }

            _objectAnnotationModeActivated = newState;
            if (_objectAnnotationModeActivated)
            {
                rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("Objects");
            }
            else
            {
                rayInteractor.interactionLayers = 2 * InteractionLayerMask.NameToLayer("UI");
            }
        }

        public void SetGeneralAnnotationActivatedState(int state)
        {
            if (!photonView.IsMine)
                return;
            _generalAnnotationModeActivated = state % 2 == 1;
        }

        public void UpdateObjectAnnotationActivatedState(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            if (_objectAnnotationModeActivated)
                text.text = "Object Annotation Mode:" + " Activated";
            else
                text.text = "Object Annotation Mode:" + " Deactivated";
        }

        public void UpdateGeneralAnnotationActivatedState(TextMeshProUGUI text)
        {
            if (!photonView.IsMine)
                return;
            if (_generalAnnotationModeActivated)
                text.text = "General Annotation Mode:" + " Activated";
            else
                text.text = "General Annotation Mode:" + " Deactivated";
        }

        private void AnnotationCreation()
        {
            if (!photonView.IsMine)
                return;
            if (EnsureViewingSetup())
            {
                if (_objectAnnotationModeActivated)
                    ObjectAnnotation();
                if (_generalAnnotationModeActivated)
                    GeneralAnnotation();
            }
        }

        private void ObjectAnnotation()
        {
            if (rayInteractor == null && hmd != null)
            {
                rayInteractor = hmd.rightController.GetComponentInChildren<XRRayInteractor>();
                lineRenderer = hmd.rightController.GetComponentInChildren<LineRenderer>();
                lineRenderer.enabled = true;
                lineVisual = rayInteractor.gameObject.GetComponent<LineRenderer>();
                rightController = Utils.GetChildBySubstring(hmd.rightController, "right_oculus_controller_mesh");
            }

            List<IXRHoverInteractable> interactables = rayInteractor.interactablesHovered;
            if (interactables.Count > 0 && selectedGo == null && createGeneralAnnotationAction.action.IsPressed())
            {
                Debug.Log("Found object to annotate");
                selectedGo = interactables[0].transform.gameObject;
                startAnnotationTime = state.currentReplayTime;

                object[] data = new object[] { startAnnotationTime, state.currentReplayTime, currentAnnotationTypeId };
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(PreviewAnnotationEventCode, data, raiseEventOptions, SendOptions.SendReliable);
                
                if(lineRenderer != null)
                    lineRenderer.material.color = colors[currentAnnotationTypeId];
                
                selectedGOMaterialHandler = selectedGo.GetComponent<MaterialHandler>();
                selectedGOMaterialHandler.ChangeToAnnotationMaterial(colors[currentAnnotationTypeId]);
            }

            if (selectedGo != null && createGeneralAnnotationAction.action.IsPressed())
            {
                object[] data = new object[] { startAnnotationTime, state.currentReplayTime, currentAnnotationTypeId };
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(PreviewAnnotationEventCode, data, raiseEventOptions, SendOptions.SendReliable);
            }

            if (selectedGo != null && !createGeneralAnnotationAction.action.IsPressed())
            {
                endAnnotationTime = state.currentReplayTime;
                Debug.Log("Annotation created!");
                Annotate(selectedGo, currentAnnotationTypeId, startAnnotationTime, endAnnotationTime, PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.LocalPlayer.NickName);
                selectedGo = null;
                
                selectedGOMaterialHandler.EndAnnotation();
                
                if(lineRenderer != null)
                    lineRenderer.material.color = baseGradient.Evaluate(0.0f);;
            }
        }

        private void GeneralAnnotation()
        {
            if (createGeneralAnnotationAction.action.IsPressed())
            {
                _creatingGeneralAnnotation = true;
                if (startAnnotationTime < 0.0f)
                {
                    startAnnotationTime = state.currentReplayTime;

                    if (rightController != null)
                    {
                        Material rightControllerMaterial = rightController.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
                        rightControllerMaterial.EnableKeyword("_EMISSION");
                        rightControllerMaterial.SetColor("_EmissionColor", 0.15f * colors[currentAnnotationTypeId]);
                        rightControllerMaterial.EnableKeyword("_EMISSION");
                        rightController.GetComponent<SkinnedMeshRenderer>().sharedMaterial = rightControllerMaterial;
                    }
                }

                object[] data = new object[] { startAnnotationTime, state.currentReplayTime, currentAnnotationTypeId };
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(PreviewAnnotationEventCode, data, raiseEventOptions,
                        SendOptions.SendReliable);
            }
            else if (startAnnotationTime > 0.0f)
            {
                endAnnotationTime = state.currentReplayTime;
                Annotate(null, currentAnnotationTypeId, startAnnotationTime, endAnnotationTime, PhotonNetwork.LocalPlayer.ActorNumber, PhotonNetwork.LocalPlayer.NickName);
                startAnnotationTime = -1.0f;
                _creatingGeneralAnnotation = false;
                
                if (rightController != null)
                {
                    Material rightControllerMaterial = rightController.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
                    rightControllerMaterial.SetColor("_EmissionColor", Color.black);
                    rightController.GetComponent<SkinnedMeshRenderer>().sharedMaterial = rightControllerMaterial;
                }
            }
        }

        public void OnDestroy()
        {
            if (!photonView.IsMine)
                return;
            if (state != null && state.currentState == State.Idle && init)
                UploadAndClearAnnotations();

            if (state != null && state.currentState == State.Replaying)
            {
                ExportAnnotations();
            }
        }

        private void UploadAndClearAnnotations()
        {
            if (!photonView.IsMine)
                return;
            StartCoroutine(UploadAnnotations());
            init = false;

            if (idOutlines != null)
            {
                foreach (KeyValuePair<int, Outline> entry in idOutlines)
                {
                    if (entry.Value != null)
                        entry.Value.enabled = false;
                }

                idOutlines.Clear();
            }

            idActive.Clear();

            for (int i = 0; i < annotationButtons.Count; ++i)
            {
                if (annotationButtons[i].button != null)
                    Destroy(annotationButtons[i].button.gameObject);
            }

            annotationButtons.Clear();
            annotations.annotations.Clear();

            init = false;
            _retrievingAnnotationsStarted = false;
        }

        private void VisualizeAnnotations()
        {
            if (!photonView.IsMine)
                return;
            if (annotations != null && annotations.annotations != null)
            {
                for (int i = 0; i < annotations.annotations.Count; ++i)
                {
                    Annotation annotation = annotations.annotations[i];
                    int id = annotation.objectId;
                    if (idActive.ContainsKey(annotation.id))
                        idActive[annotation.id] = false;
                }

                for (int i = 0; i < annotations.annotations.Count; ++i)
                {
                    Annotation annotation = annotations.annotations[i];
                    int id = annotation.objectId;
                    int annotationOutlineId = annotation.id;

                    if (state.originalIdGameObjects.ContainsKey(id))
                    {
                        if (idOutlines != null && !idOutlines.ContainsKey(annotationOutlineId))
                        {
                            GameObject go = state.originalIdGameObjects[id];
                            if (go != null)
                            {
                                Outline outline = go.AddComponent<Outline>();
                                if (outline != null)
                                {
                                    outline.OutlineMode = Outline.Mode.OutlineVisible;
                                    Color c = colors[annotation.annotationTypeId];
                                    c.a = 0.5f;
                                    outline.OutlineColor = c;
                                    outline.OutlineWidth = (annotation.annotationTypeId + 1) * 2.0f;
                                    idOutlines.Add(annotationOutlineId, outline);
                                    idActive.Add(annotationOutlineId, false);
                                }
                            }
                        }
                        else
                        {
                            if (annotation.startTime <= state.currentReplayTime &&
                                state.currentReplayTime <= annotation.endTime)
                            {
                                idActive[annotationOutlineId] = true;
                                idOutlines[annotationOutlineId].enabled = true;
                                //if (annotation.annotationTypeId >= 0 && colors.Count > annotation.annotationTypeId)
                                //    idOutlines[annotationOutlineId].OutlineColor = colors[annotation.annotationTypeId];
                            }
                        }
                    }
                }


                if (idOutlines != null)
                {
                    foreach (KeyValuePair<int, Outline> entry in idOutlines)
                    {
                        if (!idActive[entry.Key])
                            entry.Value.enabled = false;
                    }
                }
            }
        }

        public void Annotate(GameObject selectedGameObject, int annotationTypeId, float startT, float endT, int authorId, string author, bool automatic=false)
        {
            if (!photonView.IsMine)
                return;
            string title = "placeholder";
            if (annotationTypes.Count > annotationTypeId)
                title = annotationTypes[annotationTypeId];

            float startTime = startT;
            float endTime = endT;
            int objectId = 0;
            if(selectedGameObject != null)
                objectId = selectedGameObject.GetInstanceID();
            int originalId = 0;
            if (state.newIdOriginalId.ContainsKey(objectId))
                originalId = state.newIdOriginalId[objectId];

            object[] data = new object[] { title, startTime, endTime, authorId, author, originalId, annotationTypeId, automatic };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(AnnotationEventCode, data, raiseEventOptions, SendOptions.SendReliable);

            startAnnotationTime = -1.0f;
        }

        private void HandleInput()
        {
            //if (switchAnnotationTypeAction.action.IsPressed() && Time.time - lastAnnotationTypeSwitch > 0.5f)
            //{
            //    currentAnnotationTypeId += 1;
            //    currentAnnotationTypeId %= annotationTypes.Count;
            //    lastAnnotationTypeSwitch = Time.time;
            //    selectedAnnotationTypeText.text = annotationTypes[currentAnnotationTypeId];
            //}
        }

        public void CreateAnnotation(string title, float startTime, float endTime, int authorId, string author,
            int objectId, int annotationTypeId, bool automatic)
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
            annotation.automaticAnnotation = automatic;
            annotation.id = annotationCounter++;

            if (previewAnnotationButton != null)
            {
                Destroy(previewAnnotationButton);
                previewAnnotationButton = null;
            }

            CreateAnnotationButton(annotation);
        }

        public void DeleteAutomaticAnnotations(int id)
        {
            for (int i = 0; i < annotationButtons.Count; ++i)
            {
                if (annotationButtons[i].button != null && annotationButtons[i].annotation.automaticAnnotation &&
                    annotationButtons[i].annotation.annotationTypeId == id)
                {

                    object[] data = new object[]
                    {
                        annotationButtons[i].annotation.annotationTypeId, annotationButtons[i].annotation.startTime,
                        annotationButtons[i].annotation.endTime
                    };
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    PhotonNetwork.RaiseEvent(DeleteAnnotationEventCode, data, raiseEventOptions, SendOptions.SendReliable);

                    //photonView.RPC(nameof(DeleteAutomaticAnnotations), RpcTarget.AllBuffered, currentAnalysisQueryId);
                    //Destroy(annotationButtons[i].button.gameObject);
                }
            }
        }

        private void UpdatePreviewAnnotationButton(float startTime, float currentTime, int currentId)
        {
            if (!photonView.IsMine)
                return;
            if (previewAnnotationButton == null)
            {
                RectTransform sliderRectTransform = slider.GetComponent<RectTransform>();
                float posPerS = sliderRectTransform.rect.width / state.recordingDuration;
                float buttonPosXStart = posPerS * startTime;
                float buttonPosXEnd = posPerS * currentTime;
                float buttonWidth = buttonPosXEnd - buttonPosXStart;
                float buttonHeight = 60.0f;
                float padding = 30.0f;
                Vector2 scale = Vector2.one;
                string text = "";

                float xOffset = -sliderRectTransform.rect.width / 2 + buttonPosXStart + 0.5f * buttonWidth;
                float yOffset = (currentId + 1) * (sliderRectTransform.rect.height + padding) + 30.0f;

                Button button = Utils.CreateButton(annotationButtonPrefab, text, colors[currentId],
                    annotationsUIParent.transform, xOffset, yOffset, buttonWidth, buttonHeight, scale, 1.0f);
                previewAnnotationButton = button.gameObject;
            }
            else
            {
                RectTransform rectTransform = previewAnnotationButton.GetComponent<RectTransform>();
                RectTransform sliderRectTransform = slider.GetComponent<RectTransform>();
                float posPerS = sliderRectTransform.rect.width / state.recordingDuration;
                float buttonPosXStart = posPerS * startTime;
                float buttonPosXEnd = posPerS * currentTime;
                float buttonWidth = buttonPosXEnd - buttonPosXStart;
                float padding = 30.0f;
                float yOffset = (currentId + 1) * (sliderRectTransform.rect.height + padding) + 30.0f;
                rectTransform.sizeDelta = new Vector2(buttonWidth, rectTransform.sizeDelta.y);

                rectTransform.localPosition =
                    new Vector3(
                        sliderRectTransform.localPosition.x - sliderRectTransform.rect.width / 2 + buttonPosXStart +
                        0.5f * buttonWidth, yOffset, 0.0f);
            }
        }

        public void CreateAnnotationButton(Annotation annotation)
        {
            if (!photonView.IsMine)
                return;
            annotations.annotations.Add(annotation);
            float posPerS = sliderRectTransform.rect.width / state.recordingDuration;
            float buttonPosXStart = posPerS * annotation.startTime;
            float buttonPosXEnd = posPerS * annotation.endTime;
            float buttonWidth = buttonPosXEnd - buttonPosXStart;
            float buttonHeight = 60.0f;
            float padding = 30.0f;
            Vector2 scale = Vector2.one;
            string text = ""; //annotation.title + ", start: " + annotation.startTime;

            float xOffset = -sliderRectTransform.rect.width / 2 + buttonPosXStart + 0.5f * buttonWidth;
            float yOffset = (annotation.annotationTypeId + 1) * (sliderRectTransform.rect.height + padding) + 30.0f;

            Button button = Utils.CreateButton(annotationButtonPrefab, text, colors[annotation.annotationTypeId],
                annotationsUIParent.transform, xOffset, yOffset, buttonWidth, buttonHeight, scale, 1.0f);
            

            /*
            GameObject annotationButtonGo = Instantiate(annotationButtonPrefab);
            annotationButtonGo.transform.SetParent(annotationsUIParent.transform, false);
            // set correct position on time slider
            RectTransform rectTransform = annotationButtonGo.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(buttonWidth, rectTransform.sizeDelta.y);

            rectTransform.localPosition = new Vector3(sliderRectTransform.localPosition.x + xOffset , yOffset, 0.0f);
            rectTransform.localScale = new Vector3(1.0f, 0.2f, 1.0f);
            rectTransform.localEulerAngles = Vector3.zero;
            // set text
            TextMeshProUGUI text = annotationButtonGo.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = annotation.title;
            // set button background color
            if (annotation.annotationTypeId >= 0 && colors.Count > annotation.annotationTypeId)
            {
                Image buttonBackground = annotationButtonGo.GetComponent<Image>();
                buttonBackground.color = colors[annotation.annotationTypeId];
            }

            Button button = annotationButtonGo.GetComponent<Button>();
            */

            AnnotationButton annotationButton = new AnnotationButton();
            annotationButton.buttonGo = button.gameObject;
            annotationButton.annotation = annotation;
            annotationButton.button = button;

            button.onClick.AddListener(() => VisualizeAnnotationInteractionOptions(annotationButton));

            annotationButtons.Add(annotationButton);
        }

        private void VisualizeAnnotationInteractionOptions(AnnotationButton annotationButton)
        {
            if (!photonView.IsMine)
                return;
         
            if (activatedButton != annotationButton && activatedButton != null)
            {
                AnnotationButton targetButton = null;
                for (int i = 0; i < annotationButtons.Count; ++i)
                    if (annotationButtons[i].button == activatedButton.button)
                        targetButton = annotationButtons[i];

                if (targetButton != null)
                {
                    targetButton.button.onClick.RemoveAllListeners();
                    Utils.DestroyButtons(targetButton.childButtons, targetButton.button,
                        colors[targetButton.annotation.annotationTypeId],
                        () => VisualizeAnnotationInteractionOptions(targetButton));
                    targetButton.childButtons.Clear();
                }
            }
            
            float xOffset = -110.0f;
            float yOffset = 120.0f;
            Vector2 scale = Vector2.one;
            float buttonWidth = 100.0f;
            float buttonHeight = 100.0f;
            Button deleteButton = Utils.CreateButton(annotationButtonPrefab, "", Color.white,
                annotationButton.buttonGo.transform, xOffset, yOffset, buttonWidth, buttonHeight, scale, 50.0f);

            foreach (Transform child in deleteButton.transform)
            {
                Image deleteImage = child.GetComponent<Image>();
                if (deleteImage != null)
                {
                    deleteImage.sprite = deleteTex;
                    child.gameObject.SetActive(true);
                }
            }
         

            xOffset = 10.0f;

            Button goButton = Utils.CreateButton(annotationButtonPrefab, "Go", Color.white,
                annotationButton.buttonGo.transform, xOffset, yOffset, buttonWidth, buttonHeight, scale, 50.0f);

            List<Button> buttons = new List<Button>();
            buttons.Add(deleteButton);
            buttons.Add(goButton);
            
            annotationButton.childButtons.Add(deleteButton);
            annotationButton.childButtons.Add(goButton);

            annotationButton.button.GetComponent<UnityEngine.UI.Outline>().enabled = true;
            Image image = annotationButton.buttonGo.GetComponent<Image>();
            Color selectedColor = colors[annotationButton.annotation.annotationTypeId] - 0.2f * Color.white;
            if (selectedColor.r < 0.0f)
                selectedColor.r = 0.0f;
            if (selectedColor.g < 0.0f)
                selectedColor.g = 0.0f;
            if (selectedColor.b < 0.0f)
                selectedColor.b = 0.0f;
            
            image.color = selectedColor;
            
            annotationButton.button.onClick.RemoveAllListeners();
            annotationButton.button.onClick.AddListener(() => Utils.DestroyButtons(buttons, annotationButton.button,
                colors[annotationButton.annotation.annotationTypeId],() => VisualizeAnnotationInteractionOptions(annotationButton)));

            List<Button> allButtons = new List<Button>();
            allButtons.Add(deleteButton);
            allButtons.Add(goButton);
            allButtons.Add(annotationButton.button);

            deleteButton.onClick.AddListener(() => DeleteAnnotation(annotationButton));
            goButton.onClick.AddListener(() => TemporalNavigationToAnnotation(annotationButton));
            
            activatedButton = annotationButton;
        }

        public void TemporalNavigationToAnnotation(AnnotationButton currentAnnotation)
        {
            if (state.currentState == State.Replaying)
            {
                state.currentReplayTime = currentAnnotation.annotation.startTime;
            }
        }

        public void DeleteAnnotation(AnnotationButton currentAnnotation)
        {
            if (!photonView.IsMine)
                return;
            // TODO: shared annotation deletion!
            Annotation a = currentAnnotation.annotation;
            
            object[] data = new object[] { a.annotationTypeId, a.startTime, a.endTime };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            PhotonNetwork.RaiseEvent(DeleteAnnotationEventCode, data, raiseEventOptions, SendOptions.SendReliable);
            //photonView.RPC("DeleteAnnotationRPC", RpcTarget.All, a.annotationTypeId, a.startTime, a.endTime);
        }

        [PunRPC]
        public void DeleteAnnotationRPC(int annotationType, float startTime, float endTime)
        {
            if (!photonView.IsMine)
                return;
            Annotation wanted = null;
            for (int i = 0; i < annotations.annotations.Count; ++i)
            {
                Annotation a = annotations.annotations[i];
                if (a.annotationTypeId == annotationType && Mathf.Abs(a.startTime - startTime) < 0.01f &&
                    Mathf.Abs(a.endTime - endTime) < 0.01f)
                {
                    wanted = a;
                    break;
                }
            }

            if (wanted != null)
            {
                annotations.annotations.Remove(wanted);

                if (idOutlines.ContainsKey(wanted.id))
                {
                    Outline outline = idOutlines[wanted.id];
                    if (outline != null)
                    {
                        Destroy(outline);
                        idOutlines.Remove(wanted.id);
                    }
                }

                AnnotationButton annotationButton = null;
                for (int i = 0; i < annotationButtons.Count; ++i)
                {
                    if (annotationButtons[i].annotation == wanted)
                    {
                        annotationButton = annotationButtons[i];
                        break;
                    }
                }

                if (annotationButton != null)
                {
                    Destroy(annotationButton.buttonGo);
                }
            }
            else
            {
                Debug.LogError("Could not find the required annotation for deletion!");
            }
        }

        private IEnumerator RetrieveAnnotations()
        {
            if (!photonView.IsMine)
                yield return true;
            _retrievingAnnotationsStarted = true;
            if (state.selectedServer.Length > 0 && state.selectedReplayFile.Length > 0)
            {
                string completeURL = state.selectedServer + "/annotations/" + state.selectedReplayFile;

                using (var uwr = new UnityWebRequest(completeURL, UnityWebRequest.kHttpVerbGET))
                {
                    DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
                    uwr.downloadHandler = dH;

                    yield return uwr.SendWebRequest();
                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning(uwr.error + " for annotation retrieval!");
                        init = true;
                    }
                    else
                    {
                        string response = uwr.downloadHandler.text;
                        Annotations remoteAnnotations = JsonUtility.FromJson<Annotations>(response);

                        if (remoteAnnotations != null && remoteAnnotations.annotations != null)
                        {
                            foreach (var t in remoteAnnotations.annotations)
                                CreateAnnotationButton(t);
                        }

                        init = true;
                    }
                }
            }
        }

        public IEnumerator UploadAnnotations()
        {
            if (!photonView.IsMine)
                yield return true;
            if (state.selectedServer.Length > 0 && state.selectedReplayFile.Length > 0)
            {
                string completeURL = state.selectedServer + "/annotations/" + state.selectedReplayFile;

                WWWForm form = new WWWForm();
                string json = JsonUtility.ToJson(annotations);
                string fileName = "Annotation.json";
                form.AddBinaryData("file", System.Text.Encoding.UTF8.GetBytes(json), fileName);

                UnityWebRequest www = UnityWebRequest.Post(completeURL, form);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    Debug.Log("Upload complete!");
                }
            }
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