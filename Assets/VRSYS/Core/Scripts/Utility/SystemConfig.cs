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
//   Authors:        Sebastian Muehlhaus, Andr� Kunert, Ephraim Schott, Lucky Chandrautama
//   Date:           2022
//-----------------------------------------------------------------

using System.IO;
using UnityEngine;
using UnityEditor;


/* Derive from this class and add it as a component under NetworkSetup 
 * to define custom config behavior executed prior to connecting entering a Photon room. */

namespace Vrsys
{

    public class SystemConfig : MonoBehaviour
    {
        public string systemConfigPath = "";

        public string content { get; protected set; }

        public static SystemConfig Instance { get; private set; }

        [Tooltip("For TiledWall, the vSyncCount: 0, Fullscreen Mode: ExclusiveFullScreen. For PowerWall, the vSyncCount: 1, Fullscreen Mode: Fullscreen Windowed, Default Screen Width: 4096, Height: 2160")]
        public BuildSettings buildSetting;

        public bool dontDestroyOnLoad = false;

        public enum BuildSettings
        {
            TiledWall,
            Powerwall
        }

        private void Awake()
        {
            if (Instance != null) 
                Destroy(this);
            else if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }


        private void Start()
        {
            if (Instance != null)
                return;
#if UNITY_EDITOR

            switch (buildSetting)
            {
                case BuildSettings.TiledWall:
                    QualitySettings.vSyncCount = 0;
                    PlayerSettings.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    break;

                case BuildSettings.Powerwall:
                    QualitySettings.vSyncCount = 1;
                    PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                    PlayerSettings.defaultIsNativeResolution = false;
                    PlayerSettings.defaultScreenWidth = 4096;
                    PlayerSettings.defaultScreenHeight = 2160;

                    break;
            }

#endif
            Instance = this;
            if (systemConfigPath.Length > 0)
                Read();
        }

        // override this in derived classes for custom read behavior, e.g. Json Parsing
        public virtual void Read()
        {
            Debug.Log("Read Application Config: " + systemConfigPath);
            if (!File.Exists(systemConfigPath))
            {
                //throw new FileNotFoundException(systemConfigPath);
                return;
            }

            StreamReader reader = new StreamReader(systemConfigPath);
            content = reader.ReadToEnd();
            reader.Close();

            Debug.Log("content: " + content);
        }

        public void Save(string configString)
        {
            string file = string.Format(systemConfigPath);
            string configJson = JsonUtility.ToJson(configString, true); // prettyPrint = true
            using (StreamWriter sw = File.CreateText(file))
            {
                sw.Write(configJson);
                sw.Close();
            }
            Debug.Log("Save Application Config: " + systemConfigPath);
        }
    }

}