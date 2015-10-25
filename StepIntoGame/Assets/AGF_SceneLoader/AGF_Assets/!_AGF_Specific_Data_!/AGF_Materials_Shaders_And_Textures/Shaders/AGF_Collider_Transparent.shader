#warning Upgrade NOTE: unity_Scale shader variable was removed; replaced 'unity_Scale.w' with '1.0'

Shader "AGF/AGF_LocalTransparent" {
	Properties {
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_Blend("Blending", Range (0.01, 0.4)) = 0.2
	}

	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
		LOD 100

		Pass {
			Name "BASE"
			Tags { "LightMode" = "Always" }

			Fog { Mode Off }
			Alphatest Greater 0
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB

			CGPROGRAM
			#pragma exclude_renderers xbox360 ps3 flash
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed _Blend;

			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float3 norm : TEXCOORD2;
			};

			v2f vert(a2v v) {
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv0.xy = (v.vertex.yz - _MainTex_ST.zw) * _MainTex_ST.xy;
				o.uv0.zw = (v.vertex.xz - _MainTex_ST.zw) * _MainTex_ST.xy;
				o.uv1.xy = (v.vertex.xy - _MainTex_ST.zw) * _MainTex_ST.xy;
				o.norm = v.normal * 1.0;
				return o;
			}

			fixed4 frag(v2f i) : COLOR {
				fixed3 n = max(abs(i.norm) - _Blend, 0);
				n /= (n.x + n.y + n.z).xxx;
				fixed4 c1 = tex2D(_MainTex, i.uv0.xy);
				fixed4 c2 = tex2D(_MainTex, i.uv0.zw);
				fixed4 c3 = tex2D(_MainTex, i.uv1.xy);
				fixed4 c = (c1 * n.xxxx) + (c2 * n.yyyy) + (c3 * n.zzzz);
				return c;
			}
			ENDCG 
		}
	}

	Fallback Off
}