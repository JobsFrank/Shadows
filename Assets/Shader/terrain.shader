Shader "Unlit/terrain"
{
	Properties{
		_MainTex("Main Tex", 2D) = "white" {}
	}

		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
			Pass{
			Tags{ "LightMode" = "ForwardBase" "Queue" = "Geometry" }


			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest 
			#pragma multi_compile_fog
			#pragma shader_feature HARD_SHADOW SOFT_SHADOW_2x2 SOFT_SHADOW_4Samples SOFT_SHADOW_4x4
			#include "UnityCG.cginc"
			#include "../Include/ReceiveShadows_Include.cginc"

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed _bias;
			uniform fixed _strength;
			uniform fixed _farplaneScale;
			uniform fixed _texmapScale;
			uniform float _farplaneWidth;
			uniform float4x4 _depthV;
			uniform float4x4 _depthVPBias;
			uniform sampler2D _actShadowMap;


			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 shadowCoord : TEXCOORD1;
				UNITY_FOG_COORDS(2)
			};
			v2f vert(a2v v) 
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.shadowCoord = mul(_depthVPBias, mul(unity_ObjectToWorld, v.vertex));
				o.shadowCoord.z = -(mul(_depthV, mul(unity_ObjectToWorld, v.vertex)).z * _farplaneScale);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			} 
			fixed4 frag(v2f i) : SV_Target
			{
				float shadow_type;
				#if HARD_SHADOW
					shadow_type = FragHartShadows(i.shadowCoord,_actShadowMap,_bias,_strength,_texmapScale);
				#elif SOFT_SHADOW_2x2
					shadow_type = FragPCF2x2(i.shadowCoord,_actShadowMap,_bias,_strength,_texmapScale);
				#elif SOFT_SHADOW_4Samples
					shadow_type = FragPCF4Samples(i.shadowCoord,_actShadowMap,_bias,_strength,_texmapScale);
				#elif SOFT_SHADOW_4x4
					shadow_type = FragPCF4x4(i.shadowCoord,_actShadowMap,_bias,_strength,_texmapScale);
				#endif
				fixed4 albedo = tex2D(_MainTex, i.uv.xy)*shadow_type; 
				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo.rgb*5.0; 
				UNITY_APPLY_FOG(i.fogCoord, ambient);
				return fixed4(ambient, 1);
			}
		ENDCG
		}
	}
	Fallback Off
}
