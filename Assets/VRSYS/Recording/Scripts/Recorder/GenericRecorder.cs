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