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
//   Authors:        Sebastian Muehlhaus, André Kunert
//   Date:           2022
//-----------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vrsys
{
    public class HeadtrackingObserver : MonoBehaviour
    {
        private float measurementDuration = 5f; // in sec
        private float movementThreshold = 0.01f; // in meter

        private List<float> timeStepList;
        private List<Vector3> posStepList;

        public bool movementFlag = false;

        private GameObject infoDisplay;
        public Vector2 infoDisplayPos;

        // Start is called before the first frame update
        void Start()
        {
            timeStepList = new List<float>();
            posStepList = new List<Vector3>();

            measurementDuration = ProjectionWallSystemConfigParser.Instance.config.multiUserSettings.monoFallbackDetectionDuration;
            movementThreshold = ProjectionWallSystemConfigParser.Instance.config.multiUserSettings.monoFallbackDetectionMovementThreshold;

            infoDisplay = Utility.FindRecursive(gameObject, "HeadtrackingInfoDisplay");

            GameObject infoPanel = Utility.FindRecursive(infoDisplay, "InfoPanel");
            infoPanel.GetComponent<RectTransform>().anchoredPosition = infoDisplayPos;

            GameObject infoText = Utility.FindRecursive(infoDisplay, "InfoText");
            infoText.GetComponent<RectTransform>().anchoredPosition = infoDisplayPos;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateMovementHistory();
            DetectSignificantMovement();
        }

        private void UpdateMovementHistory()
        {
            timeStepList.Add(Time.time);
            posStepList.Add(transform.localPosition);

            int counter = 0;
            foreach (float timeStep in timeStepList)
            {
                if (Time.time - timeStep > measurementDuration)
                    counter += 1;
            }
            for (int i = 0; i < counter; i++)
            {
                timeStepList.RemoveAt(0);
                posStepList.RemoveAt(0);
            }
        }

        private void DetectSignificantMovement()
        {
            float dist = 0f;

            for (int i = 0; i < posStepList.Count; i++)
            {
                if (i > 0)
                {
                    Vector3 pos = posStepList[i];
                    Vector3 posLF = posStepList[i - 1];

                    dist += (pos - posLF).magnitude;
                }
            }

            movementFlag = dist > movementThreshold;
        }

        public void ShowInfoDisplay(bool flag)
        {
            infoDisplay.SetActive(flag);
        }
    }
}
