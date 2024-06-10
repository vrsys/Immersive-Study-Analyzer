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
