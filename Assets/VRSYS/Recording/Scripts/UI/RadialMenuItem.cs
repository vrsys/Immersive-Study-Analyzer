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

using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;


[Serializable]
public class StateEvent : UnityEvent<int>
{
}

[Serializable]
public class FloatEvent : UnityEvent<float>
{
}

[Serializable]
public class TextEvent : UnityEvent<TextMeshProUGUI>
{
}

[Serializable]
public class RadialSliderEvent : UnityEvent<UnityEngine.UI.Extensions.RadialSlider>
{
}

[ExecuteAlways]
public class RadialMenuItem : MonoBehaviourPun
{
    public string description;
    public Color color;
    public Color inactiveColor;
    public Color activeColor;
    public RectTransform iconRectTransform;
    public List<Sprite> icons = new List<Sprite>();
    [Range(0, 360)] public float childrenAngularWidth = 360.0f;
    public float childRadius = 100.0f;
    public Transform children;
    public RadialRenderer renderer;
    public float floatValue = 0.0f;
    public float floatIncreaseStep = 0.1f;
    public float maxFloatValue = 1.0f;
    public bool visualizeText = true;
    
    public int id = -1;
    
    [SerializeField] private StateEvent stateEvent = new StateEvent();
    [SerializeField] private UnityEvent OnClickEvent = new UnityEvent();
    [SerializeField] private TextEvent textEvent = new TextEvent();
    [SerializeField] private FloatEvent floatEvent = new FloatEvent();
    [SerializeField] private RadialSliderEvent radialSliderEvent = new RadialSliderEvent();

    public UnityEvent OnClickEvents
    {
        get { return OnClickEvent; }
        set { OnClickEvent = value; }
    }

    public StateEvent stateEvents
    {
        get { return stateEvent; }
        set { stateEvent = value; }
    }

    public TextEvent textEvents
    {
        get { return textEvent; }
        set { textEvent = value; }
    }

    public FloatEvent floatEvents
    {
        get { return floatEvent; }
        set { floatEvent = value; }
    }
    
    public RadialSliderEvent radialSliderEvents
    {
        get { return radialSliderEvent; }
        set { radialSliderEvent = value; }
    }

    private RadialMenuManager _manager;
    private int _itemId;

    private RectTransform _rectTransform;
    private int _totalItemCount;
    private int _iconSize = 100;
    private float _radius;
    private float _borderRadius;
    private float _angleWidth;
    public float _angle;
    private int _lastItemCount;
    private bool _hasChildItems = false;
    private RadialMenuItem _parentItem;
    private List<RadialMenuItem> _siblings = new List<RadialMenuItem>();
    private List<RadialMenuItem> _children = new List<RadialMenuItem>();
    private bool _activeState = true;
    private int _currentActiveIcon = 0;
    private bool _registered = false;
    private bool _childrenActive = false;
    private UnityEngine.UI.Extensions.RadialSlider _radialSlider;
    private Color _lastColor;
    private Sprite _lastIcon;

    // Start is called before the first frame update
    void Start()
    {
        _manager = GetComponentInParent<RadialMenuManager>();

        if (_manager != null && !_registered)
            _registered = _manager.Register(this);

        _rectTransform = GetComponent<RectTransform>();

        if (children != null)
            children.gameObject.SetActive(_childrenActive);

        if (textEvent != null && renderer != null)
            textEvent.Invoke(renderer.GetTextMesh());

        _radialSlider = GetComponentInChildren<UnityEngine.UI.Extensions.RadialSlider>(true);
    }

    private void OnDisable()
    {
        renderer.SetTextState(false);
        SetChildItemVisibility(false);
    }

    //private void OnDestroy()
    //{
    //    if (_manager != null && _registered)
    //    {
    //        _manager.Deregister(this);
    //    }
    //}

    public void Initialize(RadialMenuItem parent, List<RadialMenuItem> siblings, int itemId, int totalItems,
        float radius, float borderRadius, Material uiMaterial, int iconSize)
    {
        _parentItem = parent;
        _siblings = siblings;

        _iconSize = iconSize;
        _itemId = itemId;
        _totalItemCount = totalItems;
        _radius = radius;
        _borderRadius = borderRadius;

        if (_parentItem != null)
        {
            _angleWidth = (_parentItem.GetChildAngularWidth() / totalItems) * Mathf.Deg2Rad;
            //90 * (i - 2) + 0 + 180f;
            
            _angle = _angleWidth * (itemId - 0.5f * totalItems) + _parentItem.GetAngle() + 0.5f * _angleWidth;
            _parentItem.RegisterChild(this);
            id = _parentItem.id * 10 + itemId;
        }
        else
        {
            _angleWidth = (360.0f / totalItems) * Mathf.Deg2Rad;
            _angle = _angleWidth * itemId;
            id = itemId + 1;
        }

        
        if (_rectTransform == null || _manager == null)
        {
            _manager = GetComponentInParent<RadialMenuManager>();

            if (_manager != null && !_registered)
                _registered = _manager.Register(this);

            _rectTransform = GetComponent<RectTransform>();
        }
        
        _rectTransform.position = _manager.GetComponent<RectTransform>().position;
        _lastColor = color;

        List<RadialMenuItem> items = new List<RadialMenuItem>();
        if (children != null)
        {
            foreach (Transform child in children)
            {
                RadialMenuItem item = child.GetComponent<RadialMenuItem>();
                if (item != null)
                    items.Add(item);
            }

            if (items.Count != _lastItemCount)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    _hasChildItems = true;
                    items[i].Initialize(this, items, i, items.Count, _radius + childRadius, _radius, uiMaterial, iconSize);
                    items[i].enabled = false;
                    items[i].enabled = true;
                }

                _lastItemCount = items.Count;
            }
        }

        renderer.Initialize(this, uiMaterial);
        if (icons.Count > _currentActiveIcon)
            renderer.SetIcon(icons[_currentActiveIcon], _angle, iconSize);

        if (textEvent != null && renderer != null)
            textEvent.Invoke(renderer.GetTextMesh());
    }

    // Update is called once per frame
    void Update()
    {
        if (renderer != null)
        {
            if (_lastColor != color && renderer)
            {
                renderer.SetColor(color);
                _lastColor = color;
            }

            if (icons.Count > _currentActiveIcon && icons[_currentActiveIcon] != _lastIcon)
            {
                renderer.SetIcon(icons[_currentActiveIcon], _angle, _iconSize);
                _lastIcon = icons[_currentActiveIcon];
            }
        }
    }
    
    public void RegisterChild(RadialMenuItem child)
    {
        _children.Add(child);
    }


    public void ModifyFloat()
    {
        floatValue = (floatValue + floatIncreaseStep);
        if (floatValue > maxFloatValue)
            floatValue = 0.0f;

        if (textEvent != null && renderer != null)
            textEvent.Invoke(renderer.GetTextMesh());
        if (floatEvent != null)
            floatEvent.Invoke(floatValue);
    }

    public void SwitchState()
    {
        int newIconId = (_currentActiveIcon + 1) % icons.Count;

        if (icons.Count > newIconId)
        {
            _currentActiveIcon = newIconId;
            renderer.SetIcon(icons[_currentActiveIcon], _angle, _iconSize);
        }
        else
            Debug.LogWarning("No icon for state change set! State change might not be understandable for user!");


        if (stateEvents != null)
            stateEvents.Invoke(_currentActiveIcon);
        if (textEvent != null && renderer != null)
            textEvent.Invoke(renderer.GetTextMesh());
        if (floatEvent != null)
            floatEvent.Invoke(floatValue);
    }

    public void ToggleChildItemVisibility()
    {
        if (children != null)
        {
            _childrenActive = !_childrenActive;
            SetChildItemVisibility(_childrenActive);
        }
    }

    public void ActivateChildItemVisibility()
    {
        if (children != null && !_childrenActive)
            SetChildItemVisibility(true);
    }

    public void ToggleChildItemVisibilityCenter()
    {
        if (children != null && !_childrenActive)
            SetChildItemVisibility(true);
        else if(children != null && AllChildrenActive())
            SetChildItemVisibility(false);
    }
    
    public void ActivateSiblings()
    {
        if (_siblings != null)
        {
            for (int i = 0; i < _siblings.Count; ++i)
               _siblings[i].SetActiveState(true);
        }
    }
    
    public void ActivateParentSiblings()
    {
        if (_parentItem != null)
        {
            _parentItem.ActivateSiblings();
        }
    }

    public void ToggleSiblingAndItemVisibility()
    {
        if (_parentItem != null)
        {
            _parentItem.ToggleChildItemVisibility();
            _parentItem.ActivateSiblings();
        }
    }

    public bool AllChildrenActive()
    {
        bool active = false;
        
        if (children != null)
        {
            active = true;
            foreach (Transform child in children)
            {
                RadialMenuItem childItem = child.gameObject.GetComponent<RadialMenuItem>();
                if (!childItem._activeState)
                {
                    active = false;
                    break;
                }
            }
        }

        return active;
    }
    
    public void SetChildItemVisibility(bool state)
    {
        if (children != null)
        {
            if(_manager != null)
                _manager.ChildrenActivatedState(id, state);
            
            children.gameObject.SetActive(state);
            _childrenActive = state;

            for (int i = 0; i < _children.Count; ++i)
                _children[i].SetActiveState(state);
        }
    }

    public void ToggleSiblingItemState()
    {
        if (_siblings != null)
        {
            for (int i = 0; i < _siblings.Count; ++i)
            {
                if (_siblings[i] != this)
                {
                    _siblings[i].ToggleActiveState();
                }
            }
        }
    }

    public void DisableSiblingChildItemVisiblity()
    {
        if (_siblings != null)
        {
            for (int i = 0; i < _siblings.Count; ++i)
            {
                if (_siblings[i] != this)
                {
                    _siblings[i].SetChildItemVisibility(false);
                }
            }
        }
    }

    public void SetText(string t)
    {
        renderer.SetText(t);
        renderer.SetTextState(true);
    }

    public int GetItemId()
    {
        return _itemId;
    }

    public float GetAngle()
    {
        return _angle;
    }

    public float GetChildAngularWidth()
    {
        return childrenAngularWidth;
    }

    public void ToggleActiveState()
    {
        _activeState = !_activeState;

        SetActiveState(_activeState);
    }
    
    public void SetActiveState(bool state)
    {
        _activeState = state;

        if(_manager != null)
            _manager.ItemActiveState(id, state);
        
        if (_activeState)
        {
            renderer.SetColor(color);
            renderer.SetButtonState(true);
        }
        else
        {
            renderer.SetColor(inactiveColor);
            renderer.SetButtonState(false);
        }
    }
    
    public float GetRadius()
    {
        return _radius;
    }

    public float GetBorderRadius()
    {
        return _borderRadius;
    }

    public float GetAngleWidth()
    {
        return _angleWidth;
    }

    public int GetItemCount()
    {
        return _totalItemCount;
    }

    public string GetDescription()
    {
        return description;
    }

    public UnityEvent GetOnClickEvent()
    {
        return OnClickEvent;
    }

    public RadialMenuManager GetManager()
    {
        _manager = GetComponentInParent<RadialMenuManager>();
        return _manager;
    }

    public void UpdateRadialSliderValues()
    {
        Debug.Log("Update radial slider values!");
        if (_radialSlider == null)
            _radialSlider = GetComponentInChildren<UnityEngine.UI.Extensions.RadialSlider>(true);
        if (radialSliderEvent != null)
            radialSliderEvent.Invoke(_radialSlider);
    }
}