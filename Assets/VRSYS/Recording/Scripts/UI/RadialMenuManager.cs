using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Vrsys;
using Vrsys.Scripts.Recording;

[ExecuteAlways]
public class RadialMenuManager : MonoBehaviourPun
{
    public float radius = 500.0f;
    public float borderRadius = 250.0f;
    public int iconSize = 150;
    public Material uiMaterial;
    public InputActionProperty toggleMenu;
    
    private int lastItemCount = 0;
    private RectTransform _rectTransform;
    private bool _cameraInitialized = false;
    private Dictionary<GameObject, RadialMenuItem> _radialMenuItems = new Dictionary<GameObject, RadialMenuItem>();
    private Dictionary<int, RadialMenuItem> _radialMenuItemsInt = new Dictionary<int, RadialMenuItem>();
    private List<IEnumerator> coroutines = new List<IEnumerator>();
    private bool _menuActive, _initialized = false;
    
    
    // Start is called before the first frame update
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        lastItemCount = 0;
        toggleMenu.action.Enable();
    }

    private void OnDestroy()
    {
        _radialMenuItems.Clear();
        _radialMenuItemsInt.Clear();
    }

    public bool Register(RadialMenuItem item)
    {
        if (!_radialMenuItems.ContainsKey(item.gameObject))
        {
            _radialMenuItemsInt[item.id] = item;
            _radialMenuItems[item.gameObject] = item;
            return true;
        }
        else
        {
            if (!_radialMenuItemsInt.ContainsKey(item.id))
                _radialMenuItemsInt[item.id] = item;
            
            Debug.LogError("Item already registered!");
            return false;
        }
    }
    
    public void Deregister(RadialMenuItem item)
    {
        if (_radialMenuItems.ContainsKey(item.gameObject))
        {
            _radialMenuItems.Remove(item.gameObject);
            _radialMenuItemsInt.Remove(item.id);
        }
    }
    
    public void Initialize()
    {
        List<RadialMenuItem> items = new List<RadialMenuItem>();
        foreach (Transform child in transform)
        {
            RadialMenuItem item = child.GetComponent<RadialMenuItem>();
            if (item != null)
                items.Add(item);
        }
        
        if (items.Count != lastItemCount)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                items[i].Initialize(null, items, i, items.Count, radius, borderRadius, uiMaterial, iconSize);
            }

            lastItemCount = items.Count;
        }

        for (int i = 0; i < coroutines.Count; ++i)
        {
            StartCoroutine(coroutines[i]);
        }
    }
    
    public void ItemActiveState(int id, bool state)
    {
        if(photonView != null && photonView.IsMine)
            photonView.RPC(nameof(ItemActiveStateRPC), RpcTarget.Others, id, state);
    }

    [PunRPC]
    public void ItemActiveStateRPC(int id, bool state)
    {
        if (_radialMenuItemsInt.ContainsKey(id))
        {
            _radialMenuItemsInt[id].SetActiveState(state);
        } else if (id == 1 && _radialMenuItemsInt.ContainsKey(0))
            _radialMenuItemsInt[0].SetActiveState(state);
    }

    public void ChildrenActivatedState(int id, bool state)
    {
        if(photonView != null && photonView.IsMine)
            photonView.RPC(nameof(ChildrenActivatedStateRPC), RpcTarget.Others, id, state);
    }
    
    [PunRPC]
    public void ChildrenActivatedStateRPC(int id, bool state)
    {
        if (_radialMenuItemsInt.ContainsKey(id))
        {
            _radialMenuItemsInt[id].SetChildItemVisibility(state);
        } else if (id == 1 && _radialMenuItemsInt.ContainsKey(0))
            _radialMenuItemsInt[0].SetChildItemVisibility(state);
    }
    
    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (_radialMenuItems.Count != lastItemCount)
        {
            Initialize();
            lastItemCount = _radialMenuItems.Count;
        }
        #endif
        
        if (PhotonNetwork.InRoom)
        {
            if (!_cameraInitialized)
            {
                Canvas canvas = GetComponent<Canvas>();
                AvatarHMDAnatomy anatomy = NetworkUser.localNetworkUser.GetComponent<AvatarHMDAnatomy>();
                if(anatomy != null)
                    canvas.worldCamera = anatomy.head.GetComponentInParent<Camera>();
                
                _cameraInitialized = true;
            }

            if (!_initialized)
            {
                Invoke(nameof(Initialize), 5.0f);
                _initialized = true;
            }

            if (photonView != null && photonView.IsMine && toggleMenu.action.triggered)
                SetRadialMenuState(!_menuActive);

        }
    }

    public void SetRadialMenuState(bool state)
    {
        if(photonView != null && photonView.IsMine)
            photonView.RPC(nameof(RadialMenuStateRPC), RpcTarget.All,  state);
    }
    
    [PunRPC]
    public void RadialMenuStateRPC(bool state)
    {
        _menuActive = state;
        foreach (Transform child in transform)
        {
            RadialMenuItem item = child.GetComponent<RadialMenuItem>();
            if (item != null)
            {
                item.gameObject.SetActive(_menuActive);
                if(_menuActive)
                    item.SetChildItemVisibility(true);
            }
        }
    }

    public void AddCoroutine(IEnumerator coroutine)
    {
        coroutines.Add(coroutine);
    }
}
