using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    public class OneEuroFilter
    {
        private bool _firstTime = true;
        private LowpassFilter xfilt = new LowpassFilter();
        private LowpassFilter dxfilt = new LowpassFilter();
        private float _dcutoff, _cutoff, _rate, _mincutoff, _beta;
        public OneEuroFilter(float dcutoff, float cutoff, float rate, float mincutoff, float beta)
        {
            _dcutoff = dcutoff;
            _cutoff = cutoff;
            _rate = rate;
            _mincutoff = mincutoff;
            _beta = beta;
        }
        
        public float Filter(float x)
        {
            float dx = _firstTime ? 0 : (x - xfilt.GetHatXPrev()) * _rate;
            float edx = dxfilt.Filter(dx, _dcutoff, _rate);
            _cutoff = _mincutoff + _beta * Mathf.Abs(edx);
            return xfilt.Filter(x, _cutoff, _rate);
        }
    }
    
    public class LowpassFilter
    {
        private bool _firstTime = true;
        private float _hatXPrev = 0.0f;

        public float Filter(float x, float cutoff, float rate)
        {
            if (_firstTime)
            {
                _firstTime = false;
                _hatXPrev = x;
            }

            float alpha = ComputeAlpha(cutoff, rate);
            float hatX = alpha * x + (1 - alpha) * _hatXPrev;
            _hatXPrev = hatX;
            return hatX;
        }

        public float GetHatXPrev()
        {
            return _hatXPrev;
        }

        private float ComputeAlpha(float cutoff, float rate)
        {
            float tau = 1.0f / (2.0f * Mathf.PI * cutoff);
            float te = 1.0f / rate;
            return 1.0f / (1.0f + tau / te);
        }
    }

}