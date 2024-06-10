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