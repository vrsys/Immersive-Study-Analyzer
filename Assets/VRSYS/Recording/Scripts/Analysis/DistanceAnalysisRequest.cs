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
    public class DistanceAnalysisRequest : AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        private static extern int AddDistanceAnalysisRequest(int analysisId, int idA, int idB, float maxDist, int logicalOp);

        private GameObject a, b;
        private float maxDistance = -1.0f;
        private GameObject distancePreview1, distancePreview2;
        
        public DistanceAnalysisRequest(int analysisId, int recorderId, GameObject go1, GameObject go2, float dist, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.DistanceAnalysis) {
            a = go1; b = go2; maxDistance = dist;
            string nameA = Utils.GetObjectName(a).Replace("[Rec]", "");
            string nameB = Utils.GetObjectName(b).Replace("[Rec]", "");
            int originalIdA = GetOriginalID(recorderId, nameA, nameA.Length, a.GetInstanceID());
            int originalIdB = GetOriginalID(recorderId, nameB, nameB.Length, b.GetInstanceID());
            AddDistanceAnalysisRequest(analysisId, originalIdA, originalIdB,maxDistance, (int) logicalOperator);
            buttonColor = Color.yellow;
            Debug.Log("Added DistanceAnalysisRequest for analysis request with id: " + analysisId);
        }

        ~DistanceAnalysisRequest()
        {
            ClearVisualisations();
        }

        public override void ClearVisualisations()
        {
            if(distancePreview1 != null)
                GameObject.Destroy(distancePreview1);  
            
            if(distancePreview2 != null)
                GameObject.Destroy(distancePreview2);  
        }

        
        public void SetDistancePreviewGOs(GameObject go1, GameObject go2)
        {
            distancePreview1 = go1;
            distancePreview2 = go2;
        }
        
        public GameObject GetObjectA()
        {
            return a;
        }

        public GameObject GetObjectB()
        {
            return b;
        }

        public float GetMaxDist()
        {
            return maxDistance;
        }
        
        public override string Text()
        {
            string text = "DistanceRequestDetails" +
                          "<br>Object A: " + a.name +
                          "<br>Object B: " + b.name +
                          "<br>max Distance: " + maxDistance;
            return text;
        }
        
        public override string ShortText()
        {
            return "DistanceAnalysis";
        }
    }
}