// FR-03 inspection-target outline: a slightly inflated shell with a pulsing fresnel rim.
Shader "DroneTrainer/RimHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (0.27, 0.88, 0.82, 1)
        _Power ("Rim Power", Range(0.5, 8)) = 2.2
        _Intensity ("Intensity", Range(0, 4)) = 2
        _Pulse ("Pulse Speed", Float) = 3
        _Offset ("Shell Offset (m)", Float) = 0.0012
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Rim"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Power;
                half _Intensity;
                half _Pulse;
                float _Offset;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(i.normalOS);
                posWS += nWS * _Offset;
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = nWS;
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half ndv = saturate(dot(normalize(i.normalWS), normalize(i.viewWS)));
                half rim = pow(1.0h - ndv, _Power);
                half pulse = _Pulse > 0.01h ? 0.62h + 0.38h * sin(_Time.y * _Pulse) : 1.0h;
                half a = saturate((rim * _Intensity + 0.12h) * pulse);
                return half4(_Color.rgb * a, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
