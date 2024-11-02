Shader "Custom/PlaneWaveVisualizer_Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1,0,0,1)
        _ColorB ("Color B", Color) = (0,0,1,1)
        _WaveHeight ("Wave Height", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorA;
            float4 _ColorB;
            float _WaveHeight;

            // Add buffer reference
            StructuredBuffer<float> amplitudes;

            v2f vert (appdata v)
            {
                v2f o;
                
                float amplitude = amplitudes[v.vertexID];
                
                float4 modifiedVertex = v.vertex;
                modifiedVertex.y += amplitude * _WaveHeight;
                
                o.vertex = UnityObjectToClipPos(modifiedVertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                float heightColor = (amplitude + 1.0) * 0.5; 
                o.color = lerp(_ColorA, _ColorB, heightColor);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}