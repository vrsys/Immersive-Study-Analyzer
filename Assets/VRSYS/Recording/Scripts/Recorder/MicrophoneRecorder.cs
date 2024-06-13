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

using UnityEngine;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    public class MicrophoneRecorder : AudioRecorder
    {
        private MicrophoneClipReader _microphoneClipReader;
        private float[] _audioData;
        private int _audioSamplesPerRecordStep = -1;

        public void SetMicrophoneReader(MicrophoneClipReader reader)
        {
            _microphoneClipReader = reader;
        }
        
        public void Start()
        {
            base.Start();
        }
        
        public override bool Record(float recordTime)
        {
            if (RecordingSamplingRate < 0)
            {
                RecordingChannelNum = _microphoneClipReader.Channels;
                RecordingSamplingRate = _microphoneClipReader.SamplingRate;
                _audioSamplesPerRecordStep = RecordingSamplingRate / SoundRecordingStepsPerSecond;
            
                _audioData = new float[_audioSamplesPerRecordStep];
                FirstRecord = true;
            }
            
            float readAudio = 1;
            bool recordedData = false;

            float recordedTime = 0.0f;
            
            while (readAudio > 0)
            {
                readAudio = _microphoneClipReader.Read(_audioData);
                if (readAudio >= 0)
                {
                    if (RecordingTime < 0 && FirstRecord)
                    {
                        RecordingTime = recordTime - readAudio / SoundRecordingStepsPerSecond;
                        FirstRecord = false;
                        Debug.Log("Initial microphone recording time: " + RecordingTime);
                    }

                    if (RecordingTime >= 0.0f)
                    {
                        bool result = RecordSoundDataAtTimestamp(controller.RecorderID, _audioData, _audioSamplesPerRecordStep, RecordingSamplingRate, 0, RecordingChannelNum, RecordingTime, id);

                        if (!result)
                            return false;
                    }

                    RecordingTime += _audioSamplesPerRecordStep / (float)RecordingSamplingRate;
                    recordedTime = _audioSamplesPerRecordStep / (float)RecordingSamplingRate;
                    
                    recordedData = true;
                }
            }
            
            if (recordedData && Mathf.Abs(recordTime - (RecordingTime + recordedTime)) > 0.5f)
            {
                Debug.LogError("Error! Microphone time not aligned. Difference: " + (recordTime - (RecordingTime + recordedTime)));
            }

            return true;
        }
    }
}