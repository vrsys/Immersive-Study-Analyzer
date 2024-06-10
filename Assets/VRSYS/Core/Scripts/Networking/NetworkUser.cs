// VRSYS plugin of Virtual Reality and Visualization Research Group (Bauhaus University Weimar)
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
// Copyright (c) 2022 Virtual Reality and Visualization Research Group
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
//   Authors:        Ephraim Schott, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

namespace Vrsys
{
    [RequireComponent(typeof(AvatarAnatomy))]
    public class NetworkUser : MonoBehaviourPunCallbacks, IPunObservable
    {
        public string userName = "";
        public string userId = "";
        public static GameObject localGameObject;
        public static GameObject localHead;
        public static NetworkUser localNetworkUser
        {
            get
            {
                if(localGameObject != null)
                    return localGameObject.GetComponent<NetworkUser>();
                return null;
            }
        }

        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Tooltip("The viewing prefab to instantiate for the local user. For maximum support, this should contain a ViewingSetupAnatomy script at root level, which supports the AvatarAnatomy attached to gameObject.")]
        [SerializeField]
        private GameObject viewingSetup;

        [Tooltip("If true, a TMP_Text element will be searched in child components and a text will be set equal to photonView.Owner.NickName. Note, this feature may create unwanted results if the GameObject, which contains this script, holds any other TMP_Text fields but the actual NameTag.")]
        public bool setNameTagToNickname = true;

        [Tooltip("The spawn position of this NetworkUser")]
        public Vector3 spawnPosition = Vector3.zero;

        public List<string> tags = new List<string>();

        [HideInInspector]
        public AvatarAnatomy avatarAnatomy { get; private set; }

        [HideInInspector]
        public ViewingSetupAnatomy viewingSetupAnatomy { get; private set; }

        private Vector3 receivedScale = Vector3.one;
        

        private bool hasPendingScaleUpdate
        {
            get
            {
                return (transform.localScale - receivedScale).magnitude > 0.001;
            }
        }

        private void Awake()
        {
            if (photonView != null && photonView.Owner != null)
            {
                Debug.Log("A");
                userName = photonView.Owner.NickName;
                userId = photonView.Owner.UserId;
                Debug.Log("B");

                if (userName == null)
                    userName = "No Name";
                if (userId == null)
                    userId = System.Guid.NewGuid().ToString();

                Debug.Log("C");
                if (!NetworkManager.instance.userGameobjectDict.ContainsKey(userId))
                {
                    NetworkManager.instance.userGameobjectDict.Add(userId, this.gameObject);
                }
                else
                {
                    Debug.LogError("NetworkUser Error: User ID exists already.");
                }

                Debug.Log("E");
                avatarAnatomy = GetComponent<AvatarAnatomy>();
                if (avatarAnatomy != null)
                {
                    if (photonView.IsMine)
                    {
                        NetworkUser.localGameObject = gameObject;
                        NetworkUser.localHead = avatarAnatomy.head;

                        InitializeAvatar();
                        InitializeViewing();
                        //HideHandsInFavorOfControllers();
                    }

                    if (PhotonNetwork.IsConnected)
                    {
                        gameObject.name = photonView.Owner.NickName +
                                          (photonView.IsMine ? " [Local User]" : " [External User]");
                        var nameTagTextComponent = avatarAnatomy.nameTag.GetComponentInChildren<TMP_Text>();
                        if (nameTagTextComponent && setNameTagToNickname)
                        {
                            nameTagTextComponent.text = photonView.Owner.NickName;
                        }
                    }
                }

                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
            }
        }

        private void Update()
        {
            if (!photonView.IsMine && hasPendingScaleUpdate)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, receivedScale, Time.deltaTime);
            }
        }

        private void InitializeAvatar()
        {
            //avatarAnatomy.nameTag.SetActive(false);
            Color clr = ParseColorFromPrefs(new Color(.6f, .6f, .6f));
            photonView.RPC("SetColor", RpcTarget.AllBuffered, new object[] { new Vector3(clr.r, clr.g, clr.b) });
        }

        private void InitializeViewing()
        {
            //Check whcih platform is running
            if (viewingSetup == null)
            {
                throw new System.ArgumentNullException("Viewing Setup must not be null for local NetworkUser.");
            }

            viewingSetup = Instantiate(viewingSetup);
            viewingSetup.transform.position = spawnPosition;
            viewingSetup.transform.SetParent(gameObject.transform, false);
            viewingSetup.name = "Viewing Setup";

            viewingSetupAnatomy = viewingSetup.GetComponentInChildren<ViewingSetupAnatomy>();
            if (viewingSetupAnatomy)
            {
                avatarAnatomy.ConnectFrom(viewingSetupAnatomy);
            }
            else
            {
                Debug.LogWarning("Your Viewing Setup Prefab does not contain a '" + typeof(ViewingSetupAnatomy).Name + "' Component. This can lead to unexpected behavior.");
            }
        }

        private void HideHandsInFavorOfControllers()
        {
            AvatarHMDAnatomy ahmda = GetComponent<AvatarHMDAnatomy>();
            if (ahmda != null)
            {
                ahmda.handRight.SetActive(false);
                ahmda.handLeft.SetActive(false);
            }
        }

        [PunRPC]
        void SetColor(Vector3 color)
        {
            avatarAnatomy.SetColor(new Color(color.x, color.y, color.z));
        }

        [PunRPC]
        void SetName(string name)
        {
            gameObject.name = name + (photonView.IsMine ? " [Local User]" : " [External User]");
            var nameTagTextComponent = avatarAnatomy.nameTag.GetComponentInChildren<TMP_Text>();
            if (nameTagTextComponent && setNameTagToNickname)
            {
                nameTagTextComponent.text = name;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting && photonView.IsMine)
            {
                stream.SendNext(viewingSetup.transform.lossyScale);
            }
            else if (stream.IsReading)
            {
                receivedScale = (Vector3)stream.ReceiveNext();
            }
        }

        public static Color ParseColorFromPrefs(Color fallback)
        {
            Color color;
            switch (PlayerPrefs.GetString("UserColor"))
            {
                case "ColorBlack": color = new Color(.2f, .2f, .2f); break;
                case "ColorRed": color = new Color(1f, 0f, 0f); break;
                case "ColorGreen": color = new Color(0f, 1f, 0f); break;
                case "ColorBlue": color = new Color(0f, 0f, 1f); break;
                case "ColorPink": color = new Color(255f / 255f, 192f / 255f, 203 / 255f); break;
                case "ColorWhite": color = new Color(1f, 1f, 1f); break;
                default: color = fallback; break;
            }
            return color;
        }
    }
}
