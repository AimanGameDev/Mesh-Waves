Shader "Custom/SphereWaveVisualizer"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _ColorA ("Color A", Color) = (1,0,0,1)
        _ColorB ("Color B", Color) = (0,0,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _WaveHeight ("Wave Height", Float) = 1.0
        _AmplitudeMultiplier ("Amplitude Multiplier", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 5.0
        #include "UnityCG.cginc"
        
        #ifdef SHADER_API_D3D11
            StructuredBuffer<float> amplitudes;
        #endif

        sampler2D _MainTex;
        float4 _ColorA;
        float4 _ColorB;
        float _WaveHeight;
        float _AmplitudeMultiplier;

        struct Input
        {
            float2 uv_MainTex;
            float4 vertexColor : COLOR;
        };

        struct appdata_full2 
        {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            float4 texcoord3 : TEXCOORD3;
            float4 color : COLOR;
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        half _Glossiness;
        half _Metallic;

        void vert(inout appdata_full2 v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            #ifdef SHADER_API_D3D11
                const float amplitude = amplitudes[v.vertexID];
                v.vertex.y += amplitude * _WaveHeight;
                
                v.color = lerp(_ColorA, _ColorB, amplitude * _AmplitudeMultiplier);
            #else
                v.color = _ColorA;
            #endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * IN.vertexColor;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}