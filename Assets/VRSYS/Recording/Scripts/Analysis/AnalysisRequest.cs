using System.Runtime.InteropServices;
using UnityEngine;

namespace VRSYS.Recording.Scripts.Analysis
{
    public struct TimeInterval
    {
        public float startTime;
        public float endTime;
        public int analysisId;
    }

    public enum LogicalOperator
    {
        And = 0,
        Or = 1,
        AndNegated = 2,
        OrNegated = 3
    }

    public enum AnalysisRequestType
    {
        DistanceAnalysis, GazeAnalysis, ContainmentAnalysis, MovementAnalysis, SoundActivationAnalysis, VelocityAnalysis
    }
    
    public class AnalysisRequest
    {
        [DllImport("RecordingPlugin")]
        protected static extern int GetOriginalID(int recorderId, string objectName, int objectNameLenght, int newObjectId);
        
        private int analysisId = 0;
        private AnalysisRequestType type;
        protected Color buttonColor;
        
        protected AnalysisRequest(int id, AnalysisRequestType t)
        {
            analysisId = id;
            type = t;
        }

        public virtual void ClearVisualisations()
        {
            
        }
        
        public int getAnalysisId()
        {
            return analysisId;
        }

        public AnalysisRequestType getAnalysisRequestType()
        {
            return type;
        }

        public virtual string Text()
        {
            return "";
        } 
        
        public virtual string ShortText()
        {
            return "";
        } 
        
        public Color GetButtonColor()
        {
            return buttonColor;
        } 
    }
}