using System;
using System.Runtime.InteropServices;
using AOT;
using Photon.Pun;
using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    enum PrintColor
    {
        red,
        green,
        blue,
        black,
        white,
        yellow,
        orange
    };
    
    public class PluginConfigurator : MonoBehaviourPun
    {
        [DllImport("RecordingPlugin")]
        private static extern bool SetRecordingMaxBufferSize(int recorderId, int maxBufferSize);

        [DllImport("RecordingPlugin")]
        private static extern bool SetReplayBufferNumber(int recorderId, int bufferNumber);

        [DllImport("RecordingPlugin")]
        private static extern bool SetReplayBufferStoredTimeInterval(int recorderId, float timeInterval);

        [DllImport("RecordingPlugin")]
        private static extern bool SetSoundRecordingMaxBufferSize(int recorderId, int maxBufferSize);
        
        [DllImport("RecordingPlugin", CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterDebugCallback(debugCallback cb);
        
        [DllImport("RecordingPlugin")]
        private static extern bool SetDebugMode(int recorder_id, bool debug);

        public int recordingMaxBufferSize = 10000;
        public int replayBufferNumber = 3;
        public float replayBufferTimeInterval = 10.0f;
        public int recordingSoundMaxBufferSize = 100;
        public bool isDebug = false;
        private bool isDebugLast = false;
        [HideInInspector] public string recordingDirectory = "";
        
        public void Start()
        {
            if (!photonView.IsMine)
                return;
            RegisterDebugCallback(OnDebugCallback);
            SetRecordingMaxBufferSize(0, recordingMaxBufferSize);
            SetSoundRecordingMaxBufferSize(0, recordingSoundMaxBufferSize);
            SetReplayBufferNumber(0, replayBufferNumber);
            SetReplayBufferStoredTimeInterval(0, replayBufferTimeInterval);
        }

        public void Update()
        {
            if (!photonView.IsMine)
                return;
            if (isDebug != isDebugLast)
            {
                isDebugLast = isDebug;
                SetDebugMode(0, isDebug);
            }
        }

        delegate void debugCallback(IntPtr request, int color, int size);

        
        [MonoPInvokeCallback(typeof(debugCallback))]
        static void OnDebugCallback(IntPtr request, int color, int size)
        {
            //Ptr to string
            string debug_string = Marshal.PtrToStringAnsi(request, size);

            //Add Specified Color
            //debug_string =
            //    String.Format("{0}{1}{2}{3}{4}",
            //        "<color=",
            //        ((PrintColor)color).ToString(),
            //        ">",
            //        debug_string,
            //        "</color>"
            //    );
            
            if((PrintColor)color == PrintColor.black)
                Debug.Log(debug_string);
            else if ((PrintColor)color == PrintColor.yellow)
                Debug.LogWarning(debug_string);
            else if ((PrintColor)color == PrintColor.red)
                Debug.LogError(debug_string);
            else 
                Debug.LogAssertion(debug_string);
        }
    }
}