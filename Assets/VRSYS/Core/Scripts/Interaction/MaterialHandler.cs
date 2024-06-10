using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public enum ObjectState
{
    Default = 0,
    Hovered = 1,
    Selected = 2
}

public class MaterialHandler : MonoBehaviourPunCallbacks
{
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material hoverMaterial;

    public bool applyToChildren = false;

    public List<MeshRenderer> targets = new List<MeshRenderer>();

    private MeshRenderer[] meshRenderers;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private Dictionary<int, bool> selectingPlayer;
    private Dictionary<int, bool> hoveringPlayer;
    private Dictionary<int, Material> originalMaterials;
    private ObjectState state;
    private bool annotated;

    // Start is called before the first frame update
    void Start()
    {
        selectingPlayer = new Dictionary<int, bool>();
        hoveringPlayer = new Dictionary<int, bool>();
        originalMaterials = new Dictionary<int, Material>();

        if (originalMaterial == null)
        {
            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                originalMaterial = renderer.material;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

            if (skinnedMeshRenderer != null)
            {
                originalMaterial = skinnedMeshRenderer.material;
            }
        }

        state = ObjectState.Default;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ChangeToHoverMaterial()
    {
        if(!annotated)
            ApplyHoverMaterial(PhotonNetwork.LocalPlayer.ActorNumber);
        //photonView.RPC("ApplyHoverMaterial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void ChangeToSelectedMaterial()
    {
        if(!annotated)
            ApplySelectedMaterial(PhotonNetwork.LocalPlayer.ActorNumber);
        //photonView.RPC("ApplySelectedMaterial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void ChangeToAnnotationMaterial(Color color)
    {
        annotated = true;
        ApplyAnnotationMaterial(PhotonNetwork.LocalPlayer.ActorNumber, color);
        //photonView.RPC("ApplySelectedMaterial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    public void EndAnnotation()
    {
        annotated = false;
        ApplyOriginalMaterial(PhotonNetwork.LocalPlayer.ActorNumber);
        //photonView.RPC("ApplySelectedMaterial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    public void ResetMaterial()
    {
        if(!annotated)
            ApplyOriginalMaterial(PhotonNetwork.LocalPlayer.ActorNumber);
        //photonView.RPC("ApplyOriginalMaterial", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    private void ApplyMaterial(Material m, bool original)
    {
        if (TryGetComponent(out MeshRenderer meshRenderer))
        {
            if (!original && !originalMaterials.ContainsKey(gameObject.GetInstanceID()))
            {
                originalMaterials[gameObject.GetInstanceID()] = meshRenderer.material;
            }

            if (m != null && meshRenderer.name != "AnalysisPreview")
                meshRenderer.material = m;
        }

        if (TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
        {
            if (!original && !originalMaterials.ContainsKey(gameObject.GetInstanceID()))
            {
                originalMaterials[gameObject.GetInstanceID()] = skinnedMeshRenderer.material;
            }

            if (m != null && skinnedMeshRenderer.name != "AnalysisPreview")
                skinnedMeshRenderer.material = m;
        }

        if (applyToChildren)
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (!original && !originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    originalMaterials[renderer.gameObject.GetInstanceID()] = renderer.material;
                    if (m != null && renderer.name != "AnalysisPreview")
                        renderer.material = m;
                }
                else if (!original)
                {
                    if (m != null && renderer.name != "AnalysisPreview")
                        renderer.material = m;
                }
                else if (original && originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    renderer.material = originalMaterials[renderer.gameObject.GetInstanceID()];
                }
            }

            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                if (!original && !originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    originalMaterials[renderer.gameObject.GetInstanceID()] = renderer.material;
                    if (m != null && skinnedMeshRenderer.name != "AnalysisPreview")
                        renderer.material = m;
                }
                else if (!original)
                {
                    if (m != null && skinnedMeshRenderer.name != "AnalysisPreview")
                        renderer.material = m;
                }
                else if (original && originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    renderer.material = originalMaterials[renderer.gameObject.GetInstanceID()];
                }
            }
        }

        if (targets.Count > 0)
        {
            foreach (MeshRenderer renderer in targets)
                if (m != null  && renderer.name != "AnalysisPreview")
                    renderer.material = m;
        }
    }

    private void ApplyMaterial(Material m, bool original, Color color)
    {
        if (TryGetComponent(out MeshRenderer meshRenderer))
        {
            if (!original && !originalMaterials.ContainsKey(gameObject.GetInstanceID()))
            {
                originalMaterials[gameObject.GetInstanceID()] = meshRenderer.material;
            }

            if (m != null && meshRenderer.name != "AnalysisPreview")
                meshRenderer.material = m;
        }

        if (TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
        {
            if (!original && !originalMaterials.ContainsKey(gameObject.GetInstanceID()))
            {
                originalMaterials[gameObject.GetInstanceID()] = skinnedMeshRenderer.material;
            }

            if (m != null && skinnedMeshRenderer.name != "AnalysisPreview")
                skinnedMeshRenderer.material = m;
        }

        if (applyToChildren)
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (!original && !originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    originalMaterials[renderer.gameObject.GetInstanceID()] = renderer.material;
                    if (m != null && renderer.name != "AnalysisPreview")
                    {
                        renderer.material = m;
                        renderer.material.color = color;
                    }
                }
                else if (!original)
                {
                    if (m != null && renderer.name != "AnalysisPreview")
                    {
                        renderer.material = m;
                        renderer.material.color = color;
                    }
                }
                else if (original && originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    renderer.material = originalMaterials[renderer.gameObject.GetInstanceID()];
                }
            }

            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                if (!original && !originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    originalMaterials[renderer.gameObject.GetInstanceID()] = renderer.material;
                    if (m != null && skinnedMeshRenderer.name != "AnalysisPreview")
                    {
                        renderer.material = m;
                        renderer.material.color = color;
                    }
                }
                else if (!original)
                {
                    if (m != null && skinnedMeshRenderer.name != "AnalysisPreview")
                    {
                        renderer.material = m;
                        renderer.material.color = color;
                    }
                }
                else if (original && originalMaterials.ContainsKey(renderer.gameObject.GetInstanceID()))
                {
                    renderer.material = originalMaterials[renderer.gameObject.GetInstanceID()];
                }
            }
        }

        if (targets.Count > 0)
        {
            foreach (MeshRenderer renderer in targets)
                if (m != null && renderer.name != "AnalysisPreview")
                {
                    renderer.material = m;
                    renderer.material.color = color;
                }
        }
    }
    [PunRPC]
    public void ApplyHoverMaterial(int playerID)
    {
        if (hoverMaterial != null)
        {
            hoveringPlayer[playerID] = true;
            ApplyMaterial(hoverMaterial, false);
            state = ObjectState.Hovered;
        }
    }

    [PunRPC]
    public void ApplySelectedMaterial(int playerID)
    {
        selectingPlayer[playerID] = true;
        ApplyMaterial(selectedMaterial, false);
        state = ObjectState.Selected;
    }
    
    
    [PunRPC]
    public void ApplyAnnotationMaterial(int playerID, Color color)
    {
        selectingPlayer[playerID] = true;
        ApplyMaterial(selectedMaterial, false, color);
        state = ObjectState.Selected;
    }

    [PunRPC]
    public void ApplyOriginalMaterial(int playerID)
    {
        selectingPlayer[playerID] = false;
        hoveringPlayer[playerID] = false;

        bool reset = true;

        foreach (KeyValuePair<int, bool> playerSelecting in selectingPlayer)
        {
            if (playerSelecting.Value)
            {
                reset = false;
                break;
            }
        }

        foreach (KeyValuePair<int, bool> playerHovering in hoveringPlayer)
        {
            if (playerHovering.Value)
            {
                reset = false;
                break;
            }
        }

        if (reset)
        {
            ApplyMaterial(originalMaterial, true);
            state = ObjectState.Default;
        }
    }

    #region RecordingReplayFunctionality

    public int GetMaterialId()
    {
        return (int)state;
    }

    public void SetByMaterialId(int state)
    {
        if (state >= 0 && state <= 2)
        {
            if (state == (int)ObjectState.Selected)
            {
                ApplyMaterial(selectedMaterial, false);
            }
            else if (state == (int)ObjectState.Hovered)
            {
                ApplyMaterial(hoverMaterial, false);
            }
            else if (state == (int)ObjectState.Default)
            {
                ApplyMaterial(originalMaterial, true);
            }

            this.state = (ObjectState)state;
        }
    }

    #endregion
}