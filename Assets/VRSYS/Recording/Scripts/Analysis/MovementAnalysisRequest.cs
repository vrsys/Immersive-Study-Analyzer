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

using System.Runtime.InteropServices;
using UnityEngine;
using Vrsys.Scripts.Recording;

namespace VRSYS.Recording.Scripts.Analysis
{
    public class MovementAnalysisRequest : AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        private static extern int AddRotationAnalysisRequest(int analysisId, int idA, float temporalSearchInterval, float rotationThreshold, int logicalOp);

        private GameObject a;
        private float temporalSearchInterval, rotationThreshold;
        
        public MovementAnalysisRequest(int analysisId, int recorderId, GameObject go1, float tSearchInterval, float rotThresh, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.MovementAnalysis) {
            a = go1; temporalSearchInterval = tSearchInterval; rotationThreshold = rotThresh;
            string nameA = Utils.GetObjectName(a).Replace("[Rec]", "");
            int originalIdA = GetOriginalID(recorderId, nameA, nameA.Length, a.GetInstanceID());
            buttonColor = Color.cyan;
            AddRotationAnalysisRequest(analysisId, originalIdA, temporalSearchInterval, rotationThreshold, (int) logicalOperator);
            Debug.Log("Added MovementAnalysisRequest for analysis request with id: " + analysisId);
        }
        
        public GameObject GetObject()
        {
            return a;
        }

        public float GetTemporalSearchInterval()
        {
            return temporalSearchInterval;
        }

        public float GetRotationThreshold()
        {
            return rotationThreshold;
        }
        
        public override string Text()
        {
            string text = "MovementAnalysisRequest" +
                          "<br>Object: " + a.name +
                          "<br>Temporal search interval: " + temporalSearchInterval +
                          "<br>Rotation threshold: " + rotationThreshold;
            return text;
        }
        
        public override string ShortText()
        {
            return "MovementAnalysis";
        }
        
    }
}