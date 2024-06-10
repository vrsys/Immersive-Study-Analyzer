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