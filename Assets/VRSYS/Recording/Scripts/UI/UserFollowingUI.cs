using Photon.Pun;
using UnityEngine;
using Vrsys;

public enum BodyPart
{
    HandLeft, HandRight, Head
}

[RequireComponent(typeof(Canvas))]
public class UserFollowingUI : MonoBehaviourPunCallbacks, IPunObservable
{

    public BodyPart attachedTo;
    public Vector3 offset;
    [SerializeField]
    public float UIScale;

    private Canvas _canvas;
    private GameObject parent = null;
    
    // Start is called before the first frame update
    void Start()
    {
        _canvas = GetComponent<Canvas>();
        
        photonView.ObservedComponents.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(_canvas.renderMode != RenderMode.WorldSpace)
            _canvas.renderMode = RenderMode.WorldSpace;

        if(!photonView.IsMine)
            return;
        
        if (parent == null && PhotonNetwork.InRoom)
        {
            if (attachedTo == BodyPart.HandLeft)
                parent = ((ViewingSetupHMDAnatomy)NetworkUser.localNetworkUser.viewingSetupAnatomy).leftController;
            else if (attachedTo == BodyPart.HandRight)
                parent = ((ViewingSetupHMDAnatomy)NetworkUser.localNetworkUser.viewingSetupAnatomy).rightController;
            else if (attachedTo == BodyPart.Head)
                parent = NetworkUser.localHead;
        }

        if (parent != null)
        {
            Transform parentTransform = parent.transform;
            Transform uiParentTransform = transform;

            uiParentTransform.position = parentTransform.position;
            Vector3 rot = parentTransform.rotation.eulerAngles;
            uiParentTransform.rotation = Quaternion.Euler(15.0f, rot.y, 0.0f);
            uiParentTransform.position += offset.x * transform.TransformDirection(Vector3.right) + offset.y * transform.TransformDirection(Vector3.up) + 
            offset.z * transform.TransformDirection(Vector3.forward);
            uiParentTransform.localScale = Vector3.one * UIScale;
            //uiParentTransform.LookAt(NetworkUser.localHead.transform);
            
        }

    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.transform.position);
            stream.SendNext(transform.transform.rotation.eulerAngles);
            stream.SendNext(transform.localScale);
        }
        else if (stream.IsReading)
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = Quaternion.Euler((Vector3)stream.ReceiveNext());
            transform.localScale = (Vector3)stream.ReceiveNext();
        }
    }
}
