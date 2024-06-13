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

using UnityEngine;

namespace VRSYS.Scripts.Recording
{
    public abstract class Recorder : MonoBehaviour
    {
        public RecorderController controller;
        
        public RecorderController Controller
        {
            get => controller;

            set { controller = value; RegisterRecorder(); }
        }
        
        public bool registered = false;
        protected int id = 99999;
        protected int recorderId = 99999;
        public bool preview = false;
        public bool portal = false;

        protected AudioClip _soundEffect;
        protected Material _trailMaterial;

        protected float _lastReplayTime;
        
        public virtual void Start()
        {
            if(id == 99999)
                id = gameObject.GetInstanceID();
            
            if (controller != null && !registered)
                RegisterRecorder();
            
            _soundEffect = Resources.Load<AudioClip>("Audio/ambient");
            _trailMaterial = Resources.Load<Material>("Textures/Trail");
        }

        public void OnDestroy()
        {
            DeregisterRecorder();
        }

        public virtual bool Record(float recordTime)
        {
            return false;
        }

        public virtual bool Replay(float replayTime)
        {
            return false;
        }
        
        public virtual bool Preview(float previewTime)
        {
            return false;
        }

        public void RegisterRecorder()
        {
            if (id == 99999)
            {
                Debug.LogError("Error! Id not correctly set!");
            }

            controller.RegisterRecorder(id, this);
            recorderId = controller.RecorderID;
            registered = true;
        }

        public void DeregisterRecorder()
        {
            if(controller != null)
                controller.DeregisterRecorder(id, this);
            recorderId = 99999;
            registered = false;
        }
        
        public virtual void Update()
        {
            if (!registered && controller != null)
                RegisterRecorder();
        }

        public void MarkAsPreviewRecorder()
        {
            preview = true;
            registered = false;
            portal = false;
        }

        public void MarkAsPortalRecorder()
        {
            preview = false;
            registered = false;
            portal = true;
        }
        
        public void SetId(int customId)
        {
            id = customId;
        }
    }
}