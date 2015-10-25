using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

public class AGF_AssetBundleResourceExtractor : MonoBehaviour {
	
	public static string sourceFolder = "Assets/AGF_SourceAssets"; // May be changed by the user.
	public static string sceneFolder = ""; // To be set by the tool.
	
	public static void ExtractSkybox(){
		// get the camera reference.
		Camera cameraObj = GameObject.Find ("AGF_CameraManager").GetComponent<AGF_CameraManager>().GetMainCamera();
		AGF_AtmosphereManager atmosphereManager = GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>();
		
		// step 1: create the target directory, if necessary.
		string targetDirectory = Main.TrimEndFromString( Application.dataPath, "Assets" ) + sourceFolder + "/" + sceneFolder + "/Skyboxes";
		if ( Directory.Exists( targetDirectory ) == false ){
			Directory.CreateDirectory( targetDirectory );
		}
		
		// step 2: clear the old files, if they exist.
		if ( File.Exists( targetDirectory + "/skyboxCubemap.cubemap" ) ){
			AssetDatabase.DeleteAsset( sourceFolder + "/skyboxCubemap.cubemap" );
		}
		if ( File.Exists( targetDirectory + "/skyboxMaterial.mat" ) ){
			AssetDatabase.DeleteAsset( sourceFolder + "/skyboxMaterial.mat" );
		}
		
		// step 3: copy over the cubemap from the camera.
		Cubemap cubemapReference = (Cubemap)cameraObj.GetComponent<Skybox>().material.GetTexture("_Tex");
		Cubemap skyboxCubemap = new Cubemap(cubemapReference.width, TextureFormat.RGB24, false);
		
		EditorUtility.CopySerialized( cubemapReference, skyboxCubemap );
		
		// step 4: create the cubemap asset.
		AssetDatabase.CreateAsset( skyboxCubemap, sourceFolder + "/" + sceneFolder + "/Skyboxes/skyboxCubemap.cubemap" );
//		AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Skyboxes/skyboxCubemap.cubemap" );
		
		// step 5: Create a temporary material, and link it up to the cubemap.
		Material skyboxMaterial = new Material( cameraObj.GetComponent<Skybox>().material );
		skyboxMaterial.SetTexture( "_Tex", (Cubemap)AssetDatabase.LoadAssetAtPath(sourceFolder + "/" + sceneFolder + "/Skyboxes/skyboxCubemap.cubemap", typeof(Cubemap) ) );
		
		// step 6: create the material asset.
		AssetDatabase.CreateAsset( skyboxMaterial, sourceFolder + "/" + sceneFolder + "/Skyboxes/skyboxMaterial.mat" );
//		AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Skyboxes/skyboxMaterial.mat" );
		
		// Now that both the cubemap and material have been created, assign the material into the camera.
		cameraObj.GetComponent<Skybox>().material = (Material)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Skyboxes/skyboxMaterial.mat", typeof(Material) );
		
		// In addition, we need to add another camera script to control the skybox rotation, etc.
		if ( cameraObj.GetComponent<AGF_SkyboxRotator>() == null ){
			cameraObj.gameObject.AddComponent<AGF_SkyboxRotator>();
		}
		cameraObj.gameObject.GetComponent<AGF_SkyboxRotator>().autoRotate = atmosphereManager.GetCurrentSkybox().autoRotate;
		cameraObj.gameObject.GetComponent<AGF_SkyboxRotator>().autoRotationSpeed = atmosphereManager.GetCurrentSkybox().autoRotationSpeed;
		cameraObj.gameObject.GetComponent<AGF_SkyboxRotator>().rotation = atmosphereManager.GetCurrentSkybox().rotation;
		cameraObj.gameObject.GetComponent<AGF_SkyboxRotator>().tint = atmosphereManager.GetCurrentSkybox().tint;
		
		EditorUtility.SetDirty( cameraObj.gameObject );
	}
	
	private static string[] colorMaps = new string[]{ "splatA_c.asset", "splatB_c.asset", "splatC_c.asset", "splatD_c.asset", "splatE_c.asset" };
	private static string[] normalMaps = new string[]{ "splatA_n.asset", "splatB_n.asset", "splatC_n.asset", "splatD_n.asset", "splatE_n.asset" };
	public static void ExtractTerrainTextures(){
		AGF_TerrainManager terrainManager = GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>();
		
		// step 1: create the target directory, if necessary.
		string targetDirectory = Main.TrimEndFromString( Application.dataPath, "Assets" ) + sourceFolder + "/" + sceneFolder + "/Terrain";
		if ( Directory.Exists( targetDirectory ) == false ){
			Directory.CreateDirectory( targetDirectory );
		}
		
		// step 2: clear the old files, if they exist.
		for ( int i = 0; i < 5; i++ ){
			if ( File.Exists( targetDirectory + "/" + colorMaps[i] ) ){
				AssetDatabase.DeleteAsset( sourceFolder + "/" + sceneFolder + "/Terrain/" + colorMaps[i] );
			}
			if ( File.Exists ( targetDirectory + "/" + normalMaps[i] ) ){
				AssetDatabase.DeleteAsset( sourceFolder + "/" + sceneFolder + "/Terrain/" + normalMaps[i] );
			}
		}
		
		if ( File.Exists( targetDirectory + "/TriplanarMaterial.mat" ) ){
			AssetDatabase.DeleteAsset( sourceFolder + "/" + sceneFolder + "/Terrain/TriplanarMaterial.mat" );	
		}
		
		// step 3: copy the terrain template.
		TerrainData template = (TerrainData)AssetDatabase.LoadAssetAtPath( "Assets/AGF_SceneLoader/AGF_Assets/Resources/Terrain/TerrainTemplate.asset", typeof(TerrainData) );
		if ( template == null ){
			Debug.LogError ("Template was null :(");			
		} else {
			TerrainData newTemplate = new TerrainData();
			EditorUtility.CopySerialized( template, newTemplate );
			AssetDatabase.CreateAsset( newTemplate, sourceFolder + "/" + sceneFolder + "/Terrain/" + "TerrainTemplate.asset" );
//			AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Terrain/" + "TerrainTemplate.asset" ); 
			TerrainData importedTemplate = (TerrainData)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Terrain/" + "TerrainTemplate.asset", typeof(TerrainData) );
			if ( importedTemplate == null ){
				Debug.LogError ("Imported template was null :(");				
			} else {
				terrainManager.GetCurrentTerrainInstance().terrainData = importedTemplate;
				terrainManager.GetCurrentTerrainInstance().GetComponent<TerrainCollider>().terrainData = importedTemplate;
			}
		}
			
		// step 4: copy the textures from the terrrain into new temporary textures, and save them out as pngs.
		for ( int i = 0; i < 5; i++ ){
			Texture2D cRef = terrainManager.GetTerrainImage(i);
			Texture2D colorMap = new Texture2D( cRef.width, cRef.height, TextureFormat.RGB24, true );
			EditorUtility.CopySerialized( cRef, colorMap );
			AssetDatabase.CreateAsset( colorMap, sourceFolder + "/" + sceneFolder + "/Terrain/" + colorMaps[i] );
			
			Texture2D nRef = terrainManager.GetTerrainNormal(i);
			Texture2D normalMap = new Texture2D( nRef.width, nRef.height, TextureFormat.RGB24, true );
			EditorUtility.CopySerialized( nRef, normalMap );
			AssetDatabase.CreateAsset( normalMap, sourceFolder + "/" + sceneFolder + "/Terrain/" + normalMaps[i] );
			
//			AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Terrain/" + colorMaps[i] ); 
//			AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Terrain/" + normalMaps[i] ); 
			
			terrainManager.SetTexture ( i, 
				(Texture2D)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Terrain/" + colorMaps[i], typeof(Texture2D) ),
				(Texture2D)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Terrain/" + normalMaps[i],typeof(Texture2D) ),
				"" );
		}
		
		// step 5: copy the detail textures from the terrain, and save them out as pngs.
		for ( int i = 0; i < terrainManager.GetDetailLayerCount(); i++ ){
			Texture2D dRef = terrainManager.GetDetailLayer(i).image;
			Texture2D detailMap = new Texture2D( dRef.width, dRef.height, TextureFormat.ARGB32, true );
			EditorUtility.CopySerialized( dRef, detailMap );
			AssetDatabase.CreateAsset( detailMap, sourceFolder + "/" + sceneFolder + "/Terrain/Detail_" + i + ".asset" );
//			AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Terrain/Detail_" + i + ".asset" );
			
			terrainManager.SetVegetationTexture( i, (Texture2D)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Terrain/Detail_" + i + ".asset", typeof(Texture2D) ), "" );
		}
		
		// step 6: copy the terrain material, and save it.
		Material triplanarMat = new Material( terrainManager.terrainMaterial );
		EditorUtility.CopySerialized( terrainManager.terrainMaterial, triplanarMat );
		
		AssetDatabase.CreateAsset( triplanarMat, sourceFolder + "/" + sceneFolder + "/Terrain/TriplanarMaterial.mat" );
//		AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Terrain/TriplanarMaterial.mat" );
		terrainManager.SetTerrainMaterial( (Material)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Terrain/TriplanarMaterial.mat", typeof(Material) ) );
		
		EditorUtility.SetDirty( terrainManager );
	}
	
	public static void InitWarehouseDirectories(){
		string targetDirectory = Main.TrimEndFromString( Application.dataPath, "Assets" ) + sourceFolder + "/" + sceneFolder + "/Warehouse";
		if ( Directory.Exists( targetDirectory ) == false ){
			Directory.CreateDirectory( targetDirectory );
		}
		
		if ( Directory.Exists( targetDirectory + "/Meshes" ) == true ){
			Directory.Delete( targetDirectory + "/Meshes", true );
		}
		Directory.CreateDirectory( targetDirectory + "/Meshes" );
		
		if ( Directory.Exists( targetDirectory + "/Materials" ) == true ){
			Directory.Delete( targetDirectory + "/Materials", true );
		}
		Directory.CreateDirectory( targetDirectory + "/Materials" );
		
		if ( Directory.Exists ( targetDirectory + "/Textures" ) == true ){
			Directory.Delete( targetDirectory + "/Textures", true );
		}
		Directory.CreateDirectory ( targetDirectory + "/Textures" );
	}
	
	private static Dictionary<Mesh, string> savedMeshList;
	private static Dictionary<Material, string> savedMaterialList;
	private static Dictionary<Texture2D, string> savedTextureList;
	private static List<Transform> warehouseObjectList;
	public static void InitWarehouseExtractionLists(){
		savedMeshList = new Dictionary<Mesh, string>();
		savedMaterialList = new Dictionary<Material, string>();
		savedTextureList = new Dictionary<Texture2D, string>();
		
		warehouseObjectList = new List<Transform>();
		
		Dictionary<int, Transform> placedTileList = GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>().GetPlacedTileList();
		foreach ( KeyValuePair<int, Transform> pair in placedTileList ){
			warehouseObjectList.Add ( pair.Value );	
		}
	}
	
	public static int GetNumberOfWarehouseObjects(){
		return warehouseObjectList.Count;
	}
	
	public static bool ExtractWarehouseObject( int id ){
		string targetDirectory = Main.TrimEndFromString( Application.dataPath, "Assets" ) + sourceFolder + "/" + sceneFolder + "/Warehouse";
		
		// Get the tile and related names.
		Transform tile = warehouseObjectList[id];
		TileProperties tileProperties = tile.GetComponent<TileProperties>();
		string[] split = tileProperties.tileID.Split( new char[]{'/'} );
//		string packName = split[0];
		string tileName = split[1];
		
		print ( "Extracting assets from: " + tileName );
		
		// part A: SCRIPTS
		Component[] scripts = tile.GetComponentsInChildren<MonoBehaviour>();
		
		// remove the scripts.
		for ( int i = 0; i < scripts.Length; i++ ){
			DestroyImmediate( scripts[i] );	
		}
		
		// part B: MESH FILTERS
		MeshFilter[] objectMeshFilters = tile.GetComponentsInChildren<MeshFilter>();
		
		for ( int i = 0; i < objectMeshFilters.Length; i++ ){
			Mesh sharedMesh = objectMeshFilters[i].sharedMesh;
			if(sharedMesh==null)
				break;
			string meshPath = "Meshes/" + sharedMesh.name + ".asset";
			
			// create the meshes, if necessary.
			if ( savedMeshList.ContainsKey( sharedMesh ) == false ){
				Mesh newMesh = new Mesh();
				EditorUtility.CopySerialized( sharedMesh, newMesh );
				int count = 1;
				while ( File.Exists( targetDirectory + "/" + meshPath ) ){
					// a different version of the mesh already exists. we'll need to rename this one.
					meshPath = "Meshes/" + sharedMesh.name + count.ToString() + ".asset";	
					count++;
				}
				
				// ensure that the meshPath string has no invalid characters.
				meshPath = RemoveInvalidPathCharacters( meshPath );
				
				AssetDatabase.CreateAsset( newMesh, sourceFolder + "/" + sceneFolder + "/Warehouse/" + meshPath );
//				AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Warehouse/" + meshPath );
				
				savedMeshList.Add ( sharedMesh, meshPath );
			}
			
			// save the mesh reference back into the object.
			objectMeshFilters[i].sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Warehouse/" + savedMeshList[sharedMesh], typeof(Mesh) );
		}
		
		// part C: MESH COLLIDERS
		MeshCollider[] objectMeshColliders = tile.GetComponentsInChildren<MeshCollider>();
		
		for ( int i = 0; i < objectMeshColliders.Length; i++ ){
			Mesh sharedMesh = objectMeshColliders[i].sharedMesh;
			string meshPath = "Meshes/" + sharedMesh.name + ".asset";
			
			// create the meshes, if necessary.
			if ( savedMeshList.ContainsKey( sharedMesh ) == false ){
				Mesh newMesh = new Mesh();
				EditorUtility.CopySerialized( sharedMesh, newMesh );
				int count = 1;
				while ( File.Exists( targetDirectory + "/" + meshPath ) ){
					// a different version of the mesh already exists. we'll need to rename this one.
					meshPath = "Meshes/" + sharedMesh.name + count.ToString() + ".asset";	
					count++;
				}
				
				// ensure that the meshPath string has no invalid characters.
				meshPath = RemoveInvalidPathCharacters( meshPath );
				
				AssetDatabase.CreateAsset( newMesh, sourceFolder + "/" + sceneFolder + "/Warehouse/" + meshPath );
//				AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Warehouse/" + meshPath );
				
				savedMeshList.Add ( sharedMesh, meshPath );
			}
			
			// save the mesh reference back into the object.
			objectMeshColliders[i].sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Warehouse/" + savedMeshList[sharedMesh], typeof(Mesh) );
		}
		
		// part D: MATERIALS and TEXTURES
		Renderer[] objRenderers = tile.GetComponentsInChildren<Renderer>();
		
		for ( int i = 0; i < objRenderers.Length; i++ ){
			Material[] rendererMaterials = objRenderers[i].sharedMaterials;
			
			for ( int j = 0; j < rendererMaterials.Length; j++ ){
				Material sharedMaterial = rendererMaterials[j];
				if(sharedMaterial==null)
					break;
				string materialPath = "Materials/" + sharedMaterial.name + ".mat";

				
				// create the materials, if necessary.
				if ( savedMaterialList.ContainsKey( sharedMaterial ) == false ){
					Material newMat = new Material( sharedMaterial );
					EditorUtility.CopySerialized( sharedMaterial, newMat );
					
					int count = 1;
					while ( File.Exists ( targetDirectory + "/" + materialPath ) ){
						// a different version of the material already exists. we'll need to rename this one.
						materialPath = "Materials/" + sharedMaterial.name + count.ToString() + ".mat";	
						count++;
					}
					
					// before saving out this material (we now know the name is unique, and valid), we need to save out the textures that the material refers to.
					Dictionary<string,string> texturePaths = ExtractTexturesFromMaterial( sharedMaterial, targetDirectory, savedTextureList );
					
					if ( newMat == null ){
						print ("Material was null, reattempting...");
						return false;
					}
					foreach( KeyValuePair<string,string> textureInfo in texturePaths ){ 
						newMat.SetTexture( textureInfo.Key, (Texture2D)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Warehouse/" + textureInfo.Value, typeof(Texture2D) ) );
					}
					
					// save the shader reference directly.
					newMat.shader = Shader.Find( sharedMaterial.shader.name );
					
					// ensure that the materialPath string has no invalid characters.
					materialPath = RemoveInvalidPathCharacters( materialPath );
					
					AssetDatabase.CreateAsset( newMat, sourceFolder + "/" + sceneFolder + "/Warehouse/" + materialPath );
//					AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Warehouse/" + materialPath );
					
					objRenderers[i].sharedMaterials[j] = newMat;
					
					savedMaterialList.Add ( sharedMaterial, materialPath );
				}
				
				// save the material reference back into the object.
				Material mat = (Material)AssetDatabase.LoadAssetAtPath( sourceFolder + "/" + sceneFolder + "/Warehouse/" + savedMaterialList[sharedMaterial], typeof(Material) );
				rendererMaterials[j] = mat;
				objRenderers[i].sharedMaterials = rendererMaterials;
			}
		}
		
		return true;
	}
	
	private static string[] commonTextureProperties = new string[]{
        "_AlphaTex",
        "_BumpMap",
        "_BackTex",
        "_Control",
        "_DecalTex",
        "_Detail",
        "_DownTex",
        "_FrontTex",
        "_LeftTex",
        "_LightMap",
        "_MainTex",
        "_RightTex",
        "_Splat0",
        "_Splat1",
        "_Splat2",
        "_Splat3",
        "_UpTex"
    };
	private static Dictionary<string, string> ExtractTexturesFromMaterial( Material material, string targetDirectory, Dictionary<Texture2D, string> savedTextureList ){
		Dictionary<string, string> texturePaths = new Dictionary<string, string>();
		
		for ( int i = 0; i < commonTextureProperties.Length; i++ ){
			string textureProperty = commonTextureProperties[i];
			if ( material.HasProperty(textureProperty) && material.GetTexture(textureProperty) != null ){
				Texture2D oldTexture = (Texture2D)material.GetTexture(textureProperty);
				string texturePath = "Textures/" + oldTexture.name + ".asset";
				
				// create the textures, if necessary.
				if ( savedTextureList.ContainsKey( oldTexture ) == false ){
					Texture2D newTex = new Texture2D(oldTexture.width, oldTexture.height);
					EditorUtility.CopySerialized( oldTexture, newTex );
					int count = 1;
					while ( File.Exists ( targetDirectory + "/" + texturePath ) ){
						// a different version of the material already exists. we'll need to rename this one.
						texturePath = "Materials/" + oldTexture.name + count.ToString() + ".asset";	
						count++;
					}
					
					// ensure that the texturePath string has no invalid characters.
					texturePath = RemoveInvalidPathCharacters( texturePath );
					
					AssetDatabase.CreateAsset( newTex, sourceFolder + "/" + sceneFolder + "/Warehouse/" + texturePath );
//					AssetDatabase.ImportAsset( sourceFolder + "/" + sceneFolder + "/Warehouse/" + texturePath );
					
					savedTextureList.Add ( oldTexture, texturePath );
				}
				
				texturePaths.Add ( textureProperty, texturePath );
			}
		}
		
		return texturePaths;
	}
	
	private static string RemoveInvalidPathCharacters( string filepath ){
		for ( int i = 0; i < Path.GetInvalidPathChars().Length; i++ ){
			filepath = filepath.Replace( Path.GetInvalidPathChars()[i].ToString(), "" );
		}
		
		filepath = filepath.Replace( ":", "" );
		
		return filepath;
	}
}
