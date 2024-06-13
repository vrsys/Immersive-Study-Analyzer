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
    public class ContainmentAnalysisRequest : AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        private static extern int AddContainmentAnalysisRequest(int analysisId, int objectId, float[] minMax, int logicalOp);

        private GameObject a;
        private Vector3 min, max;
        private GameObject _containmentCube;
        
        public ContainmentAnalysisRequest(int analysisId, int recorderId, GameObject go1, Vector3 mi, Vector3 ma, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.ContainmentAnalysis) {
            a = go1; min = mi; max = ma;
            string nameA = Utils.GetObjectName(a).Replace("[Rec]", "");
            int originalIdA = GetOriginalID(recorderId, nameA, nameA.Length, a.GetInstanceID());

            float[] minMax = new float[6];
            minMax[0] = min.x; minMax[1] = min.y; minMax[2] = min.z; minMax[3] = max.x; minMax[4] = max.y; minMax[5] = max.z;
            AddContainmentAnalysisRequest(analysisId, originalIdA, minMax, (int) logicalOperator);
            buttonColor = Color.blue;
            Debug.Log("Added ContainmentAnalysisRequest for analysis request with id: " + analysisId);
            Debug.Log(Text());
        }

        ~ContainmentAnalysisRequest()
        {
            ClearVisualisations();
        }

        public override void ClearVisualisations()
        {
            if(_containmentCube != null)
                GameObject.Destroy(_containmentCube);  
        }
        
        public void SetContainmentCube(GameObject cube)
        {
            _containmentCube = cube;
        }

        public GameObject GetObject()
        {
            return a;
        }

        public Vector3 GetMin()
        {
            return min;
        }

        public Vector3 GetMax()
        {
            return max;
        }

        public override string Text()
        {
            string text = "ContainmentAnalysisRequest" +
                          "<br>Object: " + a.name +
                          "<br>Bounding Box Min: " + min +
                          "<br>Bounding Box Max: " + max;
            return text;
        }
        
        public override string ShortText()
        {
            return "ContainmentAnalysis";
        }
    }
}