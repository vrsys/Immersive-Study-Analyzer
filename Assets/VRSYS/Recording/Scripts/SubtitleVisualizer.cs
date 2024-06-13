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