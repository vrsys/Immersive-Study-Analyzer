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