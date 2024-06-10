using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace VRSYS.Scripts.Recording
{
    [Serializable]
    public class Word
    {
        public string word;
        public float start_time;
        public float duration;
    }

    [Serializable]
    public class Transcript
    {
        public float confidence;
        public Word[] words;
    }

    [Serializable]
    public class Transcripts
    {
        public Transcript[] transcripts;
    }
    
    public class SubtitleVisualizer : MonoBehaviourPun
    {
        public float subtitleFuturePreview = 3.5f;
        public GameObject subtitleTextGO;
        
        private Text _subtitle;
        private Transcripts _transcripts;

        private int _lastSubtitleIndex = -1;
        private float _subtitleLastTimeStamp = 0.0f;

        public void Start()
        {
            if (!photonView.IsMine)
                return;
            if(subtitleTextGO != null)
                _subtitle = subtitleTextGO.GetComponent<Text>();
        }

        public void Activate()
        {
            if (!photonView.IsMine)
                return;
            subtitleTextGO.SetActive(true);
            _subtitle.enabled = true;
            _lastSubtitleIndex = -1;
            _subtitleLastTimeStamp = 0.0f;
        }

        public void Deactivate()
        {
            if (!photonView.IsMine)
                return;
            subtitleTextGO.SetActive(false);
            _subtitle.enabled = false;
            _lastSubtitleIndex = -1;
            _subtitleLastTimeStamp = 0.0f;
        }
        
        public void SubtitleGeneration(float replayTime)
        {
            if (!photonView.IsMine)
                return;
            if (_transcripts != null)
            {
                if (_subtitleLastTimeStamp + subtitleFuturePreview <= replayTime || replayTime < _subtitleLastTimeStamp)
                {
                    if (replayTime < _subtitleLastTimeStamp)
                    {
                        _subtitleLastTimeStamp = replayTime;
                        _lastSubtitleIndex = 0;
                    }

                    string subtitleText = "";
                    float startTime = _subtitleLastTimeStamp + subtitleFuturePreview;
                    float endTime = startTime + subtitleFuturePreview;
                    if (_lastSubtitleIndex == -1)
                        _lastSubtitleIndex = 0;
                    if (_transcripts.transcripts.Length > 0)
                    {
                        for (int i = _lastSubtitleIndex; i < _transcripts.transcripts[0].words.Length; i++)
                        {
                            Word currentWord = _transcripts.transcripts[0].words[i];
                            if (currentWord.start_time <= endTime)
                            {
                                subtitleText += currentWord.word + " ";
                                _lastSubtitleIndex = i;
                            } else 
                                break;
                        }
                    }

                    _subtitle.text = subtitleText;
                    _subtitleLastTimeStamp = replayTime;
                }
            }
        }

        public void SetTranscripts(Transcripts newTranscripts)
        {
            if (!photonView.IsMine)
                return;
            _transcripts = newTranscripts;
        }

        public bool TranscriptsSet()
        {
            return _transcripts != null;
        }
    }
}