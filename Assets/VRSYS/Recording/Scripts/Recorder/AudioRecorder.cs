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
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    enum SpatializationMode {
        NoSpatialization, BasicSpatialization, BinauralSpatialization
    }
    public class AudioRecorder : Recorder
    {
        [DllImport("RecordingPlugin")]
        protected static extern bool RecordSoundDataAtTimestamp(int recorderId, float[] soundData, int soundDataLength,
            int samplingRate, int startIndex, int channelNum, float timeStamp, int soundOrigin);

        [DllImport("RecordingPlugin")]
        private static extern int GetSoundChunkForTime(int recorderId, int soundOrigin, float timeStamp,
            IntPtr soundData);

        [DllImport("RecordingPlugin")]
        private static extern int GetSamplingRate(int recorderId, int soundOrigin);

        [DllImport("RecordingPlugin")]
        private static extern int GetChannelNum(int recorderId, int soundOrigin);

        [DllImport("RecordingPlugin")]
        private static extern bool OpenExistingRecordingFile(int recorderId, string recordingDir, int recordingDirNameLength, string recordingName, int recordingNameLength);
        
        [DllImport("RecordingPlugin")]
        private static extern bool StopReplay(int recorderId);
        
        [DllImport("RecordingPlugin")]
        private static extern float GetRecordingStartGlobalTimeOffset(int recorderId);
        
        private AudioClip _clip;
        private AudioSource _source;
        private Transform _targetTransform;
        private SpatializationMode _mode = SpatializationMode.NoSpatialization;
        private float globalRecordingStartOffset = 0.0f;
        
        protected int RecordingSamplingRate = -1;
        protected int RecordingChannelNum = -1;
        protected int SoundRecordingStepsPerSecond = 10;
        protected float RecordingTime = -1.0f;
        protected bool FirstRecord = false;

        private int _playbackSamplingRate = -1;
        private int _playbackChannelNum = -1;

        private float[][] _replayAudioData;
        private bool _initializedReplay = false;
        private float _nextSoundReplayTime;
        private int _audioWritePos;
        private float[] _soundDTO = new float[4800];
        private float[] _emptySound = new float[1024];

        public void Start()
        {
            base.Start();

            _replayAudioData = new float[6][];
            _replayAudioData[0] = new float[1000];
            _replayAudioData[1] = new float[512];
            _replayAudioData[2] = new float[1024];
            _replayAudioData[3] = new float[2048];
            _replayAudioData[4] = new float[4096];
            _replayAudioData[5] = new float[4800];
        }

        private void OnDestroy()
        {
            base.OnDestroy();

            if (_source != null)
                Destroy(_source);

            if (_mode == SpatializationMode.BinauralSpatialization && !portal)
            {
                StopReplay(1);
            }
        }

        protected void InitializeReplayData()
        {
            if (id == 0 && controller.LocalRecordedUserHead != null)
                _targetTransform = controller.LocalRecordedUserHead.transform;
            else if (id == 1 && controller.ExternalRecordedUserHead != null)
                _targetTransform = controller.ExternalRecordedUserHead.transform;

            int recorderId = controller.RecorderID;
            
            if (_mode == SpatializationMode.BinauralSpatialization)
            {
                recorderId = 1;
                StopReplay(recorderId);
                bool result = OpenExistingRecordingFile(recorderId, controller.RecordingDirectory, controller.RecordingDirectory.Length, controller.PartnerReplayFile, controller.PartnerReplayFile.Length);

                
                if(!result)
                    Debug.LogError("Error!");
                _playbackSamplingRate = GetSamplingRate(recorderId, 0);
                //_playbackSamplingRate = 48000;
                _playbackChannelNum = GetChannelNum(recorderId, 0);

                float offset_base = GetRecordingStartGlobalTimeOffset(0);
                float offset_partner = GetRecordingStartGlobalTimeOffset(recorderId);

                // conversion from milliseconds to seconds
                globalRecordingStartOffset = (offset_base - offset_partner)/1000.0f;
            } else
            {
                _playbackSamplingRate = GetSamplingRate(recorderId, id);
                //_playbackSamplingRate = 48000;
                _playbackChannelNum = GetChannelNum(recorderId, id);
            }
            
            
      

            Debug.Log("Playback sampling rate: " + _playbackSamplingRate + ", Playback channel num: " +
                      _playbackChannelNum);

            if (_playbackSamplingRate < 0)
            {
                _playbackSamplingRate = 1000;
                _playbackChannelNum = 1;
            }

            _clip = AudioClip.Create("AudioReplay" + id, _playbackSamplingRate * 10, _playbackChannelNum, _playbackSamplingRate, false);
            if (_source == null)
            {
                string name = "AudioSource: " + id;
                if (portal)
                    name = "Portal" + name;
                
                GameObject audioSourceGo = new GameObject(name);
                audioSourceGo.transform.parent = controller.gameObject.transform;
                _source = audioSourceGo.AddComponent<AudioSource>();
            }

            _source.clip = _clip;
            _source.loop = true;
            _initializedReplay = true;

            if (_mode != SpatializationMode.NoSpatialization)
            {
                _source.spatialBlend = 1f;
                _source.spatialize = true;
                _source.volume = 1f;
            }

            SoundRecordingStepsPerSecond = _playbackSamplingRate / 1024;
        }

        unsafe public override bool Replay(float replayTime)
        {
            if (!_initializedReplay)
                InitializeReplayData();

            if (replayTime - _lastReplayTime == 0.0f)
                _source.Pause();
            else if (!_source.isPlaying)
                _source.Play();

            if (Mathf.Abs(replayTime - _lastReplayTime) >= 0.3f)
            {
                _audioWritePos = (int)((_source.time / _clip.length) * _clip.samples);
                if (_audioWritePos % _playbackChannelNum != 0)
                    _audioWritePos -= _audioWritePos % _playbackChannelNum;
                _nextSoundReplayTime = replayTime;
            }

            bool newData = false;
            if (replayTime >= _nextSoundReplayTime - 1.0f)
            {
                float loadTime = _nextSoundReplayTime;

                fixed (float* p = _soundDTO)
                {
                    // load the audio data for the next 3 seconds and insert it into the audio clip
                    for (int i = 0; i < SoundRecordingStepsPerSecond; i++)
                    {
                        int result = -1;

                        if (loadTime < controller.GetRecordingDuration())
                        {
                            if (_mode == SpatializationMode.BinauralSpatialization)
                            {
                                if(id == 0)
                                    result = GetSoundChunkForTime(0, 0, loadTime, (IntPtr)p);
                                else if(id == 1)
                                    result = GetSoundChunkForTime(1, 0, loadTime + globalRecordingStartOffset, (IntPtr)p);
                            }
                            else
                            {
                                result = GetSoundChunkForTime(controller.RecorderID, id, loadTime, (IntPtr)p);
                            }

                            if (result < 1)
                            {
                                Debug.LogWarning(
                                    "Could not get new sound data! Reason: GetSoundChunkForTime returned a negative value for sound with id: " +
                                    id + " for time: " + loadTime);
                                break;
                            }

                            bool setData = false;

                            if (result == 1000)
                            {
                                Debug.LogWarning("Received empty data for sound with id: " + id);
                                setData = _clip.SetData(_emptySound, _audioWritePos % _clip.samples);
                            }
                            else
                            {
                                int index = -1;
                                for (int j = 0; j < _replayAudioData.Length; ++j)
                                    if (_replayAudioData[j].Length == result)
                                        index = j;

                                if (index == -1)
                                {
                                    float[] tmpArray = new float[result];
                                    Debug.Log("New sound array is allocated! Length: " + result);
                                    Array.Copy(_soundDTO, _soundDTO.GetLowerBound(0), tmpArray,
                                        tmpArray.GetLowerBound(0), result);
                                    setData = _clip.SetData(tmpArray, _audioWritePos % _clip.samples);
                                }
                                else
                                {
                                    Array.Copy(_soundDTO, _soundDTO.GetLowerBound(0), _replayAudioData[index],
                                        _replayAudioData[index].GetLowerBound(0), result);
                                    setData = _clip.SetData(_replayAudioData[index], _audioWritePos % _clip.samples);
                                }
                            }


                            if (!setData)
                                Debug.LogError("Could not set audio data!");
                            else
                                newData = true;


                            _audioWritePos += result / _playbackChannelNum;
                            loadTime += (result / _playbackChannelNum) / (float)_playbackSamplingRate;
                            if (id == 0)
                            {
                                int x = 5;
                            }
                        }
                    }

                    _nextSoundReplayTime = loadTime;
                }
            }
            else
            {
                newData = true;
            }

            if(_mode != SpatializationMode.NoSpatialization && _targetTransform != null)
                _source.transform.position = _targetTransform.position;
            
            _lastReplayTime = replayTime;

            return newData;
        }

        public AudioSource GetAudioSource()
        {
            return _source;
        }
    }
}