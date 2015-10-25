#warning Upgrade NOTE: unity_Scale shader variable was removed; replaced 'unity_Scale.w' with '1.0'

Shader "AGF/AGF_Planar_Local" {
	Properties {
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Blend("Blending", Range (0.01, 0.4)) = 0.2
	}

	SubShader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 200
		
		CGPROGRAM
		#pragma exclude_renderers xbox360 ps3 flash
		#pragma surface surf Lambert vertex:vert

		sampler2D _MainTex;
		float4 _MainTex_ST;
		fixed _Blend;

		struct Input {
			float4 coord0;
			float2 coord1;
			float3 norm;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.coord0.xy = (v.vertex.yz - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord0.zw = (v.vertex.xz - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord1.xy = (v.vertex.xy - _MainTex_ST.zw) * _MainTex_ST.xy;
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
			o.Alpha = c.a;
		}
		ENDCG
	} 

	FallBack "Diffuse"
}