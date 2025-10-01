Shader "URP/Particles/ChromaKey"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _KeyColor("Key Color", Color) = (0,0,1,1) // màu nền cần cắt (xanh dương)
        _Threshold("Threshold", Range(0,1)) = 0.1 // độ sai lệch cho phép
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _KeyColor;
            float _Threshold;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = tex2D(_BaseMap, IN.uv);

                // so sánh màu pixel với màu nền
                float diff = distance(col.rgb, _KeyColor.rgb);

                if (diff < _Threshold)
                    discard; // bỏ nền xanh

                return col;
            }
            ENDHLSL
        }
    }
}
