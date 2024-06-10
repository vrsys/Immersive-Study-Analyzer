using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    public class GenericRecorder : Recorder
    {
        [DllImport("RecordingPlugin")]
        private static extern bool RecordGenericAtTimestamp(int recorderId, float time, int id, int[] intArray, float[] floatArray, byte[] charArray);

        [DllImport("RecordingPlugin")]
        private static extern bool GetGenericAtTime(int recorderId, float time, int id, IntPtr intArray, IntPtr floatArray, IntPtr charArray);
        
        protected int[] _intDTO = new int[10];
        protected float[] _floatDTO = new float[10];
        protected byte[] _charDTO = new byte[10];
        protected bool replay = false;

        protected virtual bool FillGenericData()
        {
            return false;
        }
        
        protected virtual void ProcessReplayData()
        {
            
        }
        
        public override bool Record(float recordTime)
        {
            bool result = FillGenericData();

            if (result)
            {
                result = RecordGenericAtTimestamp(controller.RecorderID, recordTime, id, _intDTO, _floatDTO,
                    _charDTO);

                if (!result)
                    Debug.Log("Could not record arbitrary data with id: " + id);

                return result;
            }
            else
            {
                return true;
            }
        } 
        
        public override unsafe bool Replay(float replayTime)
        {
            replay = true;

            if (!portal)
            {
                fixed (float* f = _floatDTO)
                {
                    fixed (int* i = _intDTO)
                    {
                        fixed (byte* c = _charDTO)
                        {
                            bool result = GetGenericAtTime(controller.RecorderID, replayTime, id, (IntPtr)i,
                                (IntPtr)f, (IntPtr)c);

                            if (!result)
                            {
                                //Debug.Log("Could not replay arbitrary data with id: " + id + " for object with name: " + gameObject.name);
                                return false;
                            }

                            ProcessReplayData();

                            return true;
                        }
                    }
                }
            }
            return true;
        } 
        
        public override bool Preview(float previewTime)
        {
            return false;
        } 
    }
}