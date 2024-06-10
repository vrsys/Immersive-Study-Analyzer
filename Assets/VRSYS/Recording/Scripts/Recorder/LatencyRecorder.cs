using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    [RequireComponent(typeof(NetworkController))]
    public class LatencyRecorder : GenericRecorder
    {
        private NetworkController _networkController;

        public override void Start()
        {
            base.Start();
            _networkController = GetComponent<NetworkController>();
        }
        
        public override void Update()
        {
            _networkController.StartRoundTripTest();
        }

        protected override bool FillGenericData()
        {
            _floatDTO[0] = _networkController.currentLatency;
            return true;
        }
        
        protected override void ProcessReplayData()
        {
            float latency = _floatDTO[0];
            //Debug.Log("Latency: " + latency);
        }
    }
}