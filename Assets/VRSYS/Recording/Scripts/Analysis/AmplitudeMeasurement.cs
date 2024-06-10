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
