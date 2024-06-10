using UnityEngine;

namespace VRSYS.Recording.Scripts
{
    public class CameraVisibilityInformation : MonoBehaviour
    {
        [HideInInspector] public bool visible;
        private Renderer[] _renderers;
        
        void Start()
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var r in _renderers)
            {
                if (r.isVisible)
                {
                    visible = true;
                    return;
                }
            }

            visible = false;
        }
    }
}