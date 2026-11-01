Shader "Tetherfall/Hostile Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.78, 0.27, 0.22, 0.72)
        _OutlineWidth ("Outline Width", Range(0.001, 0.16)) = 0.032
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HOSTILE_OUTLINE"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct VertexToFragment
            {
                float4 position : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float _OutlineWidth;

            VertexToFragment Vert(AppData input)
            {
                VertexToFragment output;
                float3 worldPosition =
                    mul(unity_ObjectToWorld, input.vertex).xyz;
                float3 worldNormal =
                    normalize(UnityObjectToWorldNormal(input.normal));
                worldPosition += worldNormal * _OutlineWidth;
                output.position =
                    mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0));
                return output;
            }

            fixed4 Frag(VertexToFragment input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    Fallback Off
}
