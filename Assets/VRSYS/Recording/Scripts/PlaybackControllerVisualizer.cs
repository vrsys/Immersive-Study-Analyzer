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