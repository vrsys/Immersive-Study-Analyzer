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