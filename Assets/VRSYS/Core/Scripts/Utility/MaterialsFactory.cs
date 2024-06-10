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
//   Authors:        Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------
using UnityEngine;

public class MaterialsFactory
{
    public static Material CreatePreviewMaterial()
    {
        var previewMaterial = new Material(Shader.Find("Standard"));
        previewMaterial.color = new Color(0.0f, 1f, 0.019f, 0.85f);
        previewMaterial.SetOverrideTag("RenderType", "Transparent");
        previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.DisableKeyword("_ALPHATEST_ON");
        previewMaterial.DisableKeyword("_ALPHABLEND_ON");
        previewMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return previewMaterial;
    }

    public static Material CreateFadeMaterial()
    {
        var fadeMaterial = new Material(Shader.Find("Standard"));
        fadeMaterial.color = new Color(0.0f, 0.02f, 1f, 0.4f);
        fadeMaterial.SetOverrideTag("RenderType", "Transparent");
        fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fadeMaterial.SetInt("_ZWrite", 0);
        fadeMaterial.DisableKeyword("_ALPHATEST_ON");
        fadeMaterial.DisableKeyword("_ALPHABLEND_ON");
        fadeMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        fadeMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return fadeMaterial;
    }

}
