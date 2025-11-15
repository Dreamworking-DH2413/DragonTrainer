Shader "Custom/SliceVisualizer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Volume ("Volume", 3D) = "" {}
        _SliceDepth ("Slice Depth", Range(0, 1)) = 0.5
    }
    SubShader
    {
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler3D _Volume;
            float _SliceDepth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample a 2D slice from the 3D volume
                float3 uvw = float3(i.uv.x, i.uv.y, _SliceDepth);
                float density = tex3D(_Volume, uvw).r;
                
                // Color mapping: black->red->yellow->white (fire-like)
                float3 color;
                if (density < 0.33)
                {
                    // Black to red
                    color = float3(density * 3.0, 0, 0);
                }
                else if (density < 0.66)
                {
                    // Red to yellow
                    float t = (density - 0.33) * 3.0;
                    color = float3(1, t, 0);
                }
                else
                {
                    // Yellow to white
                    float t = (density - 0.66) * 3.0;
                    color = float3(1, 1, t);
                }
                
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}