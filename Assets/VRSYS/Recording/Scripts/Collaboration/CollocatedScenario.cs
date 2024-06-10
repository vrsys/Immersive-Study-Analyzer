using UnityEngine;

namespace VRSYS.Recording.Scripts.Collaboration
{
    public class CollocatedScenario : MonoBehaviour
    {
        public void Start()
        {
            Photon.Voice.Unity.Recorder rec = GameObject.Find("Voice Manager").GetComponent<Photon.Voice.Unity.Recorder>();
            rec.TransmitEnabled = false;
        }
    }
}