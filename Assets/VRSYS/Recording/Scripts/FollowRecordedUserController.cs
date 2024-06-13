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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Vrsys.Scripts.Recording;

namespace VRSYS.Scripts.Recording
{
    public class FollowRecordedUserController
    {
        private RecorderState _state;
        
        private GameObject _localUser;
        private GameObject _lastSelectedUser;
        
        private const string _viewingSetup = "Viewing Setup";
        private const string _head = "Head";
        private const string _nameTag = "NameTag";
        private const string _camera = "Camera";
        private const string _camera2 = "Main Camera";
        private const string _leftController = "HandLeft";
        
        private GameObject _localViewingSetup;
        private GameObject _localHead;
        private GameObject _localCamera;
        private GameObject _localLeftController;
        
        private GameObject _targetViewSetup;
        private GameObject _targetHead;
        private GameObject _nameTargetTag;
        private GameObject _targetCamera;

        private GameObject _targetPrevViewSetup;
        private GameObject _targetPrevCamera;
        private GameObject _targetPrevHead;
        private GameObject _namePrevTag;
        private GameObject _targetPrevLeftController;
        
        private GameObject _previewUser;

        private GameObject _perspectiveCamera;
        private Image _perspectiveImage;
        
        private bool _selectedUserUpdated;
        private bool _isHmd;
        
        public FollowRecordedUserController(RecorderState state)
        {
            _state = state;
         
            var XRDevices = new List<InputDevice>();
            InputDevices.GetDevices(XRDevices);
            if (XRDevices.Count > 0)
                _isHmd = true;
            
            if (_state.selectableUsers.ContainsKey("Local User"))
                _localUser = _state.selectableUsers["Local User"];
            
            _lastSelectedUser = _localUser;
            
            _localViewingSetup = Utils.GetChildByName(_localUser,_viewingSetup);
            _localHead = Utils.GetChildByName(_localUser, _head);
            _localCamera = Utils.GetChildByName(_localUser, _camera);
            _localLeftController = Utils.GetChildByName(_localUser, _leftController);
            if(_localCamera == null)
                _localCamera = Utils.GetChildByName(_localUser, _camera2);
            
            _perspectiveCamera = Utils.GetChildByName(state.gameObject,"PerspectiveCamera");
            GameObject perspective = Utils.GetChildByName(state.gameObject,"Perspective");
            
            if(perspective != null)
                _perspectiveImage = perspective.GetComponent<Image>();

            _selectedUserUpdated = true;
        }
        
        public void SetLastSelectedView()
        {
            if (_lastSelectedUser != null && _lastSelectedUser != _localUser)
            {
                if (_targetPrevViewSetup != null && _targetPrevCamera != null)
                {
                    Debug.Log("Previous User: " + _lastSelectedUser.name);
                    // this is being done in order to set the correct viewpoint when an HMD is used because the
                    // active head tracking does interfere with setting the position of the camera
                    // TODO: sometimes this setup does not work! FIX!
                    Matrix4x4 Vi = _localViewingSetup.transform.worldToLocalMatrix;
                    Matrix4x4 CV = _localCamera.transform.localToWorldMatrix;
                    Matrix4x4 CpVp = _targetPrevCamera.transform.localToWorldMatrix;

                    Matrix4x4 Vn = CpVp * Matrix4x4.Inverse(Vi * CV);
                    bool valid = Vn.ValidTRS();
                    if (!valid)
                        Debug.LogError("Error! No valid TRS matrix");

                    Quaternion rot = Vn.rotation;
                    Vector4 pos4 = Vn * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                    Vector3 pos = new Vector3(pos4.x / pos4.w, pos4.y / pos4.w, pos4.z / pos4.w);

                    _localViewingSetup.transform.position = pos;
                    _localViewingSetup.transform.rotation = rot;
                }
            }
        }
        
        public void SetReplayUserView()
        {
            if(_state.currentState != State.PreviewReplay && _perspectiveImage != null)
                _perspectiveImage.enabled = false;
            
            bool followSelectedUser = true;
            
            GameObject selectedUser = _localUser;
            
            if (_lastSelectedUser != null && _selectedUserUpdated)
            {
                _targetPrevViewSetup = Utils.GetChildByName(_lastSelectedUser, _viewingSetup);
                _targetPrevHead = Utils.GetChildByName(_lastSelectedUser, _head);
                _namePrevTag = Utils.GetChildByName(_lastSelectedUser, _nameTag);
                _targetPrevCamera = Utils.GetChildByName(_lastSelectedUser, _camera);
                _targetPrevLeftController = Utils.GetChildByName(_lastSelectedUser, _leftController);
                if(_targetPrevCamera == null)
                    _targetPrevCamera = Utils.GetChildByName(_lastSelectedUser, _camera2);
            }
            
            // set the transform of the replay camera
            if (_state.selectedUser != "")
            {
                if (_state.selectableUsers.ContainsKey(_state.selectedUser))
                    selectedUser = _state.selectableUsers[_state.selectedUser];

               
                if (_state.previewNode != null && _state.previewNode.transform.childCount != 0)
                {
                    if(_previewUser == null || !_previewUser.name.Contains(selectedUser.name))
                        _previewUser = Utils.GetChildBySubstring(_state.previewNode, selectedUser.name);
                    
                    if (_previewUser != null)
                    {
                        selectedUser = _previewUser;
                        followSelectedUser = false;
                    }
                }
            }

            if (selectedUser != null)
            {
                if (_selectedUserUpdated)
                {
                    _targetViewSetup = Utils.GetChildByName(selectedUser, _viewingSetup);
                    _targetHead = Utils.GetChildByName(selectedUser, _head);
                    _nameTargetTag = Utils.GetChildByName(selectedUser, _nameTag);
                    _targetCamera = Utils.GetChildByName(selectedUser, _camera);
                    if(_targetCamera == null)
                        _targetCamera = Utils.GetChildByName(selectedUser, _camera2);

                    if (_lastSelectedUser != null)
                    {
                        if (_lastSelectedUser == _localUser)
                        {
                            if (_targetPrevViewSetup != null)
                                Utils.MakeChildrenInvisible(_targetPrevViewSetup);
                            if (_targetPrevHead != null)
                                Utils.MakeChildrenInvisible(_targetPrevHead);
                            if (_namePrevTag != null)
                                Utils.DeactivateAllChildren(_namePrevTag);
                            if (_targetPrevLeftController != null)
                                Utils.DeactivateXRInteractor(_targetPrevLeftController);
                        }
                        else
                        {
                            if (_targetPrevViewSetup != null)
                                Utils.MakeChildrenVisible(_targetPrevViewSetup);
                            if (_targetPrevHead != null)
                                Utils.MakeChildrenVisible(_targetPrevHead);
                            if (_namePrevTag != null)
                                Utils.ActivateAllChildren(_namePrevTag);
                        }

                        if (selectedUser == _localUser)
                        {
                            Utils.ActivateXRInteractor(_localLeftController);
                        }
                    }
                    
                    //if(_targetViewSetup != null && followSelectedUser)
                    //    Utils.MakeChildrenInvisible(_targetViewSetup);
                    if(_targetHead != null && followSelectedUser)
                        Utils.MakeChildrenInvisible(_targetHead);
                    
                    if(_targetViewSetup != null && selectedUser == _localUser)
                        Utils.MakeChildrenVisible(_targetViewSetup);
                    if(_targetHead != null && selectedUser == _localUser)
                        Utils.MakeChildrenVisible(_targetHead);
                    if(_nameTargetTag != null && selectedUser == _localUser)
                        Utils.DeactivateAllChildren(_nameTargetTag);
                    
                    _lastSelectedUser = selectedUser;
                }
                
                if (selectedUser != _localUser)
                {
                    if (followSelectedUser)
                    {
                        if(_selectedUserUpdated)
                            Utils.DisableUserInput(_isHmd, _localViewingSetup);
                        
                        Matrix4x4 Vi = _localViewingSetup.transform.worldToLocalMatrix;
                        Matrix4x4 CV = _localCamera.transform.localToWorldMatrix;
                        if (_targetCamera != null)
                        {
                            Matrix4x4 CpVp = _targetCamera.transform.localToWorldMatrix;

                            Matrix4x4 Vn = CpVp * Matrix4x4.Inverse(Vi * CV);
                            bool valid = Vn.ValidTRS();
                            if (!valid)
                                Debug.LogError("Error! No valid TRS matrix");
                            else
                            {
                                Quaternion rot = Vn.rotation;
                                Vector4 pos4 = Vn * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                                Vector3 pos = new Vector3(pos4.x / pos4.w, pos4.y / pos4.w, pos4.z / pos4.w);
                                _localViewingSetup.transform.position = pos;
                                _localViewingSetup.transform.rotation = rot;
                            }
                        }
                    }
                    else 
                        if(_selectedUserUpdated)
                            Utils.EnableUserInput(_isHmd, _localViewingSetup);
                }
                else
                { 
                    if(_selectedUserUpdated)
                        Utils.EnableUserInput(_isHmd, _localViewingSetup);
                }

                if (_nameTargetTag != null && followSelectedUser)
                {
                    _nameTargetTag.SetActive(false);
                }

                if (_lastSelectedUser != selectedUser)
                    _selectedUserUpdated = true;
                else
                    _selectedUserUpdated = false;

                if (_state.currentState == State.PreviewReplay)
                {
                    if (selectedUser != _localUser)
                    {
                        if(_perspectiveImage != null)
                            _perspectiveImage.enabled = true;
                        if (_targetViewSetup != null)
                            Utils.MakeChildrenVisible(_targetViewSetup);
                        if (_targetHead != null)
                            Utils.MakeChildrenVisible(_targetHead);
                        if (_nameTargetTag != null)
                            Utils.ActivateAllChildren(_nameTargetTag);
                    }
                    else
                    {
                        if (_perspectiveImage != null)
                            _perspectiveImage.enabled = false;
                    }


                    Vector3 userPos = _targetHead.transform.position;
                    Quaternion userRot = _targetHead.transform.rotation;
                    _perspectiveCamera.transform.position = userPos;
                    _perspectiveCamera.transform.rotation = userRot;
                }
            }
            
            // This has to be done because the nametag active state was recorded for the local user and was always false
            foreach (var userPerspective in _state.selectableUsers)
            {
                if (userPerspective.Value != null && userPerspective.Value != selectedUser && userPerspective.Value != _localUser)
                {
                    GameObject userNameTag = Utils.GetChildByName(userPerspective.Value, _nameTag);

                    if (userNameTag != null)
                        Utils.ActivateAllChildren(userNameTag);
                }
            }
        }
    }
}