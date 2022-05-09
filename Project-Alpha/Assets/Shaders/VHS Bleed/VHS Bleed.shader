//https://www.shadertoy.com/view/tddBDX

Shader "Hidden/VHS Bleed"
{
    Properties
    {
        _MainTex ("tex2D", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            
            #define color_resX 0.0
            float bias;

            //common shadertoy to unity conversions
			#define mod(x, y) (x-y*floor(x/y))
			#define atan(x, y) (atan2(y, x))
            #define iResolution _ScreenParams
            #define iTime _Time.y
            #define iTimeDelta unity_DeltaTime.x
            #define iFrame ((int)(_Time.y / iTimeDelta))

            float3 rgb2yuv(float3 rgb)
            {
                return float3(0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b,
                              -0.147 * rgb.r - 0.289 * rgb.g + 0.436 * rgb.b,
                              0.615 * rgb.r - 0.515 * rgb.g - 0.100 * rgb.b);
            }

            float3 yuv2rgb(float3 yuv)
            {
                return float3(yuv.r + 1.140 * yuv.b,
                              yuv.r - 0.395 * yuv.g - 0.581 * yuv.b,
                              yuv.r + 2.032 * yuv.g);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 fragCoord = i.uv * iResolution.xy;
                int color_res = int((sin(iTime + fragCoord.y / 10.0) + 1.1) * color_resX + bias);
                float2 uv = i.uv; // float2 uv = fragCoord/iResolution.xy;
                
                float4 sampled = tex2D(_MainTex, uv);

                float Y = 0.299 * sampled.r + 0.587 * sampled.g + 0.114 * sampled.b;
                
                float2 colorData = float2(0.0 , 0.0);
                int samples = 5;
                for (int i = 0; i < color_res; i++)
                {
                    if (int(fragCoord.x) - i > 0)
                    {
                        float2 uv = float2(fragCoord.x - (float) i, fragCoord.y)/iResolution.xy;
                        float3 sampled = rgb2yuv((tex2D(_MainTex, uv)));
                        if (length(sampled.gb) > 0.02)
                        {
                            colorData += sampled.gb;
                            samples++;
                        }

                    }
                }
                colorData = sin(colorData / float(samples) * 1.2);
                        
                float3 rgb = yuv2rgb(float3(Y, colorData.x, colorData.y));
                return float4(rgb,1.0); // fragColor = float4(rgb,1.0);


            }
            ENDCG
        }
    }
}
