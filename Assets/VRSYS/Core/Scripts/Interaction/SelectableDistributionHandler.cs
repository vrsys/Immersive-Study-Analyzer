using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SelectableDistributionHandler : MonoBehaviourPunCallbacks
{
    public bool isSelected = false;

    public void Select()
    {
        photonView.RPC("SelectionRPC", RpcTarget.All, true);
        photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
    }

    public void Deselect()
    {
        photonView.RPC("SelectionRPC", RpcTarget.All, false);
    }

    [PunRPC]
    public void SelectionRPC(bool isSelected)
    {
        this.isSelected = isSelected;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if(PhotonNetwork.IsMasterClient)
            photonView.RPC("SelectionRPC", newPlayer, isSelected); 
    }
}
