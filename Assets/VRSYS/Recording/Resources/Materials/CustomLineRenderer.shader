Shader "Unlit/CustomLineRenderer"
{
	// see : https://stackoverflow.com/a/75716886/12386571
	Properties
	{
		[MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 0.2)
		_VertexColorMap("Vertex Color Map", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
		
		ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
	
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				uint vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Interpolators {
                float4 color : COLOR;
                float4 vertex  : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
            };
			
			sampler2D _VertexColorMap;

			CBUFFER_START(UnityPerMaterial)
                float4 _VertexColorMap_ST;
                float4 _ColorTint;
                float4 _VertexColorMap_TexelSize;
            CBUFFER_END
			
			Interpolators vert (appdata v)
			{
				Interpolators o;

            	UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            	
				float2 uv = float2((v.vertexID == 0) ? _VertexColorMap_TexelSize.x : v.vertexID * _VertexColorMap_TexelSize.x, 0.5);
            	o.color = tex2Dlod(_VertexColorMap, float4(uv.xy, 0.0f, 0.0f));
            	o.vertex = UnityObjectToClipPos(v.vertex.xyz);
				
				return o;
			}
			
			fixed4 frag (Interpolators i) : SV_Target
			{
				float4 c = i.color * _ColorTint;
				c.w = 0.25;
				return c;
			}
			ENDCG
		}
	}
}