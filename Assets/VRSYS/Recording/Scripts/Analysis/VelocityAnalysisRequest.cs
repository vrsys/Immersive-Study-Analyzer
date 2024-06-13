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
    public class VelocityAnalysisRequest : AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        private static extern int AddVelocityAnalysisRequest(int analysisId, int idA, float temporalSearchInterval, float velocityThreshold, int logicalOp);

        private GameObject a;
        private float temporalSearchInterval, velocityThreshold;
        
        public VelocityAnalysisRequest(int analysisId, int recorderId, GameObject go1, float tSearchInterval, float velThresh, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.VelocityAnalysis) {
            a = go1; temporalSearchInterval = tSearchInterval; velocityThreshold = velThresh;
            string nameA = Utils.GetObjectName(a).Replace("[Rec]", "");
            int originalIdA = GetOriginalID(recorderId, nameA, nameA.Length, a.GetInstanceID());
            buttonColor = Color.grey;
            AddVelocityAnalysisRequest(analysisId, originalIdA, temporalSearchInterval, velocityThreshold, (int)logicalOperator);
            Debug.Log("Added VelocityAnalysisRequest for analysis request with id: " + analysisId);
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
            return velocityThreshold;
        }
        
        public override string Text()
        {
            string text = "VelocityAnalysisRequest" +
                          "<br>Object: " + a.name +
                          "<br>Temporal search interval: " + temporalSearchInterval +
                          "<br>Velocity threshold: " + velocityThreshold;
            return text;
        }
        
        public override string ShortText()
        {
            return "VelocityAnalysis";
        }
        
    }
}