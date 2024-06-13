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
using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(AudioListener))]
    public class AudioListenerRecorder : AudioRecorder
    {
        private long recordStartTime = -1;

        public void Start()
        {
            base.Start();
            if(RecordingSamplingRate == -1)
                RecordingSamplingRate = AudioSettings.outputSampleRate;
        }
        
        public override bool Record(float recordTime)
        {
            if (RecordingTime < 0.0f)
            {
                RecordingTime = recordTime;
                recordStartTime = DateTime.Now.Ticks;
                FirstRecord = true;
            }

            if (Mathf.Abs(recordTime - RecordingTime) > 0.1f)
            {
                Debug.LogError("Error! Audio Listener time not aligned. Difference: " + (recordTime - RecordingTime));
                RecordingTime = recordTime;
            }

            return true;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (RecordingChannelNum == -1)
                RecordingChannelNum = channels;

            if (FirstRecord)
            {
                RecordingTime += (new TimeSpan(DateTime.Now.Ticks - recordStartTime)).Milliseconds / 1000.0f;
                FirstRecord = false;
            }
            
            if (RecordingTime >= 0.0f && controller.CurrentState == State.Recording)
            {
                float duration = (data.Length / RecordingChannelNum) / (float)RecordingSamplingRate;
                float recordingTimeOfChunk = RecordingTime - duration;
                if (recordingTimeOfChunk < 0)
                {
                    Debug.LogError("Error! Sound recording time should not be negative!");
                    recordingTimeOfChunk = 0.0f;
                    RecordingTime = duration;
                }

                RecordSoundDataAtTimestamp(controller.RecorderID, data, data.Length, RecordingSamplingRate, 0, RecordingChannelNum, recordingTimeOfChunk, id);
                RecordingTime += duration;
            }
        }
    }
}