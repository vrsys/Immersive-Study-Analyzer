using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    public class FPSComputer : MonoBehaviour
    {
        public float updateRate = 1.0f;
        private int _frameCount = 0;
        private float _deltaT;
        private float _fps;

        public void Update()
        {
            _frameCount++;
            _deltaT += Time.deltaTime;
            if (_deltaT > 1.0f / updateRate)
            {
                _fps = _frameCount / _deltaT;
                _frameCount = 0;
                _deltaT -= 1.0f / updateRate;
            }
        }

        public float GetFPS()
        {
            return _fps;
        }
    }
}