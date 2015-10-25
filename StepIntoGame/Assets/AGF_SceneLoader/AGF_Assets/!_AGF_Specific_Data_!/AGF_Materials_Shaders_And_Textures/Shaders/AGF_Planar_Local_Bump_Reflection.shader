#warning Upgrade NOTE: unity_Scale shader variable was removed; replaced 'unity_Scale.w' with '1.0'

Shader "AGF/AGF_Planar_Local_Bump_Reflection" {
	Properties {
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_ReflectColor("Reflection (RGB) Strength (A)", Color) = (1,1,1,0.5)
		_Cube("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }
		_Blend("Blending", Range (0.01, 0.4)) = 0.2
	}

	SubShader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 400
		
		CGPROGRAM
		#pragma exclude_renderers xbox360 ps3 flash
		#pragma surface surf Lambert vertex:vert
		#pragma target 3.0

		sampler2D _MainTex, _BumpMap;
		float4 _MainTex_ST;
		samplerCUBE _Cube;
		fixed4 _ReflectColor;
		fixed _Blend;

		struct Input {
			float3 pos;
			float3 norm;
			float3 worldRefl;
			INTERNAL_DATA
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.pos = v.vertex.xyz;
			o.norm = v.normal * 1.0;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			//Surface inputs use all slots, so we calculate the uvs per pixel in this one
			//Can be optimised with custom reflection calculation
			//Same UV is shared between color and bump
			float2 uvx = (IN.pos.yz - _MainTex_ST.zw) * _MainTex_ST.xy;
			float2 uvy = (IN.pos.xz - _MainTex_ST.zw) * _MainTex_ST.xy;
			float2 uvz = (IN.pos.xy - _MainTex_ST.zw) * _MainTex_ST.xy;
			fixed3 n = max(abs(IN.norm) - _Blend, 0);
			n /= (n.x + n.y + n.z).xxx;
			fixed4 c1 = tex2D(_MainTex, uvx);
			fixed4 c2 = tex2D(_MainTex, uvy);
			fixed4 c3 = tex2D(_MainTex, uvz);
			fixed4 c = (c1 * n.xxxx) + (c2 * n.yyyy) + (c3 * n.zzzz);
			o.Albedo = c.rgb;
			fixed3 b1 = UnpackNormal(tex2D(_BumpMap, uvx));
			fixed3 b2 = UnpackNormal(tex2D(_BumpMap, uvy));
			fixed3 b3 = UnpackNormal(tex2D(_BumpMap, uvz));
			fixed3 b = (b1 * n.xxx) + (b2 * n.yyy) + (b3 * n.zzz);
			o.Normal = b;
			float3 worldRefl = WorldReflectionVector(IN, o.Normal);
			fixed4 reflcol = texCUBE(_Cube, worldRefl);
			reflcol.rgb *= _ReflectColor.a;
			o.Emission = reflcol.rgb * _ReflectColor.rgb;
			o.Alpha = reflcol.a * c.a;
		}
		ENDCG
	} 

	FallBack "Reflective/Bumped Diffuse"
}