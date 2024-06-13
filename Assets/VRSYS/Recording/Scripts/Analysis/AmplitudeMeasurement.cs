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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AmplitudeMeasurement : MonoBehaviour
{
    public float averageAmplitude;
    public float peakAmplitude;

    public float samplingTime = 0.5f;
    private float timeSinceLastUpdate = 0.0f;
    private float lastFilterTime = 0.0f;
    private float peakAmplitudeCandidate = 0.0f;
    private float averageAmplitudeWeightedSum = 0.0f;
    private float currentTime = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        lastFilterTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        currentTime = Time.time;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        int numSamples = data.Length / channels;
        float sample;
        float amplitudeSum = 0;
        float currentPeakAmp = 0;
        for(int sampleIdx = 0; sampleIdx < numSamples; ++sampleIdx)
        {
            sample = 0;
            for (int channelIdx = 0; channelIdx < channels; ++channelIdx)
            {
                sample += Mathf.Abs(data[sampleIdx * channels + channelIdx]);
            }
            //sample = sample / channels;
            currentPeakAmp = Mathf.Max(currentPeakAmp, sample);
            amplitudeSum += sample;
        }
        float currentAverageAmp = amplitudeSum / numSamples;

        ApplyCurrentSample(currentPeakAmp, currentAverageAmp);
    }

    void ApplyCurrentSample(float currentPeakAmplitude, float currentAverageAmplitude)
    {
        if(samplingTime <= 0)
        {
            SetCurrentMeasurements(currentPeakAmplitude, currentAverageAmplitude);
        }
        else
        {
            // update peak
            peakAmplitudeCandidate = Mathf.Max(currentPeakAmplitude, peakAmplitudeCandidate);
            // add weighted average
            var timePassed = currentTime - lastFilterTime;
            averageAmplitudeWeightedSum += currentAverageAmplitude * (timePassed / samplingTime);
            // check if sampling time passed and update public members
            timeSinceLastUpdate += timePassed;
            if (timeSinceLastUpdate >= samplingTime)
                SetCurrentMeasurements(peakAmplitudeCandidate, averageAmplitudeWeightedSum);
        }
        lastFilterTime = currentTime;
    }

    private void SetCurrentMeasurements(float currentPeakAmplitude, float currentAverageAmplitude)
    {
        averageAmplitude = currentAverageAmplitude;
        peakAmplitude = currentPeakAmplitude;
        timeSinceLastUpdate = 0;
        peakAmplitudeCandidate = 0;
        averageAmplitudeWeightedSum = 0;
    }
}
