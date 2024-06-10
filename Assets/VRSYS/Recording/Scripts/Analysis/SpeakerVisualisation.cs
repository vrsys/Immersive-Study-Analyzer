using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using Vrsys.Scripts.Recording;
using VRSYS.Scripts.Recording;
using State = VRSYS.Scripts.Recording.State;

namespace VRSYS.Recording.Scripts.Analysis
{
    [RequireComponent(typeof(RecorderController))]
    public class SpeakerVisualisation : MonoBehaviourPun
    {
        public GameObject VisualisationPrefab;
        [Range(0,1)]
        public float threshold = 0.01f;
        private RecorderController _controller;
        private bool _init;
        private Dictionary<int, Recorder> _audioRecorder;
        private Dictionary<int, AmplitudeMeasurement> _amplitudeMeasurements;
        private Dictionary<int, GameObject> _userGameObjects;

        public void Start()
        {
            _controller = GetComponent<RecorderController>();
            _amplitudeMeasurements = new Dictionary<int, AmplitudeMeasurement>();
            _userGameObjects = new Dictionary<int, GameObject>();
        }

        public void Update()
        {
            if(!photonView.IsMine)
                return;
            
            if (_controller.CurrentState == State.Replaying)
            {
                if (!_init)
                {
                    _audioRecorder = _controller.GetAudioRecorder();

                    _init = true;
                    
                    foreach (var kv in _audioRecorder)
                    {
                        if (kv.Value is AudioRecorder)
                        {
                            AudioRecorder audioRecorder = (AudioRecorder)kv.Value;
                            AudioSource source = audioRecorder.GetAudioSource();
                            if (source != null)
                            {
                                AmplitudeMeasurement measurement =
                                    source.gameObject.AddComponent<AmplitudeMeasurement>();
                                _amplitudeMeasurements.Add(kv.Key, measurement);
                            }
                            else
                                _init = false;

                            if (!_userGameObjects.ContainsKey(kv.Key))
                            {
                                GameObject user = null;
                                if (kv.Key == 0)
                                    user = Utils.GetGameObjectBySubstring("[Local User][Rec]");
                                else if (kv.Key == 1)
                                    user = Utils.GetGameObjectBySubstring("[External User][Rec]");

                                if (user != null)
                                {
                                    GameObject head = Utils.GetChildByName(user, "HeadMesh");
                                    if (head != null)
                                    {
                                        GameObject soundVisualisation = Instantiate(VisualisationPrefab);
                                        soundVisualisation.transform.SetParent(head.transform, false);
                                        soundVisualisation.SetActive(false);
                                        _userGameObjects.Add(kv.Key, soundVisualisation);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (_amplitudeMeasurements != null)
                    {
                        foreach (var kv in _amplitudeMeasurements)
                        {
                            AmplitudeMeasurement measurement = kv.Value;
                            if (measurement.peakAmplitude > threshold)
                            {
                                if (_userGameObjects.ContainsKey(kv.Key))
                                    _userGameObjects[kv.Key].SetActive(true);
                            }
                            else
                            {
                                if (_userGameObjects.ContainsKey(kv.Key))
                                    _userGameObjects[kv.Key].SetActive(false);
                            }
                        }
                    }
                }
            }
            else
            {
                if (_init)
                {
                    foreach (var kv in _userGameObjects)
                    {
                        Destroy(kv.Value);
                    }

                    _userGameObjects.Clear();

                    if (_amplitudeMeasurements != null)
                    {
                        foreach (var kv in _amplitudeMeasurements)
                        {
                            Destroy(kv.Value);
                        }

                        _amplitudeMeasurements.Clear();
                    }
                }

                _init = false;
            }
        }
    }
}