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

namespace VRSYS.Scripts.Recording
{
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// see : https://stackoverflow.com/a/75716886/12386571
public struct LineRendererPositionParams
{
    public Vector3 Position { get; }
    public Color32 InColor { get; }
    public Color32 OutColor { get; }

    public LineRendererPositionParams(
        Vector3 position,
        Color32 inColor,
        Color32 outColor)
    {
        Position = position;
        InColor = inColor;
        OutColor = outColor;
    }
}


public static class LineRendererExtensions
{
    public static List<Color32> SetPositionsWithColors(this LineRenderer @this, List<LineRendererPositionParams> positionParams)
    {
        @this.positionCount = positionParams.Count;
        @this.SetPositions(positionParams.Select(p => p.Position).ToArray());

        var colors = @this.GetEndCapColors(positionParams[0].InColor);

        for (var i = 1; i < positionParams.Count-1; i++)
            colors.AddRange(@this.GetSegmentConnectionColors(positionParams[i]));

        colors.AddRange(@this.GetEndCapColors(positionParams[positionParams.Count-1].InColor));

        return colors;
    }

    public static void SetVertexColorMap(this LineRenderer @this, Color32[] colors, Texture2D texture)
    {
        if (texture.width != colors.Length)
            texture.Reinitialize(colors.Length, 1);

        texture.SetPixels32(colors.ToArray());
        texture.Apply();
        @this.material.SetTexture("_VertexColorMap", texture);
    }

    private static List<Color32> GetSegmentConnectionColors(this LineRenderer @this, LineRendererPositionParams positionParams)
    {
        Debug.Assert(@this.numCornerVertices > 1,
            $"The number of corner vertices of a {nameof(LineRenderer)} cannot be less than 1 when using {nameof(LineRendererPositionParams)}");

        var colors = new List<Color32>();
        var half = (@this.numCornerVertices/2) * 2 + 2;

        for (var i = 0; i < half; i++)
            colors.Add(positionParams.InColor);

        if (@this.numCornerVertices%2 != 0)
            colors.AddRange(@this.GetMidPointColors(positionParams));

        for (var i = 0; i < half; i++)
            colors.Add(positionParams.OutColor);

        return colors;
    }

    private static List<Color32> GetMidPointColors(this LineRenderer @this, LineRendererPositionParams positionParams)
    {
        var color = Color32.Lerp(positionParams.InColor, positionParams.OutColor, 0.5f);
        return new List<Color32>() {color, color};
    }

    private static List<Color32> GetEndCapColors(this LineRenderer @this, Color32 color)
    {
        if (@this.numCapVertices == 0)
            return new List<Color32>() {color, color};

        var colors = new List<Color32>() {color, color, color, color, color, color};
        var remaining = (@this.numCapVertices - 1) * 2 + 2;

        for (var i = 0; i < remaining; i++)
            colors.Add(color);

        return colors;
    }
}
}