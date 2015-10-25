#warning Upgrade NOTE: unity_Scale shader variable was removed; replaced 'unity_Scale.w' with '1.0'

Shader "AGF/AGF_Planar_Local_Parallax_Specular" {
	Properties {
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_Parallax("Height", Range (0.005, 0.08)) = 0.02
		_ParallaxMap("Heightmap (R)", 2D) = "black" {}
		_Blend("Blending", Range (0.01, 0.4)) = 0.2
		_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess("Shininess", Range(0.01, 1)) = 0.078125
	}

	CGINCLUDE
	float2 Parallax2(half h, half height, half3 viewDir) {
		h = h * height - height/2.0;
		float3 v = normalize(viewDir);
		return h * v.xy;
	}
	ENDCG

	SubShader {
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 600
		
		CGPROGRAM
		#pragma exclude_renderers xbox360 ps3 flash
		#pragma surface surf BlinnPhong vertex:vert
		#pragma target 3.0

		sampler2D _MainTex, _BumpMap, _ParallaxMap;
		float4 _MainTex_ST, _BumpMap_ST;
		float _Parallax;
		fixed _Blend;
		half _Shininess;

		struct Input {
			float4 coord0;
			float2 coord1;
			float3 norm;
			float3 dirView;
		};

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			//Same UV is shared between color, bump and height, not enough texture space
			o.coord0.xy = (v.vertex.yz - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord0.zw = (v.vertex.xz - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.coord1.xy = (v.vertex.xy - _MainTex_ST.zw) * _MainTex_ST.xy;
			o.norm = v.normal * 1.0;
			o.dirView = ObjSpaceViewDir(v.vertex);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed3 n = max(abs(IN.norm) - _Blend, 0);
			n /= (n.x + n.y + n.z).xxx;
			half2 p1 = Parallax2(tex2D(_ParallaxMap, IN.coord0.xy).x, _Parallax, IN.dirView.yzx);
			half2 p2 = Parallax2(tex2D(_ParallaxMap, IN.coord0.zw).x, _Parallax, IN.dirView.xzy);
			half2 p3 = Parallax2(tex2D(_ParallaxMap, IN.coord1.xy).x, _Parallax, IN.dirView.xyz);
			fixed2 p = (p1 * n.xx) + (p2 * n.yy) + (p3 * n.zz);
			fixed4 c1 = tex2D(_MainTex, IN.coord0.xy + p);
			fixed4 c2 = tex2D(_MainTex, IN.coord0.zw + p);
			fixed4 c3 = tex2D(_MainTex, IN.coord1.xy + p);
			fixed4 c = (c1 * n.xxxx) + (c2 * n.yyyy) + (c3 * n.zzzz);
			o.Albedo = c.rgb;
			fixed3 b1 = UnpackNormal(tex2D(_BumpMap, IN.coord0.xy + p));
			fixed3 b2 = UnpackNormal(tex2D(_BumpMap, IN.coord0.zw + p));
			fixed3 b3 = UnpackNormal(tex2D(_BumpMap, IN.coord1.xy + p));
			fixed3 b = (b1 * n.xxx) + (b2 * n.yyy) + (b3 * n.zzz);
			o.Normal = b;
			o.Gloss = c.a;
			o.Alpha = c.a;
			o.Specular = _Shininess;
		}
		ENDCG
	} 

	FallBack "Bumped Specular"
}