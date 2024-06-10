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