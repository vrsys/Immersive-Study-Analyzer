using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vrsys;
using VRSYS.Recording.Scripts;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    public class ScenePreparator
    {
        [DllImport("RecordingPlugin")]
        private static extern int GetRecordingGameObjects(int recorderId, StringBuilder textBuilder, int maxSize);

        [DllImport("RecordingPlugin")]
        private static extern int GetGameObjectPath(int recorderId, StringBuilder textBuilder, int maxSize, string name,
            int nameLength);

        [DllImport("RecordingPlugin")]
        private static extern int GetGameObjectComponents(int recorderId, StringBuilder textBuilder, int maxSize,
            string name, int nameLength);


        private RecorderController _controller;
        private List<GameObject> _replayGameObjects = new List<GameObject>();
        private string[] _replayGameObjectNames;
        private Dictionary<string, bool> _namePresent = new Dictionary<string, bool>();

        private GameObject _hmdUserPrefab;
        private GameObject _hmdExternalPrefab;
        private GameObject _desktopUserPrefab;
        private GameObject _desktopExternalPrefab;

        private Dictionary<string, GameObject> _selectableUserPerspectives = new Dictionary<string, GameObject>();

        public ScenePreparator(RecorderController controller, GameObject desktopExternalPrefab,
            GameObject desktopUserPrefab, GameObject hmdExternalPrefab, GameObject hmdUserPrefab)
        {
            _controller = controller;
            _desktopExternalPrefab = desktopExternalPrefab;
            _desktopUserPrefab = desktopUserPrefab;
            _hmdExternalPrefab = hmdExternalPrefab;
            _hmdUserPrefab = hmdUserPrefab;
        }

        private bool HandleMissingGameObject()
        {
            for (int i = 0; i < _replayGameObjectNames.Length; i++)
            {
                if (_replayGameObjectNames[i] != "")
                {
                    _namePresent.Add(_replayGameObjectNames[i], false);
                }
            }

            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                SceneGraphTraversalGameObjectExistenceCheck(rootObject, "");
            }

            if (DontDestroySceneAccessor.Instance != null)
            {
                rootObjects = DontDestroySceneAccessor.Instance.GetAllRootsOfDontDestroyOnLoad();
                foreach (var rootObject in rootObjects)
                {
                    SceneGraphTraversalGameObjectExistenceCheck(rootObject, "");
                }
            }

            Debug.Log("Scene Graph existence check finished.");

            bool error = false;

            int maxSize = 300;
            StringBuilder pathBuffer = new StringBuilder(maxSize);
            StringBuilder componentBuffer = new StringBuilder(maxSize);

            TextAsset prefabListAsset = Resources.Load<TextAsset>("PrefabPathList");

            FileNameInfo prefabInfo = null;

            if (prefabListAsset != null)
                prefabInfo = JsonUtility.FromJson<FileNameInfo>(prefabListAsset.text);

            foreach (var gameObject in _namePresent)
            {
                string hierarchyName = gameObject.Key;

                // check if 
                if (gameObject.Value == false)
                {
                    //Debug.Log("GameObject: " + hierarchyName + " present in recording but not present in current scene graph!");
                    error = true;

                    int len = GetGameObjectPath(_controller.RecorderID, pathBuffer, maxSize, hierarchyName,
                        hierarchyName.Length);
                    String gameObjectPath = pathBuffer.ToString().Substring(0, len);
                    //len = GetGameObjectComponents(_controller.RecorderID, componentBuffer, maxSize, hierarchyName,hierarchyName.Length);
                    //String gameObjectComponents = componentBuffer.ToString().Substring(0, len);

                    //Debug.Log(hierarchyName + ": " + gameObjectPath);
                    int index = hierarchyName.IndexOf("/");
                    hierarchyName = (index < 0) ? hierarchyName : hierarchyName.Remove(index, 1);

                    string[] hierarchy = hierarchyName.Split('/');

                    for (int i = 0; i < hierarchy.Length; i++)
                    {
                        hierarchy[i] = hierarchy[i].Replace("/", "");
                        //Debug.Log(hierarchy[i]);
                    }

                    GameObject go = null;
                    if (hierarchy.Length > 0)
                    {
                        //go = Utils.GetOrCreateHierarchy(hierarchy, 0, null);
                    }

                    string name = hierarchy[hierarchy.Length - 1];


                    if (name.Contains("Jumping Avatar Preview"))
                    {
                        Material previewMaterial = MaterialsFactory.CreatePreviewMaterial();
                        Material fadeMaterial = MaterialsFactory.CreateFadeMaterial();
                        int pFrom = name.IndexOf("['") + "['".Length;
                        int pTo = name.LastIndexOf("']");

                        name = name.Substring(pFrom, pTo - pFrom);

                        GameObject jumpingPositionPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        jumpingPositionPreview.transform.localScale = new Vector3(1f, 0.02f, 1f);
                        jumpingPositionPreview.name = "Jumping Position Preview ['" + name + "'][Rec]";
                        ;
                        jumpingPositionPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
                        jumpingPositionPreview.GetComponentInChildren<MeshRenderer>().material = fadeMaterial;
                        jumpingPositionPreview.SetActive(false); // hide

                        GameObject jumpingCirclePrefab = Resources.Load<GameObject>("Navigation Assets/Jump-Circle");
                        GameObject jumpingCircleStartPreview =
                            GameObject.Instantiate(jumpingCirclePrefab, Vector3.zero,
                                Quaternion.identity) as GameObject;
                        jumpingCircleStartPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
                        jumpingCircleStartPreview.name = "Jumping Position Start Circle Preview ['" + name + "'][Rec]";
                        ;
                        jumpingCircleStartPreview.transform.localScale = Vector3.one;
                        jumpingCircleStartPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
                        jumpingCircleStartPreview.SetActive(false); // hide

                        GameObject jumpingArrowRingPrefab =
                            Resources.Load<GameObject>("Navigation Assets/Jump-ArrowRing");
                        GameObject jumpingCircleFullPreview =
                            GameObject.Instantiate(jumpingArrowRingPrefab, Vector3.zero, Quaternion.identity) as
                                GameObject;
                        jumpingCircleFullPreview.GetComponentInChildren<MeshRenderer>().material = previewMaterial;
                        jumpingCircleFullPreview.name = "Jumping Position Full Circle Preview ['" + name + "'][Rec]";
                        ;
                        jumpingCircleFullPreview.transform.localScale = Vector3.one;
                        jumpingCircleFullPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
                        jumpingCircleFullPreview.SetActive(false); // hide

                        GameObject avatarPrefab = Resources.Load<GameObject>("Navigation Assets/Jump-Avatar");
                        GameObject jumpingAvatarPreview =
                            GameObject.Instantiate(avatarPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                        jumpingAvatarPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
                        jumpingAvatarPreview.name = "Jumping Avatar Preview ['" + name + "'][Rec]";
                        ;
                        jumpingAvatarPreview.SetActive(false);
                    }

                    if (gameObjectPath != " " && gameObjectPath != "" && prefabInfo != null)
                    {
                        List<string> prefabCandidates = new List<string>();
                        foreach (string prefabPath in prefabInfo.fileNames)
                        {
                            //if (prefabPath.Contains(gameObjectPath) && prefabPath.Contains(".prefab"))
                            if (prefabPath.Contains(gameObjectPath))
                            {
                                string p = prefabPath;
                                int i = p.LastIndexOf('.');
                                p = i == -1 ? p : p.Substring(0, i);
                                p = p.Replace("\\", "/");
                                prefabCandidates.Add(p);
                            }
                        }

                        // TODO: what happens if there are more than one resource folder and a substructure of folders exists?
                        GameObject prefab;
                        if (prefabCandidates.Count > 0)
                        {
                            Debug.Log("Trying to instantiate missing object: " + prefabCandidates[0]);
                            prefab = Resources.Load<GameObject>(prefabCandidates[0]);
                        }
                        else
                        {
                            Debug.Log("Trying to instantiate missing object: " + gameObjectPath);
                            prefab = Resources.Load<GameObject>(gameObjectPath);
                        }

                        if (prefab != null)
                        {
                            GameObject parent = null;
                            if (go != null && go.transform.parent != null)
                            {
                                parent = go.transform.parent.gameObject;
                            }

                            go = UnityEngine.Object.Instantiate(prefab);
                            go.SetActive(false);
                            go.name = name;
                            if (parent != null)
                            {
                                go.transform.SetParent(parent.transform);
                            }

                            _replayGameObjects.Add(go);
                        }
                        else
                        {
                            //if (prefabCandidates.Count > 0)
                            //{
                            //    Debug.LogError("Could not instantiate gameobject with name: " + name +
                            //                   " and prefab path: " + prefabCandidates[0]);
                            //}
                            //else
                            //{
                            //    Debug.LogError("Could not instantiate gameobject with name: " + name +
                            //                   " and prefab path: " + gameObjectPath);
                            //}
                        }
                    }
                }
            }

            return error;
        }

        public Dictionary<string, GameObject> GetSelectableUserPerspectives()
        {
            return _selectableUserPerspectives;
        }

        public Dictionary<string, bool> GetNamePresent()
        {
            return _namePresent;
        }

        private bool HandleReplayAvatars()
        {
            if (!_selectableUserPerspectives.ContainsKey("Local User"))
                _selectableUserPerspectives.Add("Local User", NetworkUser.localNetworkUser.gameObject);

            Dictionary<string, bool> isHmdAvatar = new Dictionary<string, bool>();

            string userPattern = @"\/(.)+(\[External User\]|\[Local User\])\/";

            for (int i = 0; i < _replayGameObjectNames.Length; i++)
            {
                Regex rx = new Regex(userPattern);
                Match m = rx.Match(_replayGameObjectNames[i]);
                if (m.Success)
                {
                    string userName = m.Value.Replace("/", "");
                    if (!isHmdAvatar.ContainsKey(userName))
                    {
                        isHmdAvatar.Add(userName, false);
                    }

                    if (_replayGameObjectNames[i].Contains("HMDMesh"))
                    {
                        isHmdAvatar[userName] = true;
                    }
                }
            }

            Color[] colors = new[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan };

            int index = 0;
            foreach (var avatar in isHmdAvatar)
            {
                GameObject g = null;
                // if is HMD avatar

                if (avatar.Value)
                {
                    if (avatar.Key.Contains("Local"))
                    {
                        g = GameObject.Instantiate(_hmdUserPrefab);
                        _controller.LocalRecordedUserHead = Utils.GetChildBySubstring(g, "HeadModel");
                        g.transform.parent = null;
                    }
                    else
                    {
                        g = GameObject.Instantiate(_hmdExternalPrefab);
                        _controller.ExternalRecordedUserHead = Utils.GetChildBySubstring(g, "HeadModel");
                        g.transform.parent = null;
                    }
                }
                else
                {
                    if (avatar.Key.Contains("Local"))
                    {
                        g = GameObject.Instantiate(_desktopUserPrefab);
                        g.transform.parent = null;
                    }
                    else
                    {
                        g = GameObject.Instantiate(_desktopExternalPrefab);
                        g.transform.parent = null;
                    }
                }

                GameObject userName = Utils.GetChildByName(g, "Text (TMP)");
                if (userName != null)
                {
                    TextMeshProUGUI t = userName.GetComponent<TextMeshProUGUI>();
                    if (t != null)
                    {
                        //t.text = avatar.Key.Replace("[Local User]", "").Replace("[External User]", "");
                        if (avatar.Key.Contains("[Local User]"))
                            t.text = "Adam";
                        else
                            t.text = "Bob";
                    }
                }

                g.name = avatar.Key + "[Rec]";

                GameObject canvasGO = Utils.GetChildBySubstring(g, "UserInfoCanvas");
                if (canvasGO != null)
                    canvasGO.GetComponent<Canvas>().enabled = true;

                //if (!g.name.Contains("Guide"))
                //{
                //    GameObject shirt = Utils.GetChildBySubstring(g, "default");
                //    var renderer = shirt.GetComponent<Renderer>();
                //    if (renderer != null)
                //        renderer.material.color = colors[index];
                //    index++;
                //}

                if (_selectableUserPerspectives.ContainsKey(avatar.Key))
                {
                    GameObject.Destroy(_selectableUserPerspectives[avatar.Key]);
                    _selectableUserPerspectives[avatar.Key] = g;
                }
                else
                {
                    _selectableUserPerspectives.Add(avatar.Key, g);
                }
            }

            return false;
        }

        public void PrepareReplayScene()
        {
            _namePresent.Clear();

            // TODO: handle the case when the scene contains so many objects that a name scene representation is not 
            //       possible with 500000 characters
            int maxSize = 500000;
            StringBuilder buffer = new StringBuilder(maxSize);
            Debug.Log("Trying to get recording gameobjects");
            int len = GetRecordingGameObjects(_controller.RecorderID, buffer, buffer.Capacity);
            if (len > 0)
            {
                Debug.Log("Received a string of length: " + len);
                String gameObjectString = buffer.ToString().Substring(0, len);
                _replayGameObjectNames = gameObjectString.Split(';');

                bool error = HandleReplayAvatars();
                if (error)
                {
                    Debug.LogError("Error! Replay avatars could not be created!");
                }

                error = HandleMissingGameObject();


                if (!error)
                {
                    Debug.Log("All gameObjects from the recording are present in the current scene graph!");
                }
                else
                {
                    Debug.LogError("Not all gameObjects from the recording are present in the current scene graph!");
                }
            }
            else
            {
                Debug.LogError(
                    "Could not get the required data from the plugin! Potential reason: string builder capacity not large enough! Needed size: " +
                    (-len));
            }
        }

        private void SceneGraphTraversalGameObjectExistenceCheck(GameObject currentGameObj, string name)
        {
            // Note: this is being done because the name of a local user can change during sessions
            name += "/" + currentGameObj.name.Replace("[Rec]", "");

            foreach (Transform childTransform in currentGameObj.transform)
            {
                SceneGraphTraversalGameObjectExistenceCheck(childTransform.gameObject, name);
            }


            if (_namePresent.ContainsKey(name))
            {
                _namePresent[name] = true;
            }
            else
            {
                if (!name.Contains("RecordingSetup"))
                {
                    //Debug.Log("GameObject: " + name + " not existent in recording! Thus it will not be animated...");
                }
            }
        }
    }
}