using System;
using UnityEngine;

namespace VRSYS.Scripts.Recording
{

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ParentRectTransformGetSet : MonoBehaviour
    {
        private RectTransform _rect, _parentRect;
        public void Start()
        {
            if (transform.parent != null)
            {
                _parentRect = transform.parent.GetComponent<RectTransform>();
            }

            _rect = GetComponent<RectTransform>();
        }

        public void Update()
        {
            if (_parentRect != null)
            {
                _rect.sizeDelta = _parentRect.sizeDelta;
                _rect.position = _parentRect.position;
            }
        }
    }
}