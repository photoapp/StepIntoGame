#warning Upgrade NOTE: unity_Scale shader variable was removed; replaced 'unity_Scale.w' with '1.0'

Shader "AGF/AGF_Planar_Local_Bump_Specular" {
	Properties {
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess("Shininess", Range(0.01, 1)) = 0.078125
		_Blend("Blending", Range (0.01, 0.4)) = 0.2
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 300
		
		CGPROGRAM
		#pragma exclude_renderers xbox360 ps3 flash
		#pragma surface surf BlinnPhong vertex:vert
		#pragma target 3.0

		sampler2D _MainTex, _BumpMap;
		float4 _MainTex_ST, _BumpMap_ST;
		half _Shininess;
		fixed _Blend;

		struct Input {
			float4 coord0;
			float4 coord1;
			float4 coord2;
			float3 norm;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.coord0.xy = (v.vertex.yz - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord0.zw = (v.vertex.xz - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord1.xy = (v.vertex.xy - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord1.zw = (v.vertex.yz - _BumpMap_ST.zw) * _BumpMap_ST.xy;
			o.coord2.xy = (v.vertex.xz - _BumpMap_ST.zw) * _BumpMap_ST.xy;
			o.coord2.zw = (v.vertex.xy - _BumpMap_ST.zw) * _BumpMap_ST.xy;
			o.norm = v.normal * 1.0;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed3 n = max(abs(IN.norm) - _Blend, 0);
			n /= (n.x + n.y + n.z).xxx;
			fixed4 c1 = tex2D(_MainTex, IN.coord0.xy);
			fixed4 c2 = tex2D(_MainTex, IN.coord0.zw);
			fixed4 c3 = tex2D(_MainTex, IN.coord1.xy);
			fixed4 c = (c1 * n.xxxx) + (c2 * n.yyyy) + (c3 * n.zzzz);
			o.Albedo = c.rgb;
			fixed3 b1 = UnpackNormal(tex2D(_BumpMap, IN.coord1.zw));
			fixed3 b2 = UnpackNormal(tex2D(_BumpMap, IN.coord2.xy));
			fixed3 b3 = UnpackNormal(tex2D(_BumpMap, IN.coord2.zw));
			fixed3 b = (b1 * n.xxx) + (b2 * n.yyy) + (b3 * n.zzz);
			o.Normal = b;
			o.Gloss = c.a;
			o.Alpha = c.a;
			o.Specular = _Shininess;
		}
		ENDCG
	} 

	FallBack "Diffuse"
}