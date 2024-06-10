using System.Runtime.InteropServices;
using UnityEngine;

namespace VRSYS.Recording.Scripts.Analysis
{
    public class SoundActivationAnalysisRequest : AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        private static extern int AddSoundActivationAnalysisRequest(int analysisId, int soundId, float temporalSearchInterval, float activationLevel, int logicalOp);

        private int soundId;
        private float temporalSearchInterval, activationLevel;
        
        public SoundActivationAnalysisRequest(int analysisId, int sId, float tSearchInterval, float actThresh, LogicalOperator logicalOperator) : base(analysisId, AnalysisRequestType.SoundActivationAnalysis) {
            soundId = sId; temporalSearchInterval = tSearchInterval; activationLevel = actThresh;
            buttonColor = Color.red;
            AddSoundActivationAnalysisRequest(analysisId, soundId, temporalSearchInterval, activationLevel, (int)logicalOperator);
            Debug.Log("Added " + Text() + " for analysis request with id: " + analysisId);
        }
        
        public override string Text()
        {
            string text = "SoundAnalysisRequest" +
                          "<br>Sound ID: " + soundId +
                          "<br>Temporal Search Interval: " + temporalSearchInterval +
                          "<br>Activation Threshold: " + activationLevel;
            return text;
        }
        
        public override string ShortText()
        {
            return "SoundAnalysis";
        }
    }
}