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
using UnityEngine.InputSystem;
using Vrsys;


namespace VRSYS.Scripts.Recording
{


    public class PlaybackVolumeController : MonoBehaviour
    {
        [SerializeField] private InputActionProperty reduceVolume;
        [SerializeField] private InputActionProperty increaseVolume;
        [SerializeField] private float amountVolumeUp;
        [SerializeField] private float amountVolumeDown;

        private AudioSource playBackAudioSource;
        private RecorderState state;
        private GameObject recodingSetup;
        private bool reduceVolumeWasPressed = false;
        private bool increaseVolumeWasPressed = false;
        public float currentVolume = 0.5f;
        private ViewingSetupHMDAnatomy hmd;
        private AvatarHMDAnatomy hmdAnatomy;

        // Start is called before the first frame update
        void Start()
        {
            recodingSetup = GameObject.Find("RecordingSetup");
            state = recodingSetup.GetComponent<RecorderState>();

        }

        // Update is called once per frame
        void Update()
        {
            adjustCurrentLocalVolume();
        }


        private void adjustCurrentLocalVolume()
        {
            if (state.currentState != State.Replaying)
            {
                Debug.Log("not replaying");
                return;
            }

            if (EnsureViewingSetup())
            {
                if (playBackAudioSource == null)
                    playBackAudioSource = recodingSetup.GetComponent<AudioSource>();

                if (playBackAudioSource != null)
                {
                    currentVolume = playBackAudioSource.volume;

                    if (reduceVolume.action.IsPressed() && !reduceVolumeWasPressed)
                    {
                        currentVolume -= amountVolumeDown;
                    }

                    if (increaseVolume.action.IsPressed() && !increaseVolumeWasPressed)
                    {
                        currentVolume += amountVolumeDown;
                    }

                    currentVolume = Mathf.Clamp(currentVolume, -0.01f, 1.01f);

                    playBackAudioSource.volume = currentVolume;
                    reduceVolumeWasPressed = reduceVolume.action.IsPressed();
                    increaseVolumeWasPressed = increaseVolume.action.IsPressed();
                }
            }
        }

        bool EnsureViewingSetup() 
        {
            if(hmd != null)
                return true;

            if (GetComponent<NetworkUser>().viewingSetupAnatomy == null)
            {
                hmdAnatomy = GetComponent<AvatarAnatomy>() as AvatarHMDAnatomy;
                return false;
            }

            if(!(GetComponent<NetworkUser>().viewingSetupAnatomy is ViewingSetupHMDAnatomy))
                return false;

            hmd = GetComponent<NetworkUser>().viewingSetupAnatomy as ViewingSetupHMDAnatomy;

            return true;
        }
    }
}
