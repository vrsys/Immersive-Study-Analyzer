Shader "MK/XRay/Free" 
{
	Properties 
	{
		//Main
		_Color("_Color", Color) = (1,1,1,1)

		//XRay
		_XRayRimColor("Rim Color", Color) = (1,1,1,1)
		_XRayInside("Inside", Range(0,1) ) = 0
	    _XRayRimSize("Rim Size", Range(0,1) ) = 0.50

		//Emission
		_EmissionColor("Emission Color", Color) = (0,0,0)

		//Editor
		[HideInInspector] _MKEditorShowMainBehavior ("Main Behavior", int) = 1
		[HideInInspector] _MKEditorShowXRayBehavior ("XRay Behavior", int) = 0
	}

	/////////////////////////////////////////////////////////////////////////////////////////////
	// SM Includes
	/////////////////////////////////////////////////////////////////////////////////////////////
	CGINCLUDE
		#include "UnityCG.cginc"

        uniform fixed4 _Color;
		uniform half4 _XRayRimColor;
		uniform half _XRayRimSize;
		uniform half _XRayInside;
			
		//Emission
		uniform fixed3 _EmissionColor;

		/////////////////////////////////////////////////////////////////////////////////////////////
		// SM Input
		/////////////////////////////////////////////////////////////////////////////////////////////
		struct Input
		{
			float4 vertex : POSITION;
			half3 normal : NORMAL;
			#if UNITY_VERSION >= 560
				UNITY_VERTEX_INPUT_INSTANCE_ID
			#endif
		};

		/////////////////////////////////////////////////////////////////////////////////////////////
		// SM Output
		/////////////////////////////////////////////////////////////////////////////////////////////
		struct Output
		{
			float4 pos : SV_POSITION;
			half3 normal : TEXCOORD0;
			#if UNITY_VERSION >= 560
				UNITY_VERTEX_INPUT_INSTANCE_ID
			#endif
			UNITY_VERTEX_OUTPUT_STEREO
		};
			
		/////////////////////////////////////////////////////////////////////////////////////////////
		// Vertex
		/////////////////////////////////////////////////////////////////////////////////////////////
		Output vert (Input v)
		{
			#if UNITY_VERSION >= 560
				UNITY_SETUP_INSTANCE_ID(v);
			#endif
			Output o;
			UNITY_INITIALIZE_OUTPUT(Output, o);
			#if UNITY_VERSION >= 560
				UNITY_TRANSFER_INSTANCE_ID(v,o);
			#endif
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			o.pos = UnityObjectToClipPos (v.vertex);

			o.normal = v.normal;

			return o;
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
		// Fragment
		/////////////////////////////////////////////////////////////////////////////////////////////
		fixed4 frag(Output o) : SV_TARGET
		{
			#if UNITY_VERSION >= 560
				UNITY_SETUP_INSTANCE_ID(o);
			#endif
			_Color.a = _XRayRimColor.a = 1.0;

			fixed4 colorOut;
			float3 uv = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, o.normal));

			_XRayInside = _XRayInside*0.25;
			half rF = 0.5;

			colorOut = lerp(half4(0,0,0,0), _Color, _XRayInside);
			colorOut.rgb = lerp(colorOut.rgb, _XRayRimColor, rF * saturate(1 - pow(uv.z, _XRayRimSize)));

			colorOut.rgb += _EmissionColor.rgb;

			return colorOut;
		}
    ENDCG

	/////////////////////////////////////////////////////////////////////////////////////////////
	// SM 3.0
	/////////////////////////////////////////////////////////////////////////////////////////////
	SubShader 
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "LightMode" = "Always"}
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass
		{
			Name "XRAYMAIN"
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 3.0
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			ENDCG
		}
		Pass
		{
			Name "XRAYSEC"
			Blend One One

			CGPROGRAM
			#pragma target 3.0
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			ENDCG
		}
	} 
	/////////////////////////////////////////////////////////////////////////////////////////////
	// SM 2.5
	/////////////////////////////////////////////////////////////////////////////////////////////
	SubShader 
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "LightMode" = "Always"}
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass
		{
			Name "XRAYMAIN"
			Blend SrcAlpha OneMinusSrcAlpha 

			CGPROGRAM
			#pragma target 2.5
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			ENDCG
		}
		Pass
		{
			Name "XRAYSEC"
			Blend One One

			CGPROGRAM
			#pragma target 2.5
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			ENDCG
		}
	} 
	/////////////////////////////////////////////////////////////////////////////////////////////
	// SM 2.0
	/////////////////////////////////////////////////////////////////////////////////////////////
	SubShader 
	{
		Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" "LightMode" = "Always"}
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass
		{
			Name "XRAYMAIN"
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 2.0
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			ENDCG
		}
		Pass
		{
			Name "XRAYSEC"
			Blend One One

			CGPROGRAM
			#pragma target 2.0
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			
			ENDCG
		}
	} 
	FallBack "Unlit/Transparent"
	CustomEditor "MK.XRay.MKXRayFreeEditor"
}
