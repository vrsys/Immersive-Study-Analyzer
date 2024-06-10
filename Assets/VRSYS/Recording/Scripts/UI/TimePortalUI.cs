using System;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Vrsys;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    public class TimePortalUI : MonoBehaviourPun
    {
        public GameObject portal;
        public Material portalMaterial;
        private bool _cameraInitialized, _portalScaleAdjusted, _textureIntialized = false;
        private RectTransform _rectTransform;

        public void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
        
        
        public void Update()
        {
            if (PhotonNetwork.InRoom)
            {
                if (photonView.IsMine)
                {
                    if (!_cameraInitialized)
                    {
                        Canvas canvas = GetComponent<Canvas>();
                        canvas.worldCamera = Utils.GetChildCamera(NetworkUser.localNetworkUser.gameObject).GetComponent<Camera>();
                        _cameraInitialized = true;
                        
                    }

                    if (_rectTransform.localScale.x * 1.0f/100.0f <= 0.01f && !_portalScaleAdjusted)
                    {
                        var localScale = _rectTransform.localScale * 1.0f/100.0f;
                        portal.transform.localScale = Vector3.Scale(portal.transform.localScale, new Vector3(1.0f/localScale.x, 1.0f/localScale.y, 1.0f/localScale.z));
                        _portalScaleAdjusted = true;
                    }
                }
                else
                {
                    if (!_textureIntialized)
                    {
                        GameObject stereoViewGo = Utils.GetChildBySubstring(gameObject, "StereoView");
                        if (stereoViewGo != null)
                        {
                            MeshRenderer meshRenderer = stereoViewGo.GetComponent<MeshRenderer>();
                            meshRenderer.material = portalMaterial;
                        }
                        _textureIntialized = true;
                    }
                }
            }
        }
    }
}