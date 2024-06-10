using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    public class TransformRecorder : Recorder
    {
        [DllImport("RecordingPlugin")]
        private static extern bool RegisterObjectPath(int recorderId, int uuid, string path, int pathLength, float time);
        
        [DllImport("RecordingPlugin")]
        private static extern bool RegisterObjectComponents(int recorderId, int uuid, string componentString, int componentStringLength);
        
        [DllImport("RecordingPlugin")]
        private static extern bool RecordObjectAtTimestamp(int recorderId, string objectName, int objectNameLength, int uuid, float[] localMatrix, float timeStamp, int[] objectInformation);
        
        [DllImport("RecordingPlugin")]
        private static extern bool GetTransformAndInformationAtTime(int recorderId, string objectName, int objectNameLength, int uuid, float currentTime, IntPtr data, IntPtr objectInformation);
        
        [DllImport("RecordingPlugin")]
        private static extern int GetOriginalID(int recorder_id, string object_name, int object_name_length, int object_uuid);

        private bool replayHierarchyChanges = false;
        private bool _isRecorded = false;
        private int playbackFramerate = 60;
        private int _parent;
        private Transform _parentTransform;
        private Transform _currentParentTransform;
        private Transform _transform;
        private bool _active = false;
        private float _lastRecordTime;
        private bool _firstPreview = true;
        private bool _changedSincePreviewStart = false;
        private int _isMesh = -1;

        private int _presentInRecording = -1;
        
        private Vector3 _originalLocalPos;
        private Vector3 _originalLocalSca;
        private Quaternion _originalLocalRot;
        
        private Vector3 _lastLocalPos;
        private Vector3 _lastLocalSca;
        private Quaternion _lastLocalRot;
        
        private Vector3 _lastGlobalPos;
        private Vector3 _lastGlobalSca;
        private Quaternion _lastGlobalRot;
        
        private Vector3 _initalPreviewPos;
        private Vector3 _initialPreviewSca;
        private Quaternion _initialPreviewRot;
        
        private float[] _matrixDTO = new float[20];
        private int[] _infoDTO = new int[2];
        private float[] _positionsDTO = new float[4 * 300];

        private string _name = "";
        
        private AudioSource _source;
        private TrailRenderer _renderer;

        private bool originalIdFound;
        private int originalId;

        public override bool Record(float recordTime)
        {
            _transform = gameObject.transform;
            
            if (_name == "")
            {
                _name = Utils.GetObjectName(gameObject);
                _name = _name.Replace("[Rec]", "");
            }
            
            bool result = true;

            if(id == 99999)
                id = gameObject.GetInstanceID();

            int parentID = 0;
            if (_transform.parent != null)
                parentID = _transform.parent.gameObject.GetInstanceID();

            bool firstSeen = false;
            
            if (!_isRecorded)
            {
                _isRecorded = true;
                firstSeen = true;
                
                // TODO: change to recording only prefabs!
                string meshPath = " ";
                MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                if (meshFilter != null &&  meshFilter.sharedMesh != null)
                    meshPath = meshFilter.sharedMesh.name;

                RegisterObjectPath(controller.RecorderID, id, meshPath, meshPath.Length, recordTime);

                Component[] components = gameObject.GetComponents(typeof(Component));
                string componentString = "";
                string pattern = @"\(UnityEngine\.([^)]+)\)";
                Regex rg = new Regex(pattern);
                foreach (Component component in components)
                {
                    string c = component.ToString();
                    MatchCollection componentNames = rg.Matches(c);
                    for (int i = 0; i < componentNames.Count; i++)
                    {
                        string cs = componentNames[i].Value;
                        cs = cs.Replace("(UnityEngine.", "");
                        cs = cs.Replace(")", "");
                        componentString += cs + ",";
                    }
                }

                RegisterObjectComponents(controller.RecorderID, id, componentString, componentString.Length);
            }
            
            bool changeInActiveStatus = _active != gameObject.activeSelf;
            bool changeInParent = _parent != parentID;
            bool changeInTransform = !_lastGlobalPos.Equals(_transform.position) ||
                                     !_lastGlobalSca.Equals(_transform.lossyScale) ||
                                     !_lastGlobalRot.Equals(_transform.rotation);

            bool objectChanged = _transform.hasChanged || changeInActiveStatus || firstSeen ||
                                 changeInParent || changeInTransform;

            if (objectChanged)
            {
                bool teleportation = false;
                // This check is being done to avoid problems during jumps/teleportations when the object does not move
                // often and then suddenly does a huge "jump". The reason for this is that it can produce artifacts during
                // the replay when the interpolation is being done between two transforms which have a huge time stamp difference
                if (changeInTransform)
                {
                    float posDif = (_lastLocalPos - _transform.localPosition).magnitude;
                    float scaDif = (_lastLocalSca - _transform.localScale).magnitude;

                    float timeDif = _lastRecordTime - recordTime;

                    if ((timeDif > 1.1f / (float) controller.transformRecordingStepsPerSecond || (posDif > 0.5f || scaDif > 0.3f)) && recordTime > 0.001f)
                    {
                        teleportation = true;
                        Debug.Log("Teleportation detected? Pos Dif: " + posDif + ", Scale Dif: " + scaDif + ", Time Dif: " + timeDif + ", Time: " + recordTime + ", uuid: " + id);
                    }
                }

             
                if (teleportation)
                {
                    fillDTOLastData();

                    float teleportTime = recordTime - (1.0f / 1000.0f);
                    if (teleportTime > 0.0f)
                    {
                        result = RecordObjectAtTimestamp(controller.RecorderID, _name, _name.Length, id, _matrixDTO, teleportTime, _infoDTO);
                        
                        if (!result)
                            Debug.LogError("Recording teleportation object: Failed, " + gameObject.name);
                    }
                }

                fillDTOCurrentData();
                result = RecordObjectAtTimestamp(controller.RecorderID, _name, _name.Length, id, _matrixDTO, recordTime, _infoDTO);

                //Debug.Log("RecordTime: " + recordTime);
                
                if (!result)
                {
                    Debug.LogError("Recording object: " + _name + " Failed");
                }
                else
                {
                    _transform.hasChanged = false;
                    
                    _lastRecordTime = recordTime;
                }
            }

            return result;
        }

        unsafe public override bool Replay(float replayTime)
        {
            if (Mathf.Abs(replayTime - _lastReplayTime) < 1.0f/playbackFramerate && _transform != null)
            {
                _transform.localScale = _lastLocalSca;
                _transform.localRotation = _lastLocalRot;
                _transform.localPosition = _lastLocalPos; 
                
                return true;
            }
            
            if (preview)
                return true;
            
            if(_transform == null)
                _transform = gameObject.transform;
            
            if (_name == "")
            {
                _name = Utils.GetObjectName(gameObject).Replace("[Rec]", "");
                if (_name.Contains("TimePortalNodes"))
                {
                    _name = _name.Substring(_name.IndexOf("TimePortalNodes") + "TimePortalNodes".Length);
                }
                _originalLocalPos = _transform.localPosition;
                _originalLocalRot = _transform.localRotation;
                _originalLocalSca = _transform.localScale;
            }

            if (_presentInRecording == -1)
                _presentInRecording = controller.RecordedObjectPresent.ContainsKey(_name) ? 1 : 0;

            if (_presentInRecording == 0)
                return true;

            if(id == 99999)
                id = gameObject.GetInstanceID();

            if (recorderId == 99999)
                recorderId = controller.RecorderID;
            
            _currentParentTransform = _transform.parent;

            if (replayHierarchyChanges)
            {
                if (_currentParentTransform != null && _currentParentTransform != _parentTransform)
                {
                    _parent = _currentParentTransform.gameObject.GetInstanceID();
                    _parentTransform = _currentParentTransform;
                }
                else if (_currentParentTransform == null)
                    _parent = 0;
            }

            fixed (float* p = _matrixDTO)
            {
                fixed (int* u = _infoDTO)
                {
                    bool result = GetTransformAndInformationAtTime(recorderId, _name, _name.Length, id, replayTime, (IntPtr) p, (IntPtr) u);
                    
                    if (!result)
                        Debug.LogError("Could not get replay transform for: " + _name);
                    else
                    {
                        if (!originalIdFound)
                        {
                            originalId = GetOriginalID(controller.RecorderID, _name, _name.Length, id);
                            originalIdFound = true;
                            controller.AddOriginalIdGameobject(originalId, id, gameObject);
                        }

                        _lastLocalPos.x = _matrixDTO[0]; _lastLocalPos.y = _matrixDTO[1]; _lastLocalPos.z = _matrixDTO[2];
                        _lastLocalRot.x = _matrixDTO[4]; _lastLocalRot.y = _matrixDTO[5]; _lastLocalRot.z = _matrixDTO[6]; _lastLocalRot.w = _matrixDTO[3];
                        _lastLocalSca.x = _matrixDTO[7]; _lastLocalSca.y = _matrixDTO[8]; _lastLocalSca.z = _matrixDTO[9];
                        bool active = _infoDTO[0] > 0;
                        int newParentUUID = _infoDTO[1];

                        if(!_transform.localScale.Equals(_lastLocalSca))
                            _transform.localScale = _lastLocalSca;
                        if(!_transform.localRotation.Equals(_lastLocalRot))
                            _transform.localRotation = _lastLocalRot;
                        if(!_transform.localPosition.Equals(_lastLocalPos))
                            _transform.localPosition = _lastLocalPos;

                        if (gameObject.activeSelf != active)
                            gameObject.SetActive(active);

                        // TODO: fix parent for portal objects
                        if (replayHierarchyChanges)
                        {
                            if (newParentUUID != -99999 && _parent != newParentUUID && !portal)
                            {
                                if (controller.GetTransformRecorder(newParentUUID) != null)
                                {
                                    Transform parentTransform =
                                        controller.GetTransformRecorder(newParentUUID).transform;
                                    if (newParentUUID != 0 && parentTransform != null)
                                    {
                                        _transform.SetParent(parentTransform);
                                        _parentTransform = parentTransform;
                                        _parent = newParentUUID;
                                    }
                                    else
                                    {
                                        _transform.parent = null;
                                        _parent = 0;
                                    }
                                }

                                if (newParentUUID == 0)
                                {
                                    _transform.parent = null;
                                    _parent = 0;
                                }
                            }
                        }
                    }

                    _lastReplayTime = replayTime;
                    return result;
                }
            }
        }

        unsafe public override bool Preview(float previewTime)
        {
            _transform = gameObject.transform;

            if (!preview && _presentInRecording == 1)
            {
                _transform.localPosition = _lastLocalPos;
                _transform.localRotation = _lastLocalRot;
                _transform.localScale = _lastLocalSca;
                return true;
            }

            if (_firstPreview)
            {
                _initalPreviewPos = _transform.position;
                _initialPreviewRot = _transform.rotation;
                _initialPreviewSca = _transform.lossyScale;
                _firstPreview = false;
            }

            if (_name == "")
            {
                _name = Utils.GetObjectName(gameObject).Replace("[Rec]", "");
                _name = _name.Replace("/__RECORDING__/RecordingSetup/PreviewNodes", "");
                _originalLocalPos = _transform.localPosition;
                _originalLocalRot = _transform.localRotation;
                _originalLocalSca = _transform.localScale;
            }

            if (_presentInRecording == -1)
                _presentInRecording = controller.RecordedObjectPresent.ContainsKey(_name) ? 1 : 0;

            if (_presentInRecording == 0)
                return true;

            if (id == 99999)
                id = gameObject.GetInstanceID();

            _currentParentTransform = _transform.parent;

            if (_currentParentTransform != null && _currentParentTransform != _parentTransform)
            {
                _parent = _currentParentTransform.gameObject.GetInstanceID();
                _parentTransform = _currentParentTransform;
            }
            else if (_currentParentTransform == null)
                _parent = 0;

            fixed (float* p = _matrixDTO)
            {
                fixed (int* u = _infoDTO)
                {
                    bool result = GetTransformAndInformationAtTime(controller.RecorderID, _name, _name.Length, id,
                        previewTime, (IntPtr)p, (IntPtr)u);

                    if (!result)
                        Debug.LogError("Could not get replay transform for: " + _name);
                    else
                    {
                        _lastLocalPos.x = _matrixDTO[0]; _lastLocalPos.y = _matrixDTO[1]; _lastLocalPos.z = _matrixDTO[2];
                        _lastLocalRot.x = _matrixDTO[4]; _lastLocalRot.y = _matrixDTO[5]; _lastLocalRot.z = _matrixDTO[6]; _lastLocalRot.w = _matrixDTO[3];
                        _lastLocalSca.x = _matrixDTO[7]; _lastLocalSca.y = _matrixDTO[8]; _lastLocalSca.z = _matrixDTO[9];
                        bool active = _infoDTO[0] > 0;
                        int newParentUUID = _infoDTO[1];

                        _transform.localScale = _lastLocalSca;
                        _transform.localRotation = _lastLocalRot;
                        _transform.localPosition = _lastLocalPos;

                        if (gameObject.activeSelf != active)
                            gameObject.SetActive(active);

                        if (newParentUUID != -99999 && _parent != newParentUUID)
                        {
                            if (controller.GetTransformRecorder(newParentUUID) != null)
                            {
                                Transform parentTransform = controller.GetTransformRecorder(newParentUUID).transform;
                                if (newParentUUID != 0 && parentTransform != null)
                                {
                                    //transform.SetParent(parentTransform);
                                    //parent = newParentUUID;
                                }
                                else
                                {
                                    //transform.parent = null;
                                    //parent = 0;
                                }
                            }

                            if (newParentUUID == 0)
                            {
                                //transform.parent = null;
                                //parent = 0;
                            }
                        }

                        if (!_changedSincePreviewStart)
                        {
                            Vector3 diffS = _initialPreviewSca - _transform.lossyScale;
                            Vector3 diffP = _initalPreviewPos - _transform.position;
                            Vector3 diffR = _initialPreviewRot.eulerAngles - _transform.rotation.eulerAngles;
                            if (diffS.magnitude > 0.001f || diffP.magnitude > 0.001f || diffR.magnitude > 0.001f)
                                _changedSincePreviewStart = true;
                        }

                        if (_isMesh == -1)
                            _isMesh = gameObject.GetComponent<MeshRenderer>() != null || gameObject.GetComponent<SkinnedMeshRenderer>() != null ? 1 : 0;
                        
                        if (_changedSincePreviewStart && gameObject.activeSelf)
                        {
                            if (_isMesh == 1)
                            {
                                Outline outline = gameObject.AddComponent<Outline>();
                                outline.OutlineMode = Outline.Mode.OutlineAll;
                                outline.OutlineColor = Color.yellow;
                                outline.OutlineWidth = 5.0f;

                                _source = gameObject.AddComponent<AudioSource>();
                                _renderer = gameObject.AddComponent<TrailRenderer>();

                                _renderer.numCapVertices = 5;
                                _renderer.numCornerVertices = 5;
                                _renderer.material = _trailMaterial;
                                _renderer.time = 1.0f;
                                _renderer.endWidth = 0.1f;
                                _renderer.startWidth = 0.5f;

                                _source.clip = _soundEffect;
                                _source.loop = true;
                                _source.spatialize = true;
                                _source.spatialBlend = 1.0f;
                                _source.volume = 0.1f;
                                _source.spatialBlend = 1.0f;
                                _source.maxDistance = 20.0f;
                                _source.pitch = UnityEngine.Random.Range(0.0f, 2.0f);

                                _source.Play();
                                _isMesh = 2;
                            }

                            if (_isMesh == 2 && controller.LocalUserHead != null)
                            {
                                float dist = (controller.LocalUserHead.transform.position - _transform.position)
                                    .magnitude;
                                float trailWidthInAngle = 1.0f * (float)Math.PI / 180.0f;
                                // simple geometry using a isosceles triangle
                                float width = (float)Math.Abs((Math.Tan(trailWidthInAngle / 2.0f)) * dist * 2);
                                ;
                                _renderer.endWidth = 0.2f * width;
                                _renderer.startWidth = width;
                            }
                        }
                    }

                    return result;
                }
            }
        }

        bool fillDTOCurrentData()
        {
            _transform = gameObject.transform;
            
            _lastLocalPos = _transform.localPosition;
            _lastLocalSca = _transform.localScale;
            _lastLocalRot = _transform.localRotation;
            _lastGlobalPos = _transform.position;
            _lastGlobalSca = _transform.lossyScale;
            _lastGlobalRot = _transform.rotation;
            _active = gameObject.activeSelf;
            _parent = _transform.parent != null ? _transform.parent.GetInstanceID() : 0;

            fillDTOLastData();
            return true;
        }
        
        bool fillDTOLastData()
        {
            _matrixDTO[0] = _lastLocalPos.x;
            _matrixDTO[1] = _lastLocalPos.y;
            _matrixDTO[2] = _lastLocalPos.z;

            _matrixDTO[3] = _lastLocalRot.w;
            _matrixDTO[4] = _lastLocalRot.x;
            _matrixDTO[5] = _lastLocalRot.y;
            _matrixDTO[6] = _lastLocalRot.z;

            _matrixDTO[7] = _lastLocalSca.x;
            _matrixDTO[8] = _lastLocalSca.y;
            _matrixDTO[9] = _lastLocalSca.z;

            _matrixDTO[10] = _lastGlobalPos.x;
            _matrixDTO[11] = _lastGlobalPos.y;
            _matrixDTO[12] = _lastGlobalPos.z;

            _matrixDTO[13] = _lastGlobalRot.w;
            _matrixDTO[14] = _lastGlobalRot.x;
            _matrixDTO[15] = _lastGlobalRot.y;
            _matrixDTO[16] = _lastGlobalRot.z;

            _matrixDTO[17] = _lastGlobalSca.x;
            _matrixDTO[18] = _lastGlobalSca.y;
            _matrixDTO[19] = _lastGlobalSca.z;
            
            _infoDTO[0] = _active ? 1 : -1;
            _infoDTO[1] = _parent;
            return true;
        }

        private void OnDestroy()
        {
            if (_presentInRecording == 1)
            {
                gameObject.transform.localPosition = _originalLocalPos;
                gameObject.transform.localRotation = _originalLocalRot;
                gameObject.transform.localScale = _originalLocalSca;
            }
        }
    }
}