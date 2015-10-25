#warning Upgrade NOTE: unity_Scale shader variable was removed; replaced 'unity_Scale.w' with '1.0'

Shader "AGF/AGF_Planar_Local_Detail" {
	Properties {
		_MainTex("Base (RGB)", 2D) = "white" {}
		_DetailTex("Detail (RGB)", 2D) = "white" {}
		_Blend("Blending", Range (0.01, 0.4)) = 0.2
	}

	SubShader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 250
		
		CGPROGRAM
		#pragma exclude_renderers xbox360 ps3 flash
		#pragma surface surf Lambert vertex:vert
		#pragma target 3.0

		sampler2D _MainTex, _DetailTex;
		float4 _MainTex_ST, _DetailTex_ST;
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
			o.coord1.zw = (v.vertex.yz - _DetailTex_ST.zw) * _DetailTex_ST.xy;
			o.coord2.xy = (v.vertex.xz - _DetailTex_ST.zw) * _DetailTex_ST.xy;
			o.coord2.zw = (v.vertex.xy - _DetailTex_ST.zw) * _DetailTex_ST.xy;
			o.norm = v.normal * 1.0;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed3 n = max(abs(IN.norm) - _Blend, 0);
			n /= (n.x + n.y + n.z).xxx;
			fixed4 c1 = tex2D(_MainTex, IN.coord0.xy);
			fixed4 c2 = tex2D(_MainTex, IN.coord0.zw);
			fixed4 c3 = tex2D(_MainTex, IN.coord1.xy);
			fixed4 c = (c1 * n.xxxx) + (c2 * n.yyyy) + (c3 * n.zzzz);
			fixed3 d1 =  tex2D(_DetailTex, IN.coord1.zw).rgb;
			fixed3 d2 =  tex2D(_DetailTex, IN.coord2.xy).rgb;
			fixed3 d3 =  tex2D(_DetailTex, IN.coord2.zw).rgb;
			fixed3 d = (d1 * n.xxx) + (d2 * n.yyy) + (d3 * n.zzz);
			o.Albedo = c.rgb * d.rgb * 2;
			o.Alpha = c.a;
		}
		ENDCG
	} 

	FallBack "Diffuse"
}