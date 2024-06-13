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

using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Vrsys.Scripts.Recording;
using UnityEngine.UI;


namespace VRSYS.Scripts.Recording
{
    public class PlaybackControllerVisualizer : MonoBehaviourPunCallbacks, IInRoomCallbacks
    {

        //[SerializeField] private GameObject volumeControllerInstance;
        [SerializeField] private GameObject playbackControllerInstance;
        [SerializeField] private GameObject crownInstance;
        [SerializeField] private GameObject playbackStartCanvas;
        [SerializeField] private GameObject timeSliderCanvas;
        [SerializeField] private GameObject audioVolumeText;

        private bool deactivatedCanvas = false;
        private RecorderState state;
        private GameObject recodingSetup;
        private AudioSource playBackAudioSource;

        // Start is called before the first frame update
        void Start()
        {
            if (PhotonNetwork.IsMasterClient && photonView.IsMine)
            {
                photonView.RPC("ActivatePlaybackControllerVisuals", RpcTarget.All);
            }
            recodingSetup = GameObject.Find("RecordingSetup");
            state = recodingSetup.GetComponent<RecorderState>();
        }

        [PunRPC]
        public void ActivatePlaybackControllerVisuals()
        {
            //volumeControllerInstance?.SetActive(true);
            playbackControllerInstance?.SetActive(true);
            crownInstance?.SetActive(true);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient && photonView.IsMine)
            {
                photonView.RPC("ActivatePlaybackControllerVisuals", newPlayer);
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (PhotonNetwork.IsMasterClient && photonView.IsMine)
            {
                photonView.RPC("ActivatePlaybackControllerVisuals", RpcTarget.All);
            }
        }

        public void Update()
        {
            if (state == null)
            {
                recodingSetup = GameObject.Find("RecordingSetup");
                state = recodingSetup.GetComponent<RecorderState>();
            }
            
            if (state != null && state.currentState == State.Replaying)
            {
                if(playBackAudioSource == null)
                    playBackAudioSource = recodingSetup.GetComponent<AudioSource>();
                float currentVolume = 1.0f;
                if(playBackAudioSource != null)
                    currentVolume = playBackAudioSource.volume;
                int level = Mathf.FloorToInt(currentVolume * 100);
                
                if(audioVolumeText != null)
                    audioVolumeText.GetComponent<Text>().text = level + "%";
                
                if (!deactivatedCanvas && playbackControllerInstance.activeSelf)
                {
                    GameObject canvas = Utils.GetChildByName(gameObject, "PlaybackStartCanvas");
                    if(canvas != null)
                        canvas.SetActive(false);

                    canvas = Utils.GetChildByName(gameObject, "TimeSliderCanvas");
                    if(canvas != null)
                        canvas.SetActive(true);
                
                    canvas = Utils.GetChildByName(gameObject, "PlaybackPauseCanvas");
                    if(canvas != null)
                        canvas.SetActive(true);
                    deactivatedCanvas = true;
                }
            }
        }
    }
}