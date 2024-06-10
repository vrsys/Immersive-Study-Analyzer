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