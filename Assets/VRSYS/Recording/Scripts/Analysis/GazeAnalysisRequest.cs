using System.Runtime.InteropServices;
using UnityEngine;
using Vrsys.Scripts.Recording;

namespace VRSYS.Recording.Scripts.Analysis
{
    public class GazeAnalysisRequest : AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        private static extern int AddGazeAnalysisRequest(int analysisId, int idA, int idB, float coneAngle, float maxDistance, int logicalOp);

        [DllImport("RecordingPlugin")]
        private static extern int AddGazeAnalysisRequestAx(int analysisId, int idA, int idB, float coneAngle, float maxDistance, int axis, int logicalOp);
        
        private GameObject a, b;
        private float coneAngle, maxDistance;
        private GameObject gazePreview1, gazePreview2;
        
        public GazeAnalysisRequest(int analysisId, int recorderId, GameObject go1, GameObject go2, float angle, float dist, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.GazeAnalysis) {
            a = go1; b = go2; coneAngle = angle; maxDistance = dist;
            string nameA = Utils.GetObjectName(a).Replace("[Rec]", "");
            string nameB = Utils.GetObjectName(b).Replace("[Rec]", "");
            int originalIdA = GetOriginalID(recorderId, nameA, nameA.Length, a.GetInstanceID());
            int originalIdB = GetOriginalID(recorderId, nameB, nameB.Length, b.GetInstanceID());
            buttonColor = Color.green;
            AddGazeAnalysisRequest(analysisId, originalIdA, originalIdB, coneAngle, maxDistance, (int)logicalOperator);
            Debug.Log("Added GazeAnalysisRequest for analysis request with id: " + analysisId);
        }
        
        public GazeAnalysisRequest(int analysisId, int recorderId, GameObject go1, GameObject go2, float angle, float dist, int axis, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.GazeAnalysis) {
            a = go1; b = go2; coneAngle = angle; maxDistance = dist;
            string nameA = Utils.GetObjectName(a).Replace("[Rec]", "");
            string nameB = Utils.GetObjectName(b).Replace("[Rec]", "");
            int originalIdA = GetOriginalID(recorderId, nameA, nameA.Length, a.GetInstanceID());
            int originalIdB = GetOriginalID(recorderId, nameB, nameB.Length, b.GetInstanceID());
            buttonColor = Color.green;
            AddGazeAnalysisRequestAx(analysisId, originalIdA, originalIdB, coneAngle, maxDistance, axis, (int)logicalOperator);
            Debug.Log("Added GazeAnalysisRequest for analysis request with id: " + analysisId);
        }

        ~GazeAnalysisRequest()
        {
            ClearVisualisations();
        }

        public override void ClearVisualisations()
        {
            if(gazePreview1 != null)
                GameObject.Destroy(gazePreview1);  
            
            if(gazePreview2 != null)
                GameObject.Destroy(gazePreview2);  
        }

        
        public void SetGazePreviewGOs(GameObject cone, GameObject go)
        {
            gazePreview1 = cone;
            gazePreview2 = go;
        }
        
        public GameObject GetObjectA()
        {
            return a;
        }

        public GameObject GetObjectB()
        {
            return b;
        }

        public float GetConeAngle()
        {
            return coneAngle;
        }
        
        public float GetMaxDist()
        {
            return maxDistance;
        }
        
        public override string Text()
        {
            string text = "GazeAnalysisRequest" +
                          "<br>Object A: " + a.name +
                          "<br>Object B: " + b.name +
                          "<br>Cone angle: " + coneAngle +
                          "<br>max Distance: " + maxDistance;
            return text;
        }
        
        public override string ShortText()
        {
            return "GazeAnalysis";
        }
    }
}