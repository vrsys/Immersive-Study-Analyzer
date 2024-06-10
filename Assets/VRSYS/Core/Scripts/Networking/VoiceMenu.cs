// VRSYS plugin of Virtual Reality and Visualization Research Group (Bauhaus University Weimar)
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
// Copyright (c) 2022 Virtual Reality and Visualization Research Group
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
//   Authors:        Ephraim Schott, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Vrsys
{
    public class VoiceMenu : MonoBehaviour
    {
        public VoiceManager voiceManager;
        public Toggle muteToggle;
        public TMP_Dropdown microphoneDropdown;

        private void Awake()
        {
            if (voiceManager == null)
            {
                voiceManager = GameObject.Find("VoiceManager").GetComponent<VoiceManager>();
            }

            if (muteToggle == null)
            {
                muteToggle = GameObject.Find("MuteToggle").GetComponent<Toggle>();
            }

            if (voiceManager.isMuted)
            {
                muteToggle.isOn = true;
                //muteToggle.enabled = 
            }
            else
            {
                muteToggle.isOn = false;
            }
            muteToggle.onValueChanged.AddListener(delegate { ToggleMute(); });

            if (microphoneDropdown == null)
            {
                microphoneDropdown = GameObject.Find("MicrophoneDrop").GetComponent<TMP_Dropdown>();
            }
            microphoneDropdown.onValueChanged.AddListener(delegate { SelectMicrophone(); });


        }
        // Start is called before the first frame update
        void Start()
        {
            microphoneDropdown.ClearOptions();
            microphoneDropdown.AddOptions(voiceManager.GetMicrophones());
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void ToggleMute()
        {
            voiceManager.ToggleMute();
        }

        private void SelectMicrophone()
        {
            voiceManager.SetMicrophone(microphoneDropdown.options[microphoneDropdown.value].text);
            voiceManager.StartRecording();
            microphoneDropdown.RefreshShownValue();
        }
    }
}