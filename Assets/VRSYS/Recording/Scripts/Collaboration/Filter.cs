// VRSYS plugin of Virtual Reality and Visualization Group (Bauhaus-University Weimar)
//  _    ______  _______  _______
// | |  / / __ \/ ___/\ \/ / ___/
// | | / / /_/ /\__ \  \  /\__ \
// | |/ / _, _/___/ /  / /___/ /
// |___/_/ |_|/____/  /_//____/
//
//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |
//
//       ___               __
// |  | |__  |  |\/|  /\  |__)
// |/\| |___ |  |  | /~~\ |  \
//
// Copyright (c) 2024 Virtual Reality and Visualization Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Anton Lammert
//   Date:           2024
//-----------------------------------------------------------------

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