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
using UnityEngine.SceneManagement;
using VRSYS.Recording.Scripts;

namespace Vrsys
{
    public class Utility
    {

        public static GameObject FindRecursiveInScene(Scene scene, string name)
        {
            var sceneRoots = scene.GetRootGameObjects();

            GameObject result = null;
            foreach (var root in sceneRoots)
            {
                if (root.name.Equals(name)) return root;

                result = FindRecursive(root, name);

                if (result) break;
            }

            return result;
        }

        public static GameObject FindRecursiveInScene(string name, Scene? scn = null)
        {
            Scene scene;

            if (scn == null)
                scene = SceneManager.GetActiveScene();
            else
                scene = (Scene)scn;

            var sceneRoots = scene.GetRootGameObjects();

            GameObject result = null;
            foreach (var root in sceneRoots)
            {
                if (root.name.Equals(name)) return root;

                result = FindRecursive(root, name);

                if (result) return result;
            }
            
            sceneRoots = DontDestroySceneAccessor.Instance.GetAllRootsOfDontDestroyOnLoad();

            result = null;
            foreach (var root in sceneRoots)
            {
                if (root.name.Equals(name)) return root;

                result = FindRecursive(root, name);

                if (result) return result;
            }
            
            return null;
        }

        public static GameObject FindRecursive(GameObject entryGO, string name)
        {
            GameObject result = null;            
            foreach (Transform child in entryGO.transform)
            {
                if (child.name.Equals(name))
                    return child.gameObject;

                result = FindRecursive(child.gameObject, name);

                if (result != null)
                    break;
            }
            return result;
        }


        public static Color ParseColorFromPrefs()
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
                default: color = new Color(0.6f, 0.6f, 0.6f); break;
            }
            return color;
        }

        public static int LayermaskToLayer(LayerMask layerMask)
        {
            int layerNumber = 0;
            int layer = layerMask.value;
            while (layer > 0)
            {
                layer = layer >> 1;
                layerNumber++;
            }
            return layerNumber - 1;
        }
    }
}