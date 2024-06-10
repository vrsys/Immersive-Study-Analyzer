Shader "Custom/StereoPortalShader"
{
    Properties{
      _MainTex("Mono Texture (unused)", 2D) = "white" {}
      _LeftEyeTexture("Left Eye Texture", 2D) = "white" {}
      _RightEyeTexture("Right Eye Texture", 2D) = "white" {}
      _Color("Example color", Color) = (.25, .5, .5, 1)
    }
    SubShader{
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
          #pragma surface surf SimpleLambert
  
          half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
              half4 c;
              c.rgb = s.Albedo;
              c.a = s.Alpha;
              return c;
          }

        struct Input {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        sampler2D _LeftEyeTexture;
        sampler2D _RightEyeTexture;
        
        void surf(Input IN, inout SurfaceOutput o) {
            o.Albedo = unity_StereoEyeIndex == 0 ? tex2D(_LeftEyeTexture, IN.uv_MainTex).rgb : tex2D(_RightEyeTexture, IN.uv_MainTex).rgb;
        }

        ENDCG
    }
    Fallback "Diffuse"
}
