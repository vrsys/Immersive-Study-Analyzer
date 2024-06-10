using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    public abstract class Recorder : MonoBehaviour
    {
        public RecorderController controller;
        
        public RecorderController Controller
        {
            get => controller;

            set { controller = value; RegisterRecorder(); }
        }
        
        public bool registered = false;
        protected int id = 99999;
        protected int recorderId = 99999;
        public bool preview = false;
        public bool portal = false;

        protected AudioClip _soundEffect;
        protected Material _trailMaterial;

        protected float _lastReplayTime;
        
        public virtual void Start()
        {
            if(id == 99999)
                id = gameObject.GetInstanceID();
            
            if (controller != null && !registered)
                RegisterRecorder();
            
            _soundEffect = Resources.Load<AudioClip>("Audio/ambient");
            _trailMaterial = Resources.Load<Material>("Textures/Trail");
        }

        public void OnDestroy()
        {
            DeregisterRecorder();
        }

        public virtual bool Record(float recordTime)
        {
            return false;
        }

        public virtual bool Replay(float replayTime)
        {
            return false;
        }
        
        public virtual bool Preview(float previewTime)
        {
            return false;
        }

        public void RegisterRecorder()
        {
            if (id == 99999)
            {
                Debug.LogError("Error! Id not correctly set!");
            }

            controller.RegisterRecorder(id, this);
            recorderId = controller.RecorderID;
            registered = true;
        }

        public void DeregisterRecorder()
        {
            if(controller != null)
                controller.DeregisterRecorder(id, this);
            recorderId = 99999;
            registered = false;
        }
        
        public virtual void Update()
        {
            if (!registered && controller != null)
                RegisterRecorder();
        }

        public void MarkAsPreviewRecorder()
        {
            preview = true;
            registered = false;
            portal = false;
        }

        public void MarkAsPortalRecorder()
        {
            preview = false;
            registered = false;
            portal = true;
        }
        
        public void SetId(int customId)
        {
            id = customId;
        }
    }
}