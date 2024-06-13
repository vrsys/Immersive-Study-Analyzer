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
