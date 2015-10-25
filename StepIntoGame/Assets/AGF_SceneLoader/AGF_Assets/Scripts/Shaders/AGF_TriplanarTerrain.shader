Shader "AGF/AGF_TriplanarTerrain" {
	Properties {
		_GlobalTiling ("Global Tiling", Float) = 1
		_Projection ("Projection", Float) = 10
		
		_Splat0Tint ("Layer 0 (R) tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Splat0 ("Layer 0 (R) texture", 2D) = "white" {}
		_Splat0Bump ("Layer 0 (R) normal", 2D) = "bump" {}
		
		_Splat1Tint ("Layer 1 (G) tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Splat1 ("Layer 1 (G) texture", 2D) = "white" {}
		_Splat1Bump ("Layer 1 (G) normal)", 2D) = "bump" {} 
		
		_Splat2Tint ("Layer 2 (B) tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Splat2 ("Layer 2 (B) texture", 2D) = "white" {}
		_Splat2Bump ("Layer 2 (B) normal", 2D) = "bump" {} 
		
		_Splat3Tint ("Layer 3 (A) tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Splat3 ("Layer 3 (A) texture", 2D) = "white" {}
		_Splat3Bump ("Layer 3 (A) normal", 2D) = "bump" {} 
		
		_SideTint ("Side tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Side ("Side texture", 2D) = "white" {}
		_SideBump ("Side normal", 2D) = "bump" {} 
		
		_Control ("Control (RGBA)", 2D) = "white" {}
		 
		// used in fallback on old cards  
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf Terrain vertex:vert fullforwardshadows nodirlightmap nolightmap
		#pragma target 3.0 

		// floats
		uniform float _GlobalTiling, _Projection;
		
		// splat map textures and tints
		sampler2D 	_Splat0, 		_Splat1, 		_Splat2, 		_Splat3, 		_Side;
		sampler2D 	_Splat0Bump, 	_Splat1Bump, 	_Splat2Bump, 	_Splat3Bump, 	_SideBump;
		float4 		_Splat0Tint, 	_Splat1Tint, 	_Splat2Tint, 	_Splat3Tint, 	_SideTint;
		
		// splat map
		sampler2D 	_Control;
		
		struct SurfaceOutputTerrain {
			fixed3 Albedo;
			fixed3 Normal;
			fixed4 Light;
			fixed3 Emission;
			fixed Specular;
			fixed Alpha;
		};

		inline fixed4 LightingTerrain (SurfaceOutputTerrain s, fixed3 lightDir, fixed atten)
		{
			float4 result;
			result.rgb = (s.Albedo * s.Light.x) + ( pow(s.Light.y, s.Light.w * 128) * s.Light.z );
			result.rgb *= _LightColor0.rgb * atten * 2;
			result.a = 0.0;
			return result;
		}
		
		struct Input { 
			float2 uv_Control; 
			
			float4 packFloatOne;
			float4 packFloatTwo;
			float4 packFloatThree;
		};
		
		void vert (inout appdata_full v, out Input o) {  
			UNITY_INITIALIZE_OUTPUT(Input,o);
			float3 worldPos = mul(_Object2World, v.vertex).xyz; 
			float3 worldNormal = mul(_Object2World, float4(v.normal, 0.0f)).xyz; 
			float3 lightDir = normalize(ObjSpaceLightDir(v.vertex)); 
			float3 viewDir = normalize(ObjSpaceViewDir(v.vertex)); 
			
			o.packFloatOne = float4( worldPos[0], worldPos[1], worldPos[2], worldNormal[0] );
			o.packFloatTwo = float4( worldNormal[1], worldNormal[2], lightDir[0], lightDir[1] );
			o.packFloatThree = float4( lightDir[2], viewDir[0], viewDir[1], viewDir[2] );
		}
		
		void surf (Input IN, inout SurfaceOutputTerrain o) {  
		
			float3 INworldPos = float3( IN.packFloatOne[0], IN.packFloatOne[1], IN.packFloatOne[2] );
			float3 INworldNormal = float3( IN.packFloatOne[3], IN.packFloatTwo[0], IN.packFloatTwo[1] );
			float3 INlightDir = float3( IN.packFloatTwo[2], IN.packFloatTwo[3], IN.packFloatThree[0] );
			float3 INviewDir = float3( IN.packFloatThree[1], IN.packFloatThree[2], IN.packFloatThree[3] );
			
			// lighting values
			float3 tangent;
			float3 binormal;
			float3x3 rotation;
			float3 lightDirT;
			float3 viewDirT;
			float3 worldNormal = normalize(INworldNormal);
			fixed4 lightX;
			fixed4 lightY;
			fixed4 lightZ;
			float3 h;  
			
			// uvs
			float2 tiling = float2(_GlobalTiling/10, _GlobalTiling/10);
			half2 xUV = INworldPos.zy * tiling;
			half2 yUV = INworldPos.xz * tiling;
			half2 zUV = INworldPos.xy * tiling;
			
			// texture blending based on normal. (top + side)
			half3 normalBlend = INworldNormal; 
			 
			// pick min and max Y values.
			normalBlend.y = max(INworldNormal.y, 0.0);
			half normalBlendMinY = min(INworldNormal.y, 0.0); 
			
			// adjust by power.
			normalBlend = saturate(pow(normalBlend*0.25, _Projection));
			normalBlendMinY = saturate(pow(normalBlendMinY*0.25, _Projection));
			 
			// normalize
			half3 blendMult = 1/(normalBlend.x + normalBlend.y + normalBlend.z + normalBlendMinY);			
			normalBlend *= blendMult;
			normalBlendMinY *= blendMult; 
			 
			// define color and normal vars.
			half4 splatMap = tex2D(_Control,IN.uv_Control).rgba; // splat
			half4 xSideColor, ySideColor, yTopColor, zSideColor, yTopColorBlend; // color
			half3 xSideNormal,ySideNormal, yTopNormal, zSideNormal, yTopNormalBlend; // normal
			
			// -- Side Texture -- //
			// Color values
			xSideColor = tex2D(_Side,xUV).xyzw;
			ySideColor = tex2D(_Side,yUV).xyzw;
			zSideColor = tex2D(_Side,zUV).xyzw;
			
			// normal values
			xSideNormal = UnpackNormal(tex2D(_SideBump,xUV)).rgb; // x-projection normal (side texture)
			ySideNormal = UnpackNormal(tex2D(_SideBump,yUV)).rgb; // y-projection normal (side texture) 
			zSideNormal = UnpackNormal(tex2D(_SideBump,zUV)).rgb; // z-projection normal (side texture)
			
			// -- Top Texture -- //
			half currentSplat;
			
			// splat alpha red
			yTopColorBlend = tex2D(_Splat0, yUV).xyzw * _Splat0Tint;
			yTopNormalBlend = UnpackNormal(tex2D(_Splat0Bump,yUV)).xyz;
			
			currentSplat = splatMap.r;
			currentSplat = pow((1.5*currentSplat),1.5);
			currentSplat = clamp(currentSplat, 0.0, 1.0);
			
			yTopColor = 0;
			yTopNormal = 0;
			
			yTopColor = lerp(yTopColor, yTopColorBlend, currentSplat); // y-projection color (top texture)
			yTopNormal = lerp(yTopNormal, yTopNormalBlend, currentSplat); // y-projection normal (top texture)
			
			// splat alpha green
			yTopColorBlend = tex2D(_Splat1, yUV).xyzw * _Splat1Tint;
			yTopNormalBlend = UnpackNormal(tex2D(_Splat1Bump,yUV)).xyz;
			
			currentSplat = splatMap.g;
			currentSplat = pow((1.5*currentSplat),1.5);
			currentSplat = clamp(currentSplat, 0.0, 1.0);
			
			yTopColor = lerp(yTopColor, yTopColorBlend, currentSplat); // y-projection color (top texture)
			yTopNormal = lerp(yTopNormal, yTopNormalBlend, currentSplat); // y-projection normal (top texture)	
			
			// splat alpha blue
			yTopColorBlend = tex2D(_Splat2, yUV).xyzw * _Splat2Tint;
			yTopNormalBlend = UnpackNormal(tex2D(_Splat2Bump,yUV)).xyz;
			
			currentSplat = splatMap.b;
			currentSplat = pow((1.5*currentSplat),1.5);
			currentSplat = clamp(currentSplat, 0.0, 1.0);
			
			yTopColor = lerp(yTopColor, yTopColorBlend, currentSplat); // y-projection color (top texture)
			yTopNormal = lerp(yTopNormal, yTopNormalBlend, currentSplat); // y-projection normal (top texture)	
			
			// splat alpha alpha
			yTopColorBlend = tex2D(_Splat3, yUV).xyzw * _Splat3Tint;
			yTopNormalBlend = UnpackNormal(tex2D(_Splat3Bump,yUV)).xyz;
			
			currentSplat = splatMap.a;
			currentSplat = pow((1.5*currentSplat),1.5);
			currentSplat = clamp(currentSplat, 0.0, 1.0);
			
			yTopColor = lerp(yTopColor, yTopColorBlend, currentSplat); // y-projection color (top texture)
			yTopNormal = lerp(yTopNormal, yTopNormalBlend, currentSplat); // y-projection normal (top texture)
			
			// -- Lighting -- //
			
			// -- X projection lighting (YZ-plane) -- //
			tangent = float3(0, 0, 1);
			binormal = cross(worldNormal, tangent) * (step(worldNormal.x, 0) * 2 - 1);
			rotation = float3x3(tangent, binormal, worldNormal);
			lightDirT = mul(rotation, INlightDir);
			viewDirT = mul(rotation, INviewDir);
			
			// Determine the value of LightX.
			lightX.zw = half2(0.0,0.0);// The side texture's x-projection. (the top texture does not have an x-projection)
			lightX.x = saturate(dot(xSideNormal * normalBlend.x, lightDirT));
			h = normalize(lightDirT + viewDirT);
			lightX.y = saturate(dot(xSideNormal * normalBlend.x, h));
			
			// -- Y projection lighting (XZ-plane) -- //
			tangent = float3(1, 0, 0);
			binormal = cross(worldNormal, tangent) * (step(worldNormal.y, 0) * 2 - 1);
			rotation = float3x3(tangent, binormal, worldNormal);
			lightDirT = mul(rotation, INlightDir);
			viewDirT = mul(rotation, INviewDir);
			
			// Determine the value of LightY.
			lightY.zw = half2(0.0,0.0);// The side + top's y-projection.
			lightY.x = saturate(dot( (yTopNormal * normalBlend.y) + (ySideNormal * normalBlendMinY), lightDirT)) ;
			h = normalize(lightDirT + viewDirT);
			lightY.y = saturate(dot( (yTopNormal * normalBlend.y) + (ySideNormal * normalBlendMinY), h)) ;
			
			// -- Z projection lighting (XY-plane) -- //
			tangent = float3(-1, 0, 0);
			binormal = cross(worldNormal, tangent) * (step(worldNormal.z, 0) * 2 - 1);
			rotation = float3x3(tangent, binormal, worldNormal);
			lightDirT = mul(rotation, INlightDir);
			viewDirT = mul(rotation, INviewDir);
			
			// Determine the value of LightZ.
			lightZ.zw = half2(0.0,0.0);// The side texture's z-projection. (the top texture does not have a z-projection)
			lightZ.x = saturate(dot(zSideNormal * normalBlend.z, lightDirT));
			h = normalize(lightDirT + viewDirT);
			lightZ.y = saturate(dot(zSideNormal * normalBlend.z, h));
			
			// we now have the values for LightX, LightY, and LightZ. apply these to the Light variable. 
			o.Light = lightX + lightY + lightZ;
			 
			// assign the color. (does not take spec or normals into account)  
			half3 albedoX = (xSideColor * normalBlend.x * _SideTint); 
			half3 albedoY = (ySideColor * normalBlendMinY * _SideTint) +  (yTopColor * normalBlend.y);
			half3 albedoZ = (zSideColor * normalBlend.z * _SideTint); 
			
			o.Albedo = albedoX + albedoY + albedoZ;
		}
		ENDCG 
	} 
	Fallback "VertexLit"
}