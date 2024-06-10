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