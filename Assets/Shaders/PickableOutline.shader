
Shader "Custom/PickableOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0.9, 1, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags { "Queue" = "Geometry+1" "RenderType" = "Opaque" }

        Pass
        {
            Cull Front
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 expanded =
                    v.vertex.xyz + normalize(v.normal) * _OutlineWidth;

                o.vertex = UnityObjectToClipPos(
                    float4(expanded, 1.0)
                );

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }

            ENDCG
        }
    }

    Fallback Off
}
