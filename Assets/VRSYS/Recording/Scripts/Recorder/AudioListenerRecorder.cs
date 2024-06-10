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