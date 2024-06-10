using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRSYS.Recording.Scripts.Analysis
{
    [CanSelectMultiple(false)]
    public class HoverInformation : XRBaseInteractable
    {
        protected override void Awake()
        {
            base.Awake();
            string[] layer = new[] { "UI", "Everything", "Objects"};
            interactionLayers = InteractionLayerMask.GetMask(layer);
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }

        protected override void OnHoverEntering(HoverEnterEventArgs args)
        {
            
        }
        
        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            //base.OnHoverEntered(args);
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    
        protected override void OnHoverExiting(HoverExitEventArgs args)
        {
            
        }
        
        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            //base.OnHoverExited(args);
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            
        }

    }
}
