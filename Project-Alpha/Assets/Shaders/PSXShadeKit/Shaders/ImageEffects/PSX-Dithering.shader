﻿Shader "Hidden/PSX-Dithering"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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

			float3 _ColorResolution;
			float3 _DitherResolution;
			sampler2D _MainTex;
			float _HighResDitherMatrix;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
			};

			v2f vert(
				float4 vertex : POSITION, // vertex position input
				float2 uv : TEXCOORD0, // texture coordinate input
				out float4 outpos : SV_POSITION // clip space position output
			)
			{
				v2f o;
				o.uv = uv;
				outpos = UnityObjectToClipPos(vertex);
				return o;
			}

			float DitherColorChannel(float color, float ditherThreshold, float ditherStep)
			{
				float distance = fmod(color, ditherStep);
				float baseValue = floor(color / ditherStep) * ditherStep;

				return lerp(baseValue, baseValue + ditherStep, step(ditherThreshold, distance / ditherStep - 0.001f));
			}

			float GetDitherThreshold(float2 pixelPosition) 
			{
				const int ditheringMatrix8x8[64] =
				{
					//0, 0, 16, 16, 4, 4, 20, 20,
					//0, 0, 16, 16, 4, 4, 20, 20,
					//24, 24, 8, 8, 28, 28, 12, 12 ,
					//24, 24, 8, 8, 28, 28, 12, 12 ,
					//6, 6, 22, 22, 2, 2, 18, 18,
					//6, 6, 22, 22, 2, 2, 18, 18,
					//30, 30, 14, 14, 26, 26, 10, 10,
					//30, 30, 14, 14, 26, 26, 10, 10

					 0, 0,32,32, 8, 8,40,40,
					 0, 0,32,32, 8, 8,40,40,
					48,48,16,16,56,56,24,24,
					48,48,16,16,56,56,24,24,
					12,12,44,44, 4, 4,36,36,
					12,12,44,44, 4, 4,36,36,
					60,60,28,28,52,52,20,20,
					60,60,28,28,52,52,20,20

					//0,0,0,0,48,48,48,48,
					//0,0,0,0,48,48,48,48,
					//0,0,0,0,48,48,48,48,
					//0,0,0,0,48,48,48,48,
					//32,32,32,32,16,16,16,16,
					//32,32,32,32,16,16,16,16,
					//32,32,32,32,16,16,16,16,
					//32,32,32,32,16,16,16,16






				};

				const int ditheringMatrix4x4[16] =
				{
					0,  8,  2,  10,
					12, 4,  14, 6,
					3,  11, 1,  9,
					15, 7,  13, 5
				};

				const int ditheringMatrix2x2[4] =
				{
					0, 3,
					2, 1
				};

				return lerp(
					//ditheringMatrix2x2[fmod(pixelPosition.x, 2) + fmod(pixelPosition.y, 2) * 2] * 0.25f
					//, ditheringMatrix4x4[fmod(pixelPosition.x, 4) + fmod(pixelPosition.y, 4) * 4] * 0.0625f
					//, _HighResDitherMatrix);
					ditheringMatrix2x2[fmod(pixelPosition.x, 2) + fmod(pixelPosition.y, 2) * 2] * 0.25f
					, ditheringMatrix8x8[fmod(pixelPosition.x, 8) + fmod(pixelPosition.y, 8) * 8] * 0.015625f
					, _HighResDitherMatrix);
			}

			fixed4 frag(v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
			{
				const int2 pixelPosition = screenPos.xy;
				const float ditherThreshold = GetDitherThreshold(pixelPosition);
				const float3 ditherStep = 1.0f / max(_DitherResolution, 1.0f);
				const float3 colorStep = 1.0f / max(_ColorResolution, 1.0f);

				fixed4 col = tex2D(_MainTex, i.uv);
				col.r = DitherColorChannel(col.r, 0.5f, colorStep.r);
				col.g = DitherColorChannel(col.g, 0.5f, colorStep.g);
				col.b = DitherColorChannel(col.b, 0.5f, colorStep.b);

				col.r = DitherColorChannel(col.r, ditherThreshold, ditherStep.r);
				col.g = DitherColorChannel(col.g, ditherThreshold, ditherStep.g);
				col.b = DitherColorChannel(col.b, ditherThreshold, ditherStep.b);

				return col;
			}
			ENDCG
		}
	}
}
