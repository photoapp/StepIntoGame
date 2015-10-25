using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class AGF_TerrainManager : MonoBehaviour {
	
	public enum TerrainShaderType{
		Triplanar, Simple,	
	}
	private TerrainShaderType m_CurrentTerrainShaderType = TerrainShaderType.Triplanar;
	
	public Terrain terrainPrefab;
	public Transform waterPrefab;
	public Transform terrainExtensionPrefab;
	private Transform m_TerrainExtensionPrefab;
	
	private float m_CurrentBrushRotation;
	private Terrain currentTerrainInstance;
	private Transform currentWaterInstance;
	public float startingTerrainHeight = 0.0f;
	public float waterOffset = 1.5f;
	public int defaultResolution = 513;
	
	public float brushSize = 2.0f;
	public float brushDepth = 0.15f;
	public float brushBlend = 0.5f;
	public Material terrainMaterial;
	public Material terrainExtensionMaterial;
	private float m_InitialHeight;
	private float m_TerrainExtensionOffset = 0.0f;
	private float m_CurrentWaterHeight = 3.0f;
	private float m_CurrentProjectionPower = 0.0f;
	private float m_TerrainTiling = 1.0f;
	
	private float m_VegetationDensity = 1.0f;
	private float m_VegetationDrawDistance = 80.0f;
	
	private bool m_SculptingActive = true;
	private bool m_PaintingActive = false;
	private bool m_PlantingActive = false;
	
	private int m_CurrentHeightmapResolution = 513;
	private int m_CurrentDetailmapResolution = 512;
	private float m_CurrentTerrainSize = 500.0f;
	private float m_WindSpeed = 0.3f;
	
	
	public float m_CurrentHeightmapHeightScale = 100.0f;

	
	[System.Serializable]
	public class BrushImage {
		public Texture2D image;
		
		[HideInInspector]
		public int brushWidth, brushHeight;
	}
	
	private class TerrainImageSet{
		public Texture2D[] images;
		public Texture2D[] normals;
		public string[] paths;
		public Color[] tints;
		
		public bool terrainVisible;
		public Vector2 terrainSize;
	}
	
	public class DetailLayer{
		public Texture2D image;
		public string path;
		
		public float minWidth = 1;
		public float maxWidth = 2;
		
		public float minHeight = 1;
		public float maxHeight = 2;
		
		public Color healthyColor = new Color(0.263f, 0.976f, 0.165f, 1.0f);
		public Color dryColor = new Color(0.804f, 0.737f, 0.102f, 1.0f);
		
		public DetailRenderMode renderMode = DetailRenderMode.GrassBillboard;
		
		public bool active = true;
	}
	
	private class WaterSettings{
		public float wavePower;	
		
		public float waveAMagnitude;
		public float waveAAngle;
		public float waveBMagnitude;
		public float waveBAngle;
		public float waveCMagnitude;
		public float waveCAngle;
		public float waveDMagnitude;
		public float waveDAngle;
		
		public Color waterReflectionColor;
		public Color waterRefractionColor;
		public Color waterSpecularColor;
		
		public float waterReflectionIntensity;
		public float waterReflectionWeight;
		
		public float waterFoamIntensity;
		public float waterShoreFade;
		public float waterFoamWaves;
		
		public bool waterVisible;
	}
	private WaterSettings m_CurrentWaterSettings;
	
	private List<DetailLayer> m_CurrentDetailLayerSet;
	private TerrainImageSet m_CurrentTerrainSet;
	private int m_CurrentTerrainImageID = 0;
	
	public BrushImage[] brushImages;
	private int m_CurrentBrushImageID = -1;
	private bool m_HasInit = false;
	private bool m_IsUnderwater = false;
	
	private Dictionary<string,Dictionary<string,Texture2D>> m_LoadedColormaps;
	private Dictionary<string,Dictionary<string,Texture2D>> m_LoadedNormals;
	private Dictionary<string,Dictionary<string,Texture2D>> m_LoadedVegetation;
	
	private Color[] m_CurrentTemplate;
	
	// Use this for initialization
	void Start () {
		for( int i = 0; i < brushImages.Length; i++ ){
			brushImages[i].brushWidth = brushImages[i].image.width;
			brushImages[i].brushHeight = brushImages[i].image.height;
		}
		
		// set the default brush power
		SetCurrentProjectionPower( 10.0f );
		
		m_CurrentTerrainSet = new TerrainImageSet();
		m_CurrentTerrainSet.images = new Texture2D[5];
		m_CurrentTerrainSet.normals = new Texture2D[5];
		m_CurrentTerrainSet.tints = new Color[5]{ Color.white, Color.white, Color.white, Color.white, Color.white };
		m_CurrentTerrainSet.paths = new string[5];
		
		m_CurrentWaterSettings = new WaterSettings();
		
		m_CurrentDetailLayerSet = new List<DetailLayer>();
		
		// default the rotation to zero.
		m_CurrentBrushRotation = 0.0f;
		
		// initialize the texture list.
		m_LoadedColormaps = new Dictionary<string, Dictionary<string, Texture2D>>();
		m_LoadedNormals = new Dictionary<string, Dictionary<string, Texture2D>>();
		m_LoadedVegetation = new Dictionary<string, Dictionary<string, Texture2D>>();
	}
	
	public void EditorInit(){
		// Called while in the editor to initialize this object.
		Start();
	}
	
	public void Init(){
		m_HasInit = true;
	}
	
	// Update is called once per frame
	void Update () {
		if ( m_HasInit == false ){
			return;
		}
	}
	
	public void AddTextureToList( Texture2D newTex, string bundleName ){
		// determine if this texture is a colormap or normalmap.
		if ( newTex.name.Contains("_c") || newTex.name.Contains("_Diffuse") ){
			if ( m_LoadedColormaps.ContainsKey( bundleName ) == false ){
				m_LoadedColormaps.Add ( bundleName, new Dictionary<string,Texture2D>() );	
			}
			string name = newTex.name.Replace( "_c", "" );
			name = name.Replace( "_Diffuse", "" );
			m_LoadedColormaps[bundleName].Add ( name, newTex );	
			
		} else if ( newTex.name.Contains("_n") || newTex.name.Contains("_Normal") ){
			if ( m_LoadedNormals.ContainsKey( bundleName ) == false ){
				m_LoadedNormals.Add ( bundleName, new Dictionary<string,Texture2D>() );	
			}
			string name = newTex.name.Replace( "_n", "" );
			name = name.Replace( "_Normal", "" );
			m_LoadedNormals[bundleName].Add ( name, newTex );		
		}
	}
	
	public void AddVegetationTextureToList( Texture2D newTex, string bundleName ){
		// determine if this texture is a colormap or normalmap.
		string name = "";
		if ( newTex.name.Contains("_c") || newTex.name.Contains("_Diffuse") ){
			if ( m_LoadedVegetation.ContainsKey( bundleName ) == false ){
				m_LoadedVegetation.Add ( bundleName, new Dictionary<string,Texture2D>() );	
			}
			name = newTex.name.Replace( "_c", "" );
			name = name.Replace( "_Diffuse", "" );
			
		} else if ( newTex.name.Contains("_n") || newTex.name.Contains("_Normal") ){
			if ( m_LoadedVegetation.ContainsKey( bundleName ) == false ){
				m_LoadedVegetation.Add ( bundleName, new Dictionary<string,Texture2D>() );	
			}
			name = newTex.name.Replace( "_n", "" );
			name = name.Replace( "_Normal", "" );	
		}
		
		m_LoadedVegetation[bundleName].Add ( name, newTex );	
	}
	
	public void UnloadTexturesFromBundle( string bundleName ){
		// Colormaps first.
		if ( m_LoadedColormaps.ContainsKey( bundleName ) ){
			m_LoadedColormaps.Remove ( bundleName );
		}
		
		// Normals next.
		if ( m_LoadedNormals.ContainsKey( bundleName ) ){
			m_LoadedNormals.Remove ( bundleName );
		}
		
		Resources.UnloadUnusedAssets();
	}
	
	public void UnloadVegetationTexturesFromBundle( string bundleName ){
		if ( m_LoadedVegetation.ContainsKey( bundleName ) ){	
			m_LoadedVegetation.Remove ( bundleName );
		}
		
		Resources.UnloadUnusedAssets();
	}
	
	public Dictionary<string,Texture2D> GetLoadedColormaps(){
		Dictionary<string,Texture2D> result = new Dictionary<string,Texture2D>();
		foreach ( KeyValuePair<string,Dictionary<string,Texture2D>> bundle in m_LoadedColormaps ){
			foreach( KeyValuePair<string,Texture2D> texture in bundle.Value ){
				result.Add ( bundle.Key + "/" + texture.Key, texture.Value );	
			}
		} 
		return result;	
	}
	
	public Dictionary<string,Texture2D> GetLoadedNormalmaps(){
		Dictionary<string,Texture2D> result = new Dictionary<string,Texture2D>();
		foreach ( KeyValuePair<string,Dictionary<string,Texture2D>> bundle in m_LoadedNormals ){
			foreach( KeyValuePair<string,Texture2D> texture in bundle.Value ){
				result.Add ( bundle.Key + "/" + texture.Key, texture.Value );	
			}
		} 
		return result;		
	}
	
	public Dictionary<string,Texture2D> GetLoadedVegetationTextures(){
		Dictionary<string,Texture2D> result = new Dictionary<string,Texture2D>();
		foreach ( KeyValuePair<string,Dictionary<string,Texture2D>> bundle in m_LoadedVegetation ){
			foreach( KeyValuePair<string,Texture2D> texture in bundle.Value ){
				result.Add ( bundle.Key + "/" + texture.Key, texture.Value );	
			}
		} 
		return result;	
	}
	
	public BrushImage[] GetBrushImages(){
		return brushImages;	
	}
	
	public Texture2D GetBrushImage( int id ){
		return brushImages[id].image;	
	}
	
	public Texture2D GetTerrainImage( int id ){
		return m_CurrentTerrainSet.images[id];	
	}
	
	public Texture2D GetTerrainNormal( int id ){
		return m_CurrentTerrainSet.normals[id];	
	}
	
	public int GetDetailLayerCount(){
		return m_CurrentDetailLayerSet.Count;
	}
	
	public Terrain GetCurrentTerrainInstance(){
		return currentTerrainInstance;	
	}
	
	public void AddNewDetailLayer(){
		if ( currentTerrainInstance != null ){
			DetailPrototype[] details = currentTerrainInstance.terrainData.detailPrototypes;
			DetailPrototype[] newDetails = new DetailPrototype[details.Length+1];
			for ( int i = 0; i < details.Length; i++ ){
				newDetails[i] = details[i];
			}
			newDetails[newDetails.Length-1] = new DetailPrototype();
			currentTerrainInstance.terrainData.detailPrototypes = newDetails;
			int[,] newDetailmap = new int[currentTerrainInstance.terrainData.detailWidth, currentTerrainInstance.terrainData.detailHeight];
			for ( int i = 0; i < currentTerrainInstance.terrainData.detailWidth; i++ ){
				for ( int j = 0; j < currentTerrainInstance.terrainData.detailHeight; j++ ){	
					newDetailmap[i,j] = 0;
				}
			}
			currentTerrainInstance.terrainData.SetDetailLayer( 0, 0, newDetails.Length-1, newDetailmap );
			
			if ( m_CurrentDetailLayerSet.Count < newDetails.Length ){
				DetailLayer newLayer = new DetailLayer();
				List<string> bundleList = new List<string>(m_LoadedVegetation.Keys);
				if ( bundleList.Count > 0 ){
					List<string> imageList = new List<string>(m_LoadedVegetation[bundleList[0]].Keys);
					newLayer.path = bundleList[0] + "/" + imageList[0];
					newLayer.image = m_LoadedVegetation[bundleList[0]][imageList[0]];
				}
				m_CurrentDetailLayerSet.Add ( newLayer );	
			}
		} else {
			DetailLayer newLayer = new DetailLayer();
			List<string> bundleList = new List<string>(m_LoadedVegetation.Keys);
			if ( bundleList.Count > 0 ){
				List<string> imageList = new List<string>(m_LoadedVegetation[bundleList[0]].Keys);
				newLayer.path = bundleList[0] + "/" + imageList[0];
				newLayer.image = m_LoadedVegetation[bundleList[0]][imageList[0]];
			}
			m_CurrentDetailLayerSet.Add ( newLayer );
		}
	}
	
	public void DeleteDetailLayer( int id ){
		DetailPrototype[] details = currentTerrainInstance.terrainData.detailPrototypes;
		
		if ( currentTerrainInstance != null && details.Length > id ){
			DetailPrototype[] newDetails = new DetailPrototype[details.Length-1];
			int count = 0;
			for ( int i = 0; i < details.Length; i++ ){
				if ( i != id ){	
					newDetails[count] = details[i];
					count++;
				}
			}
			currentTerrainInstance.terrainData.detailPrototypes = newDetails;
		}
		
		m_CurrentDetailLayerSet.RemoveAt( id );
	}
	
	public void UpdateDetailLayer( int id ){
		// apply any changes made in the current detail set into the terrain instance.
		if ( currentTerrainInstance != null ){
			bool valueChanged = false;
			
			DetailLayer currentLayer = m_CurrentDetailLayerSet[id];
			DetailPrototype[] details = currentTerrainInstance.terrainData.detailPrototypes;
			
			if ( currentLayer.image != details[id].prototypeTexture ) valueChanged = true;
			details[id].prototypeTexture = currentLayer.image;
			
			if ( currentLayer.minWidth != details[id].minWidth ) valueChanged = true;
			details[id].minWidth = m_CurrentDetailLayerSet[id].minWidth;
			
			if ( currentLayer.maxWidth != details[id].maxWidth ) valueChanged = true;
			details[id].maxWidth = m_CurrentDetailLayerSet[id].maxWidth;
			
			if ( currentLayer.minHeight != details[id].minHeight ) valueChanged = true;
			details[id].minHeight = m_CurrentDetailLayerSet[id].minHeight;
			
			if ( currentLayer.maxHeight != details[id].maxHeight ) valueChanged = true;
			details[id].maxHeight = m_CurrentDetailLayerSet[id].maxHeight;
			
			if ( currentLayer.healthyColor != details[id].healthyColor ) valueChanged = true;
			details[id].healthyColor = m_CurrentDetailLayerSet[id].healthyColor;
			
			if ( currentLayer.dryColor != details[id].dryColor ) valueChanged = true;
			details[id].dryColor = m_CurrentDetailLayerSet[id].dryColor;
			
			if ( currentLayer.renderMode != details[id].renderMode ) valueChanged = true;
			details[id].renderMode = m_CurrentDetailLayerSet[id].renderMode;
			
			// only apply the new data if something changed.
			if ( valueChanged == true ){
				currentTerrainInstance.terrainData.detailPrototypes = details;
			}
		}
	}
	
	public DetailLayer GetDetailLayer( int id ){
		return m_CurrentDetailLayerSet[id];	
	}
	
	public void SetDetailLayer( int id, DetailLayer newLayer ){
		m_CurrentDetailLayerSet[id] = newLayer;	
	}
	
	public int GetCurrentBrushIndex(){
		return m_CurrentBrushImageID;
	}
	
	public Color GetTerrainTextureColor( int textureID ){
		return m_CurrentTerrainSet.tints[textureID];
	}
	
	public TerrainShaderType GetCurrentTerrainShaderType(){
		return m_CurrentTerrainShaderType;
	}
	
	public void SetCurrentTerrainShaderType( TerrainShaderType newType ){
		m_CurrentTerrainShaderType = newType;
		
		if ( m_CurrentTerrainShaderType == TerrainShaderType.Simple ){	
			terrainMaterial.shader = Shader.Find ("Nature/Terrain/Bumped Specular");
			terrainMaterial.SetColor ( "_SpecColor", Color.black );
			terrainMaterial.SetFloat ( "_Shininess", 0.0f );
		} else {
			terrainMaterial.shader = Shader.Find ("AGF/AGF_TriplanarTerrain");
		}
		
		for ( int i = 0; i < m_CurrentTerrainSet.images.Length; i++ ){
			SetTexture ( i, m_CurrentTerrainSet.images[i], m_CurrentTerrainSet.normals[i], m_CurrentTerrainSet.paths[i] );
		}
		
		SetTerrainTiling( m_TerrainTiling, true );
		
		if ( currentTerrainInstance ){
			currentTerrainInstance.Flush();	
		}
	}
	
	public void SetCurrentProjectionPower( float power ){
		if ( m_CurrentProjectionPower != power ){
			terrainMaterial.SetFloat("_Projection", power);
		}
		m_CurrentProjectionPower = power;
	}
	
	public float GetCurrentProjectionPower(){
		return m_CurrentProjectionPower;	
	}
	
	public void SetTerrainMaterial( Material mat ){
		terrainMaterial = mat;
		
		if ( currentTerrainInstance != null ){
			currentTerrainInstance.materialTemplate = terrainMaterial;	
		}
	}
	
	public void SetVegetationDrawDistance( float newFloat ){
		m_VegetationDrawDistance = newFloat;	
		if ( currentTerrainInstance != null ){
			if ( currentTerrainInstance.detailObjectDistance != newFloat ){
				currentTerrainInstance.detailObjectDistance = newFloat;	
			}
		}
	}
	
	public float GetVegetationDrawDistance(){
		return m_VegetationDrawDistance;	
	}
	
	public void SetVegetationDensity( float newFloat ){
		m_VegetationDensity = newFloat;	
		if ( currentTerrainInstance != null ){
			if ( currentTerrainInstance.detailObjectDensity != newFloat ){	
				currentTerrainInstance.detailObjectDensity = newFloat;	
			}
		}
	}
	
	public float GetVegetationDensity(){
		return m_VegetationDensity;	
	}
	
	public int GetCurrentTextureIndex(){
		return m_CurrentTerrainImageID;	
	}



	public void SetCustomTexture( int id, Texture2D newColormap, Texture2D newNormalmap, int newTextureIndex){
		SetTexture (id, newColormap, newNormalmap, "");

//		m_CurrentTerrainSet.images[id] = newColormap;
//		m_CurrentTerrainSet.normals[id] = newNormalmap;
//		
//		if ( m_CurrentTerrainShaderType == TerrainShaderType.Triplanar ){
//			if ( id < 4 ){
//				terrainMaterial.SetTexture ("_Splat" + id.ToString(), newColormap );
//				terrainMaterial.SetTexture ("_Splat" + id.ToString() + "Bump", newNormalmap );
//			} else {
//				terrainMaterial.SetTexture ("_Side", newColormap );
//				terrainMaterial.SetTexture ("_SideBump", newNormalmap );
//			}
//		} else if ( m_CurrentTerrainShaderType == TerrainShaderType.Simple ) {
//			if ( id < 4 ){
//				if ( currentTerrainInstance != null ){
//					SplatPrototype[] prototypes = currentTerrainInstance.terrainData.splatPrototypes;
//					prototypes[id].texture = newColormap;
//					prototypes[id].normalMap = newNormalmap;
//					currentTerrainInstance.terrainData.splatPrototypes = prototypes;
//				}
//			}
//			//			} else {
//			//				terrainMaterial.SetTexture ("_Side", newColormap );
//			//				terrainMaterial.SetTexture ("_SideBump", newNormalmap );
//			//			}
//		}
//		
//		// also set the texture to the terrain extension.
//		if ( id == 0 ){
//			terrainExtensionMaterial.SetTexture("_MainTex", newColormap);
//			terrainExtensionMaterial.SetTexture("_BumpMap", newNormalmap);
//		}
//		
//		FindObjectOfType<AGF_AssetLoader> ().SetUsedCustomPaint (newTextureIndex, id, 5);
//		
//		Resources.UnloadUnusedAssets();
	}
	
	public void SetTexture( int id, Texture2D newColormap, Texture2D newNormalmap, string newPath ){
		m_CurrentTerrainSet.images[id] = newColormap;
		m_CurrentTerrainSet.normals[id] = newNormalmap;
		m_CurrentTerrainSet.paths[id] = newPath;
		
		if ( m_CurrentTerrainShaderType == TerrainShaderType.Triplanar ){
			if ( id < 4 ){
				terrainMaterial.SetTexture ("_Splat" + id.ToString(), newColormap );
				terrainMaterial.SetTexture ("_Splat" + id.ToString() + "Bump", newNormalmap );
			} else {
				terrainMaterial.SetTexture ("_Side", newColormap );
				terrainMaterial.SetTexture ("_SideBump", newNormalmap );
			}
		} else if ( m_CurrentTerrainShaderType == TerrainShaderType.Simple ) {
			if ( id < 4 ){
				if ( currentTerrainInstance != null ){
					SplatPrototype[] prototypes = currentTerrainInstance.terrainData.splatPrototypes;
					prototypes[id].texture = newColormap;
					prototypes[id].normalMap = newNormalmap;
					currentTerrainInstance.terrainData.splatPrototypes = prototypes;
				}
			}
		}
		
		// also set the texture to the terrain extension.
		if ( id == 0 ){
			terrainExtensionMaterial.SetTexture("_MainTex", newColormap);
			terrainExtensionMaterial.SetTexture("_BumpMap", newNormalmap);
		}
		
		Resources.UnloadUnusedAssets();
	}

	public void SetCustomVegetationTexture( int slotToChange, Texture2D newTexture, int newTextureIndex){
		SetVegetationTexture (slotToChange, newTexture, "");

//		if(m_CurrentDetailLayerSet.Count > slotToChange+1){
//			m_CurrentDetailLayerSet[slotToChange+1].image = newTexture;
//			FindObjectOfType<AGF_AssetLoader> ().SetUsedCustomVegetation (newTextureIndex, slotToChange+1, GetDetailLayerCount());
//		}
	}
	
	public void SetVegetationTexture( int id, Texture2D newTexture, string newPath ){
		if(m_CurrentDetailLayerSet.Count > id){
			// When calling this version of the function, use whatever values currently exist. we are only changing out the texture.
			DetailLayer currentSet = m_CurrentDetailLayerSet[id];
		
			SetVegetationTexture( id, newTexture, newPath, currentSet.minWidth, currentSet.maxWidth, 
				currentSet.minHeight, currentSet.maxHeight, currentSet.healthyColor, currentSet.dryColor, currentSet.active );
		}
		else
			print("Detail layer set does not contain id");
	}
	
	public void SetVegetationTexture( int id, Texture2D newTexture, string newPath, float minWidth, float maxWidth, float minHeight, float maxHeight, Color healthyColor, Color dryColor, bool enabled ){
		if ( currentTerrainInstance != null ){
			if ( currentTerrainInstance.terrainData.detailPrototypes.Length <= id ){
				// we will need to create a new detail layer.
				AddNewDetailLayer();
			}
		}
		
		m_CurrentDetailLayerSet[id].image = newTexture;
		m_CurrentDetailLayerSet[id].path = newPath;
		m_CurrentDetailLayerSet[id].minWidth = minWidth;
		m_CurrentDetailLayerSet[id].maxWidth = maxWidth;
		m_CurrentDetailLayerSet[id].minHeight = minHeight;
		m_CurrentDetailLayerSet[id].maxHeight = maxHeight;
		m_CurrentDetailLayerSet[id].healthyColor = healthyColor;
		m_CurrentDetailLayerSet[id].dryColor = dryColor;
		m_CurrentDetailLayerSet[id].active = enabled;
		
		if ( currentTerrainInstance != null ){
			UpdateDetailLayer( id );
		}
	}
	
	public void SetTint( int id, Color newColor ){
		m_CurrentTerrainSet.tints[id] = newColor;
		
		if ( id < 4 ){
			terrainMaterial.SetColor ("_Splat" + id.ToString() + "Tint", newColor );
		} else {
			terrainMaterial.SetColor ("_SideTint", newColor );
		}
		
		// also set the tint to the terrain extension.
		if ( id == 0 ){
			terrainExtensionMaterial.SetColor ("_Color", newColor);
		}
	}
	
	public void SetCurrentTextureIndex( int newID ){
		if ( newID == 4 ) return;
		m_CurrentTerrainImageID = newID;	
	}
	
	public void OnProjectileImpact( Vector3 position ){
		if ( currentTerrainInstance != null ){
			float size = Random.Range(5.0f, 10.0f);
			float depth = Random.Range (-1.25f, -0.75f);
			bool prevSculpt = m_SculptingActive;
			bool prevPaint = m_PaintingActive;
			bool prevPlant = m_PlantingActive;
			m_SculptingActive = true;
			m_PaintingActive = true;
			m_PlantingActive = false;
			ApplyTerrainBrush( position, depth, 0, 1.0f, size, 3, false );
			m_SculptingActive = prevSculpt;
			m_PaintingActive = prevPaint;
			m_PlantingActive = prevPlant;
		}
	}
	
	private float precisionFloat = 1.4f;
	private int m_HeightX = -1;
	private int m_HeightY = -1;
	private int m_AlphaX = -1;
	private int m_AlphaY = -1;
	private Vector2[,] hits = new Vector2[0,0];
	private float[,] alphaHits = new float[0,0];
	public void ApplyTerrainBrush( Vector3 position, float depth, int brushID, float brushBlend, float brushSize, int textureID, bool requireClick ){
		TerrainData data = currentTerrainInstance.terrainData;
			
		// convert from world to local coordinates.
		Vector3 local = position - currentTerrainInstance.transform.position;
		Vector3 nlocal = new Vector3(Mathf.InverseLerp(0f, data.size.x, local.x), 0f, Mathf.InverseLerp(0f, data.size.z, local.z));
		
		int useLayer = Random.Range (0, 3); // Range on ints is [inclusive] [exclusive]
		
		if ( m_SculptingActive ){
			// calculate scale and the color layer.
			float depthScale = depth / data.size.y;
			
			// determine the proper values for the GetHeights() calculation.
			int xBase = Mathf.FloorToInt(nlocal.x * data.heightmapWidth);
			int yBase = Mathf.FloorToInt(nlocal.z * data.heightmapHeight);
			int width = Mathf.FloorToInt(brushSize / (data.size.x / data.heightmapWidth));
			int height = Mathf.FloorToInt(brushSize / (data.size.z / data.heightmapHeight));
			
			// determine what width and length encapsulates the rotated brush.
			float diagonal = Mathf.Sqrt(width * width + height * height);
			Vector2 offset = new Vector2((diagonal - width)/2.0f, (diagonal - height)/2.0f);
			int encapWidth = Mathf.RoundToInt(diagonal);
			int encapHeight = Mathf.RoundToInt(diagonal);
			
			xBase = xBase - (width / 2) - Mathf.RoundToInt(offset.x);
			yBase = yBase - (height / 2) - Mathf.RoundToInt(offset.y);
			
			// adjust for grid size, but store the old width and height to use in determining which part of the brush we should use.
	//		int oldWidth = width;
	//		int oldHeight = height;
			float xOffset, yOffset;
			AdjustValues( ref xBase, ref yBase, ref encapWidth, ref encapHeight, out xOffset, out yOffset, Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y), data.heightmapWidth, data.heightmapHeight );
			
			// get the heights.
			float[,] heights = data.GetHeights (xBase, yBase, encapWidth, encapHeight);
			float currentGridHeight = 0; // grab from level loader?
			
			// for all heights in the affected range, lower or raise them in accordance with the texture template.
			float currentTerrainHeight = currentTerrainInstance.transform.position.y;
			float heightmapYScale = currentTerrainInstance.terrainData.heightmapScale.y;
			float epsilon = 0.1f;
			
			// construct a table that records which cells have been hit.
			if ( m_HeightX != heights.GetLength(0) || m_HeightY != heights.GetLength(1) ){
				m_HeightX = heights.GetLength(0);
				m_HeightY = heights.GetLength(1);
				hits = new Vector2[m_HeightX, m_HeightY];
				for ( int i = 0; i < m_HeightX; i++ ){
					for ( int j = 0; j < m_HeightY; j++ ){
						hits[i,j] = new Vector2(0,0);
					}
				}
			} else {
				for ( int i = 0; i < m_HeightX; i++ ){
					for ( int j = 0; j < m_HeightY; j++ ){
						hits[i,j].Set(0,0);
					}
				}
			}
			
			Vector3 dir = new Vector3();
			Quaternion rotationQuat = Quaternion.AngleAxis( m_CurrentBrushRotation, Vector3.up );
			float d;
			float worldHeight;
			int widthPrecision = (int)(width * precisionFloat);
			int heightPrecision = (int)(height * precisionFloat);
			for (int x = 0; x < widthPrecision; x++) {
				for (int y = 0; y < heightPrecision; y++) {
					// construct the direction vector.
					dir.Set(x/precisionFloat - width/2.0f, 0, y/precisionFloat - height/2.0f);
					
					// rotate the direction vector.
					dir = rotationQuat * dir;
					
					// add the offset to the dir vector to get the result.
					dir.x = dir.x + offset.x + width/2.0f + xOffset;
					dir.z = dir.z + offset.y + height/2.0f + yOffset;
					
					// convert from a float to an int, to index into the array.
					int rotX = Mathf.RoundToInt(dir.x);
					if ( rotX < 0 ) rotX = 0; if ( rotX > encapWidth - 1 ) rotX = encapWidth - 1;
					int rotY = Mathf.RoundToInt(dir.z);
					if ( rotY < 0 ) rotY = 0; if ( rotY > encapHeight - 1 ) rotY = encapHeight - 1;
					
					int cx = Mathf.RoundToInt((Mathf.InverseLerp(0, width, x/precisionFloat)) * brushImages[brushID].brushWidth);
					int cy = Mathf.RoundToInt((Mathf.InverseLerp(0, height, y/precisionFloat)) * brushImages[brushID].brushHeight);
					
					d = m_CurrentTemplate[cy * brushImages[brushID].brushWidth + cx][useLayer];
					worldHeight = heights[rotY, rotX] * heightmapYScale + currentTerrainHeight;
					
					// NOTE: The original algorithm uses heights[x,y], but [y,x] seems to be more correct. is this right?
					if ( hits[rotY, rotX].y == 0 ){
						if ( (worldHeight + epsilon) <= currentGridHeight ){
							heights[rotY, rotX] = ((currentGridHeight-currentTerrainHeight) - ((currentGridHeight-worldHeight) * (1-d)))/(heightmapYScale);
							hits[rotY, rotX].Set(-1,1);
						} else {
							hits[rotY, rotX].Set(d,1);
						}
					}
				}
			}
			
			for ( int i = 0; i < m_HeightX; i++ ){
				for ( int j = 0; j < m_HeightY; j++ ){
					if ( hits[i,j].x != -1 && hits[i,j].y == 1 ){
						heights[i, j] += (hits[i,j].x * depthScale);
						
						// prevent the terrain from going below the grid.
						if ( heights[i, j] * heightmapYScale + currentTerrainHeight < currentGridHeight ){
							heights[i, j] = (currentGridHeight - currentTerrainHeight)/heightmapYScale;
						}
					}
				}
			}
			
			// set the new heights to the terrain.
			data.SetHeights (xBase , yBase , heights);	
		}
		
		if ( m_PaintingActive ){
			// next, we want to paint the terrain with our target texture, in the given area.
			int alphaWidth = Mathf.FloorToInt(brushSize / (data.size.x / data.alphamapWidth));
			int alphaHeight = Mathf.FloorToInt(brushSize / (data.size.z / data.alphamapHeight));
			float diagonal = Mathf.Sqrt(alphaWidth * alphaWidth + alphaHeight * alphaHeight);
			Vector2 offset = new Vector2((diagonal - alphaWidth)/2.0f, (diagonal - alphaHeight)/2.0f);
			
			int alphaXBase = Mathf.FloorToInt(nlocal.x * data.alphamapWidth) - Mathf.RoundToInt(offset.x);
			int alphaYBase = Mathf.FloorToInt(nlocal.z * data.alphamapHeight) - Mathf.RoundToInt(offset.y);
			
			int alphaEncapWidth = Mathf.RoundToInt(diagonal);
			int alphaEncapHeight = Mathf.RoundToInt(diagonal);
			alphaXBase -= (alphaWidth / 2);
			alphaYBase -= (alphaHeight / 2);
			
			// adjust for grid size, but store the old width and height to use in determining which part of the brush we should use.
			float alphaXOffset, alphaYOffset;
			AdjustValues( ref alphaXBase, ref alphaYBase, ref alphaEncapWidth, ref alphaEncapHeight, out alphaXOffset, out alphaYOffset, Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y), data.alphamapWidth, data.alphamapHeight );
			
			// get the alpha values.
			float[,,] alphas = data.GetAlphamaps(alphaXBase, alphaYBase, alphaEncapWidth, alphaEncapHeight);
			
			if ( m_AlphaX != alphas.GetLength(0) || m_AlphaY != alphas.GetLength (1) ){
				m_AlphaX = alphas.GetLength(0);
				m_AlphaY = alphas.GetLength(1);
				
				alphaHits = new float[m_AlphaX, m_AlphaY];
			}
			
			for ( int i = 0; i < m_AlphaX; i++ ){
				for ( int j = 0; j < m_AlphaY; j++ ){
					alphaHits[i,j] = -1;	
				}
			}
			
			Vector3 dir = new Vector3();
			Quaternion rotationQuat = Quaternion.AngleAxis( m_CurrentBrushRotation, Vector3.up );
			int alphaWidthPrecision = (int)(alphaWidth * precisionFloat);
			int alphaHeightPrecision = (int)(alphaHeight * precisionFloat);
			// for all alpha values in the affected range, paint the new texture. blend it in with the old textures.
			for ( int i = 0; i < alphaWidthPrecision; i++ ){
				for ( int j = 0; j < alphaHeightPrecision; j++ ){
					// construct the direction vector.
					dir.Set(i/precisionFloat - alphaWidth/2.0f, 0, j/precisionFloat - alphaHeight/2.0f);
					
					// rotate the direction vector.
					dir = rotationQuat * dir;
					
					// add the offset to the dir vector to get the result.
					dir.x = dir.x + offset.x + alphaWidth/2.0f + alphaXOffset;
					dir.z = dir.z + offset.y + alphaHeight/2.0f + alphaYOffset;
					
					// convert from a float to an int, to index into the array.
					int rotX = Mathf.RoundToInt(dir.x);
					if ( rotX < 0 ) rotX = 0; if ( rotX > alphaEncapWidth - 1 ) rotX = alphaEncapWidth - 1;
					int rotY = Mathf.RoundToInt(dir.z);
					if ( rotY < 0 ) rotY = 0; if ( rotY > alphaEncapHeight - 1 ) rotY = alphaEncapHeight - 1;
					
					int cx = Mathf.RoundToInt((Mathf.InverseLerp(0, alphaWidth, i/precisionFloat)) * brushImages[brushID].brushWidth);
					int cy = Mathf.RoundToInt((Mathf.InverseLerp(0, alphaHeight, j/precisionFloat)) * brushImages[brushID].brushHeight);
					
					alphaHits[rotY,rotX] = m_CurrentTemplate[cy * brushImages[brushID].brushWidth + cx][useLayer];
				}
			}
			
			for ( int i = 0; i < m_AlphaX; i++ ){
				for ( int j = 0; j < m_AlphaY; j++ ){
					if ( alphaHits[i,j] > -1 ){
						for ( int k = 0; k < 4; k++ ){
							// perform blending.
							if ( k == textureID ){
								// we should only ever add to the current texture.
								// if it's already greater than our current blend, do not reduce it.
								if ( alphas[i,j,k] > brushBlend ){
									
								} else {
		//							alphaHits[rotY,rotX,z] = d * brushBlend;
									alphas[i,j,k] += alphaHits[i,j] * brushBlend;
									if ( alphas[i,j,k] > brushBlend ){
										alphas[i,j,k] = brushBlend;	
									}
								} 
								
							} else {
								// we should only ever remove from other textures.
								// if it becomes lower than the current blend, cap it.
								// (note that if it was already lower, we don't raise it up)
								if ( alphas[i,j,k] > ( 1 - brushBlend ) ){
									alphas[i,j,k] -= alphaHits[i,j] * brushBlend;	
									if ( alphas[i,j,k] < ( 1 - brushBlend ) ){
										alphas[i,j,k] = ( 1 - brushBlend );
									}
								}
							}
						}
					}
				}
			}
		
			// set the new alphas to the terrain.
			data.SetAlphamaps(alphaXBase, alphaYBase, alphas);
		}
		
		// lastly, do the details.
		if ( m_PlantingActive ){
		
			int detailWidth = Mathf.FloorToInt(brushSize / (data.size.x / data.detailWidth));
			int detailHeight = Mathf.FloorToInt(brushSize / (data.size.z / data.detailHeight));
			float diagonal = Mathf.Sqrt(detailWidth * detailWidth + detailHeight * detailHeight);
			Vector2 offset = new Vector2((diagonal - detailWidth)/2.0f, (diagonal - detailHeight)/2.0f);
			
			int detailXBase = Mathf.FloorToInt ( nlocal.x * data.detailWidth ) - Mathf.RoundToInt(offset.x);
			int detailYBase = Mathf.FloorToInt ( nlocal.z * data.detailHeight ) - Mathf.RoundToInt(offset.y);
			
			int detailEncapWidth = Mathf.RoundToInt(diagonal);
			int detailEncapHeight = Mathf.RoundToInt(diagonal);
			detailXBase -= (detailWidth / 2);
			detailYBase -= (detailHeight / 2);
			
			// adjust for grid size, but store the old width and height to use in determining which part of the brush we should use.
			float detailXOffset, detailYOffset;
			AdjustValues( ref detailXBase, ref detailYBase, ref detailEncapWidth, ref detailEncapHeight, out detailXOffset, out detailYOffset, Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y), data.detailWidth, data.detailHeight );
	
//			print ( detailWidth + " " + detailHeight );
			
			List<int[,]> detailMaps = new List<int[,]>();
			for ( int i = 0; i < data.detailPrototypes.Length; i++ ){
				detailMaps.Add ( data.GetDetailLayer(detailXBase, detailYBase, detailEncapWidth, detailEncapHeight, i) );	
			}
	 
			Vector3 dir = new Vector3();
			Quaternion rotationQuat = Quaternion.AngleAxis( m_CurrentBrushRotation, Vector3.up );
			// write all detail data to terrain data:
			int detailWidthPrecision = (int)(detailWidth * precisionFloat);
			int detailHeightPrecision = (int)(detailHeight * precisionFloat);
			if ( detailMaps.Count > 0 ){
				for ( int i = 0; i < detailWidthPrecision; i++ ){
					for ( int j = 0; j < detailHeightPrecision; j++ ){
						// construct the direction vector.
						dir.Set(i/precisionFloat - detailWidth/2.0f, 0, j/precisionFloat - detailHeight/2.0f);
						
						// rotate the direction vector.
						dir = rotationQuat * dir;
						
						// add the offset to the dir vector to get the result.
						dir.x = dir.x + offset.x + detailWidth/2.0f + detailXOffset;
						dir.z = dir.z + offset.y + detailHeight/2.0f + detailYOffset;
						
						// convert from a float to an int, to index into the array.
						int rotX = Mathf.RoundToInt(dir.x);
						if ( rotX < 0 ) rotX = 0; if ( rotX > detailEncapWidth - 1 ) rotX = detailEncapWidth - 1;
						int rotY = Mathf.RoundToInt(dir.z);
						if ( rotY < 0 ) rotY = 0; if ( rotY > detailEncapHeight - 1 ) rotY = detailEncapHeight - 1;
						
						int cx = Mathf.RoundToInt((Mathf.InverseLerp(0, detailWidth, i/precisionFloat)) * brushImages[brushID].brushWidth);
						int cy = Mathf.RoundToInt((Mathf.InverseLerp(0, detailHeight, j/precisionFloat)) * brushImages[brushID].brushHeight);
						
						float templateValue = m_CurrentTemplate[cy * brushImages[brushID].brushWidth + cx][useLayer];
						int densityValue = (int)(templateValue/0.2f);
						
						// if depth is less than zero, just zero out all template values.
						if ( depth < 0 ) {
							if ( templateValue > 0.5f ){
								for ( int k = 0; k < detailMaps.Count; k++ ){
									if ( m_CurrentDetailLayerSet[k].active ){
										detailMaps[k][rotY,rotX] = 0;	
									}
								}
							}
						} else {
							int random = Random.Range (0,detailMaps.Count);
							if ( m_CurrentDetailLayerSet[random].active ){
								if ( densityValue > detailMaps[random][rotY,rotX] ) detailMaps[random][rotY,rotX] = densityValue;
							}
						}
						
					}
				}
			}
			
			for ( int i = 0; i < data.detailPrototypes.Length; i++ ){
				data.SetDetailLayer( detailXBase, detailYBase, i, detailMaps[i] );
			}
		}
	}
	
	public void UpdateWaterScale(){
		if ( currentWaterInstance != null ){
			currentWaterInstance.transform.localScale = new Vector3(m_CurrentTerrainSet.terrainSize.x * 0.05f, 1, m_CurrentTerrainSet.terrainSize.y * 0.05f);
		}
	}
	
	public void CreateNewTerrain( float size, int resolution, float terrainHeight = -1.0f ){
		// instantiate the terrain model. (if it is not null, destroy it first.)
		DestroyTerrain();
		currentTerrainInstance = (Terrain)Instantiate(terrainPrefab);
		currentTerrainInstance.terrainData.heightmapResolution = resolution;
		
		#if UNITY_EDITOR
			currentTerrainInstance.name = Main.TrimEndFromString( currentTerrainInstance.name, "(Clone)" );
		#endif
		
		ResetTerrainPrefab();
		
		// instantiate the terrain extension.
		m_TerrainExtensionPrefab = (Transform)Instantiate(terrainExtensionPrefab);
		m_TerrainExtensionPrefab.rotation = Quaternion.Euler( new Vector3(0,180,0) );
		
		#if UNITY_EDITOR
			m_TerrainExtensionPrefab.name = Main.TrimEndFromString( m_TerrainExtensionPrefab.name, "(Clone)" );
		#endif
		
		// set the terrain splat prototypes.
		ResetSplatPrototypes();
		
		// set the shader.
		SetCurrentTerrainShaderType( TerrainShaderType.Triplanar );
		
		// Set the Size of the terrain, and terrain extension.
		SetTerrainSize( size );
		Vector3 terrainSize = currentTerrainInstance.terrainData.size;
		
		// Set the positions of the terrain and terrain extension.
		if ( terrainHeight == -1.0f ){
			m_InitialHeight = -terrainSize.y/2; //GameObject.Find ("DrawnGridManager").GetComponent<DrawnGridManager>().GetCurrentHeight() - terrainSize.y/2;
		} else {
			m_InitialHeight = terrainHeight - terrainSize.y/2;
		}
		
		// set the details to whatever we currently have stored.
		for ( int i = 0; i < m_CurrentDetailLayerSet.Count; i++ ){
			SetVegetationTexture( i, m_CurrentDetailLayerSet[i].image, m_CurrentDetailLayerSet[i].path );	
		}
		
		// enable the pieces.
		m_TerrainExtensionPrefab.gameObject.SetActive(true);
		currentTerrainInstance.enabled = true;
	}
	
	public void SetCurrentHeightmapScale( int newHeightmapScale ){
		Debug.Log("Heightmap Scale = "+newHeightmapScale);
		
		SetTerrainSize();
	}
	
	public void SetCurrentHeightmapResolution( int newHeightmapResolution ){
		if ( m_CurrentHeightmapResolution != newHeightmapResolution ){
			m_CurrentHeightmapResolution = newHeightmapResolution;
			
			if ( currentTerrainInstance != null ){
				currentTerrainInstance.terrainData.heightmapResolution = newHeightmapResolution;
				
				// when the resolution changes, the size needs to be reset.
				SetTerrainSize();
				
				float offset = m_TerrainExtensionOffset;
				
				ResetTerrainPrefab();
				AdjustHeightmapLevel( offset );
			}
		}
	}
	
	public int GetHeightmapResolution(){
		return m_CurrentHeightmapResolution;	
	}
	
	public void SetCurrentDetailmapResolution( int newDetailmapResolution ){
		if ( m_CurrentDetailmapResolution != newDetailmapResolution ){
			m_CurrentDetailmapResolution = newDetailmapResolution;
			
			if ( currentTerrainInstance != null ){
				currentTerrainInstance.terrainData.SetDetailResolution( newDetailmapResolution, 8 );
			}
		}
	}
	
	public int GetDetailmapResolution(){
		return m_CurrentDetailmapResolution;	
	}
	
	public void SetTerrainPosition( Vector3 newPos ){
		Debug.Log("Set Terrain Position");
		
		newPos.y = -m_CurrentHeightmapHeightScale/2;
		currentTerrainInstance.transform.position = newPos;
		
		//newPos.y = newPos.y - m_TerrainExtensionOffset;
		Vector3 extensionPos = new Vector3(0, newPos.y + m_CurrentHeightmapHeightScale/2 + m_TerrainExtensionOffset, 0);
		//Vector3 extensionPos = new Vector3(0, newPos.y + m_CurrentHeightmapHeightScale/2 + m_TerrainExtensionOffset, 0);
		
		m_TerrainExtensionPrefab.position = extensionPos;
		
		extensionPos.y += 0.25f;
		//		m_EndOfWorldPrefab.position = extensionPos;
	}
	
	// when called without a parameter, reset the size, even if it is the same.
	public void SetTerrainSize(){
		float sameSize = m_CurrentTerrainSize;
		m_CurrentTerrainSize = m_CurrentTerrainSize + 1.0f;
		SetTerrainSize ( sameSize );
	}
	
	public void SetTerrainSize( float dimension ){
		if ( dimension != m_CurrentTerrainSize ){
			m_CurrentTerrainSize = dimension;
			
			if ( currentTerrainInstance != null ){
				Vector3 terrainSize = new Vector3( dimension, m_CurrentHeightmapHeightScale, dimension );
				currentTerrainInstance.terrainData.size = terrainSize;
				
				int yPos = 0;
				if(m_CurrentHeightmapHeightScale == 100)
					yPos = -50;
				else if(m_CurrentHeightmapHeightScale == 200)
					yPos = -100;
				else if(m_CurrentHeightmapHeightScale == 400)
					yPos = -200;
				
				// because size increase from the corner and not the center, set the position as well.
				SetTerrainPosition( new Vector3( -terrainSize.x/2.0f, yPos, -terrainSize.z/2.0f ) );
			} 
			
			if ( m_TerrainExtensionPrefab != null ){
				m_TerrainExtensionPrefab.localScale = new Vector3(dimension/250.0f, 1.0f, dimension/250.0f);
			}
			
			float tilingNumber = 200.0f * (dimension/250.0f) * m_TerrainTiling;
			terrainExtensionMaterial.SetTextureScale("_MainTex", new Vector2(tilingNumber, tilingNumber));
			terrainExtensionMaterial.SetTextureScale("_BumpMap", new Vector2(tilingNumber, tilingNumber));
			
			m_CurrentTerrainSet.terrainSize = new Vector2( dimension, dimension );
			
			// set the grid dimensions, now that the size has changed.
			AGF_GridManager gridManager = GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>(); 
			Vector3 newGridDimensions = new Vector3( dimension * (1/gridManager.GetCellSize()), gridManager.GetCurrentDimensions().y, dimension * (1/gridManager.GetCellSize()) );;
			gridManager.SetCurrentDimensions( newGridDimensions );
		}
	}
	
	public float GetTerrainSize(){
		return m_CurrentTerrainSize;	
	}
	
	public void SetWindSpeed( float newSpeed ){
		if ( newSpeed != m_WindSpeed ){
			m_WindSpeed = newSpeed;
			
			if ( currentTerrainInstance != null ){	
				currentTerrainInstance.terrainData.wavingGrassAmount = newSpeed;	
				currentTerrainInstance.terrainData.wavingGrassSpeed = newSpeed;	
				currentTerrainInstance.terrainData.wavingGrassStrength = newSpeed;
			}
		}
	}
	
	public float GetWindSpeed(){
		return m_WindSpeed;	
	}
	
	public void CreateNewWater( float waterHeight ){
		// instantiate the water model.
		DestroyWater();
		currentWaterInstance = (Transform)Instantiate(waterPrefab);
		
		#if UNITY_EDITOR
			currentWaterInstance.name = Main.TrimEndFromString( currentWaterInstance.name, "(Clone)" );
		#endif
		
		SetWaterHeight( waterHeight );
		UpdateWaterScale(); // update the water scale based on the terrain size.
		
		// enable the piece.
		currentWaterInstance.gameObject.SetActive(true);
	}
	
	public void DestroyWater(){
		// destroy the current water model.
		if ( currentWaterInstance != null ){
			DestroyImmediate (currentWaterInstance.gameObject);	
		}	
	}
	
	public void DestroyTerrain(){
		// destroy the current terrain model.
		if ( currentTerrainInstance != null ){
			DestroyImmediate (currentTerrainInstance.gameObject);	
		}
		
		// also destroy the extension prefab.
		if ( m_TerrainExtensionPrefab != null ){
			DestroyImmediate (m_TerrainExtensionPrefab.gameObject);	
		}
	}
	
	public void SetTerrainVisible( bool visible, bool activeSet = false ){
		if ( currentTerrainInstance != null ){
			currentTerrainInstance.gameObject.SetActive(visible);	
		}
		if ( m_TerrainExtensionPrefab != null ){
			m_TerrainExtensionPrefab.gameObject.SetActive(visible);	
		}
		
		if ( activeSet ){
			m_CurrentTerrainSet.terrainVisible = visible;	
		}
	}
	
	public void SetWaterVisible( bool visible, bool activeSet = false ){
		if ( currentWaterInstance != null ){
			currentWaterInstance.gameObject.SetActive(visible);	
			if ( activeSet ){
				m_CurrentWaterSettings.waterVisible = visible;
			}
		}
	}
	
	public bool IsTerrainVisible(){
		if ( currentTerrainInstance != null ){
			return currentTerrainInstance.gameObject.activeInHierarchy;	
		}
		return false;
	}
	
	public bool IsWaterVisible(){
		if ( currentWaterInstance != null ){
			return currentWaterInstance.gameObject.activeInHierarchy;	
		}
		return false;
	}
	
	public float GetCurrentWaterHeight(){
		return m_CurrentWaterHeight;
	}
	
	public float GetCurrentTerrainHeight(){
		if ( currentTerrainInstance != null ){
			return (currentTerrainInstance.transform.position.y + currentTerrainInstance.terrainData.size.y/2);
		}
		return -1;
	}
	
	public bool IsUnderwater(){
		return m_IsUnderwater;
	}
	
	public void SetWaterHeight( float waterHeight ){
		m_CurrentWaterHeight = waterHeight;
		if ( currentWaterInstance != null ){
			currentWaterInstance.transform.position = new Vector3(0,m_CurrentWaterHeight - 0.01f,0);	// offset height by a small epsilon.
		}
	}
	
	public void SetWaterReflectionColor( Color newColor ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_ReflectionColor") ){
						t.GetComponent<Renderer>().sharedMaterial.SetColor("_ReflectionColor", newColor );
						m_CurrentWaterSettings.waterReflectionColor = newColor;
						return;
					}
				}
			}
		}
	}
	
	public Color GetWaterReflectionColor(){
		return  m_CurrentWaterSettings.waterReflectionColor;
	}
	
	public void SetWaterRefractionColor( Color newColor ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_BaseColor") ){
						t.GetComponent<Renderer>().sharedMaterial.SetColor("_BaseColor", newColor );
						m_CurrentWaterSettings.waterRefractionColor = newColor;
						return;
					}
				}
			}
		}
	}
	
	public Color GetWaterRefractionColor(){
		return  m_CurrentWaterSettings.waterRefractionColor;
	}
	
	public void SetWaterSpecularColor( Color newColor ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_SpecularColor") ){
						t.GetComponent<Renderer>().sharedMaterial.SetColor("_SpecularColor", newColor );
						m_CurrentWaterSettings.waterSpecularColor = newColor;
						return;
					}
				}
			}
		}
	}
	
	public Color GetWaterSpecularColor(){
		return m_CurrentWaterSettings.waterSpecularColor;	
	}
	
	public void SetWaterReflectionIntensity( float intensity ){
		if ( currentWaterInstance != null ){
			foreach( Transform t in currentWaterInstance ){	
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_FresnelScale") ){
						t.GetComponent<Renderer>().sharedMaterial.SetFloat ( "_FresnelScale", intensity );
						m_CurrentWaterSettings.waterReflectionIntensity = intensity;
						return;
					}
				}
			}
		}
	}
	
	public float GetWaterReflectionIntensity(){
		return m_CurrentWaterSettings.waterReflectionIntensity;	
	}
	
	public void SetWaterReflectionWeight( float newWeight ){
		if ( currentWaterInstance != null ){
			foreach( Transform t in currentWaterInstance ){	
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_DistortParams") ){
						Vector4 distortParams = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_DistortParams" );
						distortParams.w = newWeight;
						t.GetComponent<Renderer>().sharedMaterial.SetVector ( "_DistortParams", distortParams );
						m_CurrentWaterSettings.waterReflectionWeight = newWeight;
						return;
					}
				}
			}
		}
	}
	
	public float GetWaterReflectionWeight(){
		return m_CurrentWaterSettings.waterReflectionWeight;
	}
	
	public void SetWaterSpecularDirection( Vector3 direction ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_WorldLightDir") ){
						t.GetComponent<Renderer>().sharedMaterial.SetVector("_WorldLightDir", direction );
						return;
					}
				}
			}
		}
	}
	
	public void SetWaterShoreFade( float newFade ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_InvFadeParemeter") ){
						Vector4 fadeParameter = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_InvFadeParemeter" );
						fadeParameter.y = newFade;
						t.GetComponent<Renderer>().sharedMaterial.SetVector("_InvFadeParemeter", fadeParameter );
						m_CurrentWaterSettings.waterShoreFade = newFade;
						return;
					}
				}
			}
		}
	}
	
	public float GetWaterShoreFade(){
		return m_CurrentWaterSettings.waterShoreFade;	
	}
	
	public void SetWaterFoamIntensity( float newIntensity ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_Foam") ){
						Vector4 foamParameter = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_Foam" );
						foamParameter.x = newIntensity;
						t.GetComponent<Renderer>().sharedMaterial.SetVector("_Foam", foamParameter );
						m_CurrentWaterSettings.waterFoamIntensity = newIntensity;
						return;
					}
				}
			}
		}
	}
	
	public float GetWaterFoamIntensity(){
		return m_CurrentWaterSettings.waterFoamIntensity;
	}
	
	public void SetWaterFoamWaves( float newFoam ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_Foam") ){
						Vector4 foamParameter = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_Foam" );
						foamParameter.y = newFoam;
						t.GetComponent<Renderer>().sharedMaterial.SetVector("_Foam", foamParameter );
						m_CurrentWaterSettings.waterFoamWaves = newFoam;
						return;
					}
				}
			}
		}
	}
	
	public float GetWaterFoamWaves(){
		return m_CurrentWaterSettings.waterFoamWaves;	
	}
	
//	(Wave 1 (X,Y) and 2 (Z,W) AB
//	(Wave 3 (X,Y) and 4 (Z,W) CD
		
	public void SetWaveAValues( float angle, float magnitude ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_GDirectionAB") ){
						m_CurrentWaterSettings.waveAAngle = angle;
						m_CurrentWaterSettings.waveAMagnitude = magnitude;
						
						Vector4 waveData = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_GDirectionAB" );
						
						// determine the vector2 based on angle, then shrink based on magnitude.
						angle *= Mathf.Deg2Rad;
						Vector2 waveDir = new Vector2( Mathf.Cos (angle), Mathf.Sin (angle) );
						waveDir *= magnitude;
						
						// assign back into the material vector.
						waveData.x = waveDir.x;
						waveData.y = waveDir.y;
						t.GetComponent<Renderer>().sharedMaterial.SetVector( "_GDirectionAB", waveData );
						
						return;
					}
				}
			}
		}
	}
	
	public void GetWaveAValues( out float angle, out float magnitude ){
		angle = m_CurrentWaterSettings.waveAAngle;
		magnitude =	m_CurrentWaterSettings.waveAMagnitude;
	}
	
	public void SetWaveBValues( float angle, float magnitude ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_GDirectionAB") ){
						m_CurrentWaterSettings.waveBAngle = angle;
						m_CurrentWaterSettings.waveBMagnitude = magnitude;
						
						Vector4 waveData = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_GDirectionAB" );
						
						// determine the vector2 based on angle, then shrink based on magnitude.
						angle *= Mathf.Deg2Rad;
						Vector2 waveDir = new Vector2( Mathf.Cos (angle), Mathf.Sin (angle) );
						waveDir *= magnitude;
						
						// assign back into the material vector.
						waveData.z = waveDir.x;
						waveData.w = waveDir.y;
						t.GetComponent<Renderer>().sharedMaterial.SetVector( "_GDirectionAB", waveData );
						 
						return;
					}
				}
			}
		}
	}
	
	public void GetWaveBValues( out float angle, out float magnitude ){
		angle = m_CurrentWaterSettings.waveBAngle;
		magnitude =	m_CurrentWaterSettings.waveBMagnitude;
	}
	
	public void SetWaveCValues( float angle, float magnitude ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_GDirectionCD") ){
						m_CurrentWaterSettings.waveCAngle = angle;
						m_CurrentWaterSettings.waveCMagnitude = magnitude;
						
						Vector4 waveData = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_GDirectionCD" );
						
						// determine the vector2 based on angle, then shrink based on magnitude.
						angle *= Mathf.Deg2Rad;
						Vector2 waveDir = new Vector2( Mathf.Cos (angle), Mathf.Sin (angle) );
						waveDir *= magnitude;
						
						// assign back into the material vector.
						waveData.x = waveDir.x;
						waveData.y = waveDir.y;
						t.GetComponent<Renderer>().sharedMaterial.SetVector( "_GDirectionCD", waveData );
						
						return;
					}
				}
			}
		}
	}
	
	public void GetWaveCValues( out float angle, out float magnitude ){
		angle = m_CurrentWaterSettings.waveCAngle;
		magnitude =	m_CurrentWaterSettings.waveCMagnitude;
	}
	
	public void SetWaveDValues( float angle, float magnitude ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>() != null ){
					if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_GDirectionCD") ){
						m_CurrentWaterSettings.waveDAngle = angle;
						m_CurrentWaterSettings.waveDMagnitude = magnitude;
						
						Vector4 waveData = t.GetComponent<Renderer>().sharedMaterial.GetVector( "_GDirectionCD" );
						
						// determine the vector2 based on angle, then shrink based on magnitude.
						angle *= Mathf.Deg2Rad;
						Vector2 waveDir = new Vector2( Mathf.Cos (angle), Mathf.Sin (angle) );
						waveDir *= magnitude;
						
						// assign back into the material vector.
						waveData.z = waveDir.x;
						waveData.w = waveDir.y;
						t.GetComponent<Renderer>().sharedMaterial.SetVector( "_GDirectionCD", waveData );
						
						return;
					}
				}
			}
		}
	}
	
	public void GetWaveDValues( out float angle, out float magnitude ){
		angle = m_CurrentWaterSettings.waveDAngle;
		magnitude =	m_CurrentWaterSettings.waveDMagnitude;
	}
	
	public void SetWaterClearColor( Color newColor ){
		if ( currentWaterInstance != null ){	
			currentWaterInstance.GetComponent<PlanarReflection>().clearColor = newColor;	
		}
	}
	
	public void SetWaveMagnitude( float power ){
		if ( currentWaterInstance != null ){
			foreach ( Transform t in currentWaterInstance ){
				if ( t.GetComponent<Renderer>().sharedMaterial.HasProperty("_GAmplitude") ){
					Vector3 amplitude = t.GetComponent<Renderer>().sharedMaterial.GetVector("_GAmplitude");
					t.GetComponent<Renderer>().sharedMaterial.SetVector("_GAmplitude", amplitude.normalized * power );
					m_CurrentWaterSettings.wavePower = power;
					return;
				}
			}
		}
	}
	
	public float GetWaveMagnitude(){
		return m_CurrentWaterSettings.wavePower;
	}
	
	public float GetTerrainExtensionOffset(){
		return m_TerrainExtensionOffset;	
	}
	
	public void ResetTerrainPrefab(){
		if ( currentTerrainInstance != null ){
			TerrainData data = currentTerrainInstance.terrainData;
			float[,] heights = data.GetHeights(0,0,data.heightmapWidth, data.heightmapHeight);
			float[,,] alphas = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
			
			for ( int layerID = 0; layerID < data.detailPrototypes.Length; layerID++ ){
				int[,] details = data.GetDetailLayer(0 ,0, data.detailWidth, data.detailHeight, layerID );
				for ( int i = 0; i < data.detailWidth; i++ ){
					for ( int j = 0; j < data.detailHeight; j++ ){	
						details[i,j] = 0;
					}
				}
				data.SetDetailLayer( 0, 0, layerID, details );
			}
			
			// this process somehow fixes the broken-ness of the terrain.
			DetailPrototype[] newDetails = new DetailPrototype[1];
			newDetails[0] = new DetailPrototype();
			data.detailPrototypes = newDetails;
			data.detailPrototypes = new DetailPrototype[0];
			
			m_CurrentDetailLayerSet.Clear ();
			
			for (int i = 0; i < data.heightmapWidth; i++ ){
				for (int j = 0; j < data.heightmapHeight; j++ ){
					heights[i,j] = 0.5f;
				}
			}
			
			for (int i = 0; i < data.alphamapHeight; i++ ){
				for ( int j = 0; j < data.alphamapWidth; j++ ){
					alphas[i,j,0] = 1f;
					for (int k = 1; k < 4; k++ ){
						alphas[i,j,k] = 0f;
						
					}	
				}
			}
			
			data.SetHeights(0,0,heights);
			data.SetAlphamaps(0,0,alphas);
			
			// set the density and draw distance for the vegetation.
			SetVegetationDensity( m_VegetationDensity );
			SetVegetationDrawDistance( m_VegetationDrawDistance );
		}
		
		m_TerrainExtensionOffset = 0.0f;
		if ( m_TerrainExtensionPrefab != null ){
			SetTerrainPosition( new Vector3(-currentTerrainInstance.terrainData.size.x/2,m_CurrentHeightmapHeightScale,-currentTerrainInstance.terrainData.size.z/2) );
		}
	}
	
	public void ResetSplatPrototypes(){
		SplatPrototype[] newPrototypes = new SplatPrototype[4];
		for ( int i = 0; i < newPrototypes.Length; i++ ){
			newPrototypes[i] = new SplatPrototype();
			newPrototypes[i].texture = (Texture2D)Resources.Load( "Effects/Terrain/Textures/1_c", typeof(Texture2D) );
			if ( newPrototypes[i].texture == null ){
				newPrototypes[i].texture = (Texture2D)Resources.Load( "Terrain/Textures/01", typeof(Texture2D) );
			}
		}
		if ( currentTerrainInstance != null ){
			currentTerrainInstance.terrainData.splatPrototypes = newPrototypes;
		}
	}
	
	public void AdjustHeightmapLevel( float worldDelta ){
		if ( currentTerrainInstance != null ){
			TerrainData data = currentTerrainInstance.terrainData;
			float[,] heights = data.GetHeights(0,0,data.heightmapWidth, data.heightmapHeight);
			
			// convert the world coordinate delta into a heightmap value.
			float heightmapYScale = data.heightmapScale.y;
			float heightmapDelta = worldDelta / heightmapYScale;
			
			for (int i = 0; i < data.heightmapWidth; i++ ){
				for (int j = 0; j < data.heightmapHeight; j++ ){
					heights[i,j] += heightmapDelta;
					if ( heights[i, j] < 0.0f ) heights[i,j] = 0.0f;
					if ( heights[i, j] > 1.0f ) heights[i,j] = 1.0f; 
				}
			}
			
			data.SetHeights(0,0,heights);
			
			if ( m_TerrainExtensionPrefab != null ){
				m_TerrainExtensionOffset += worldDelta;
				if ( m_TerrainExtensionOffset > heightmapYScale/2.0f ) m_TerrainExtensionOffset = heightmapYScale/2.0f;
				if ( m_TerrainExtensionOffset < 0 ) m_TerrainExtensionOffset = 0;
				
				Vector3 newPosition = currentTerrainInstance.transform.position; 
				newPosition.x = m_TerrainExtensionPrefab.position.x;
				newPosition.y = newPosition.y + 50.0f + m_TerrainExtensionOffset;
				newPosition.z = m_TerrainExtensionPrefab.position.z;
				m_TerrainExtensionPrefab.position = newPosition;
				
				newPosition.y += 0.25f;
			}
		}
	}
	
	private void AdjustValues( ref int xBase, ref int yBase, ref int encapWidth, ref int encapHeight, out float xOffset, out float yOffset, int rotOffsetX, int rotOffsetY, int maxX, int maxY ){
		// store the old height and width for use in calculating the x and y offsets.
		int oldHeight = encapHeight;
		int oldWidth = encapWidth;
		
		// the x and y offsets depend on whether or not our brush goes off the edge of the map when painting.
		bool offLeftEdge = false;
		bool offTopEdge = false;
		
		// left edge 
		if ( xBase < 0 ){
			encapWidth += xBase;
			xBase = 1;
			offLeftEdge = true;
		}
		
		// top edge
		if ( yBase < 0 ){
			encapHeight += yBase;
			yBase = 1;	
			offTopEdge = true;
		} 
		
		// right edge
		if ( (xBase + encapWidth) > maxX ){
			encapWidth += (maxX - (xBase + encapWidth) - 1 );
		}
		
		// bottom edge
		if ( (yBase + encapHeight) > maxY ){
			encapHeight += ( maxY - ( yBase + encapHeight ) - 1 );
		}
		
		// set the x and y offsets.
		if ( offLeftEdge ){
			xOffset = -(float)(oldWidth - encapWidth);
		} else {
			xOffset = 0;
		}
		if ( offTopEdge ){
			yOffset = -(float)(oldHeight - encapHeight);
		} else {
			yOffset = 0;
		}
	}
	
	public void SetTerrainTiling( float newTiling, bool forceUpdate = false ){
		if ( m_TerrainTiling != newTiling || forceUpdate ){
			m_TerrainTiling = newTiling;
			
//			float offset = (m_CurrentTerrainSize/(m_TerrainTiling * 0.5f)) * 0.25f;
			
			if ( m_CurrentTerrainShaderType == TerrainShaderType.Triplanar ){
				if ( terrainMaterial != null ){
					terrainMaterial.SetFloat( "_GlobalTiling", m_TerrainTiling );
				}
			} else if ( m_CurrentTerrainShaderType == TerrainShaderType.Simple ){
				if ( currentTerrainInstance != null ){
					SplatPrototype[] prototypes = currentTerrainInstance.terrainData.splatPrototypes;
					for ( int i = 0; i < prototypes.Length; i++ ){
						prototypes[i].tileSize = new Vector2( (1/(m_TerrainTiling)) * 10.0f, (1/(m_TerrainTiling)) * 10.0f);
						prototypes[i].tileOffset = new Vector2( 50.0f, 50.0f );
					}
					currentTerrainInstance.terrainData.splatPrototypes = prototypes;
				}
			}
			
			if ( terrainExtensionMaterial != null ){
				float tilingNumber = 200.0f * (m_CurrentTerrainSize/250.0f) * m_TerrainTiling;
				terrainExtensionMaterial.SetTextureScale("_MainTex", new Vector2(tilingNumber, tilingNumber));
				terrainExtensionMaterial.SetTextureScale("_BumpMap", new Vector2(tilingNumber, tilingNumber));
			}
		}
	}
	
	public void SetTerrainFromString( string str ){
		// rebuild the terrain from a given string.
		
		if ( str.Length == 0 ){
			return;	
		}
		
		// split the string by the '@' delimiter.
		string[] topLevelStrings = str.Split (new char[]{'@'});
		
		if(topLevelStrings.Length >= 47)
			float.TryParse(topLevelStrings[46], out m_CurrentHeightmapHeightScale);
		else
			m_CurrentHeightmapHeightScale = 100;
		
		
		int index = 0;
		
		// Get the Heightmap Width.
		int heightmapWidth;
		int.TryParse(topLevelStrings[index], out heightmapWidth);
		
		// Get the Heightmap Height.
		int heightmapHeight;
		int.TryParse(topLevelStrings[++index], out heightmapHeight);
		// create a new terrain if one does not already exist.
		CreateNewTerrain( 10, heightmapWidth, 10 );
		m_CurrentHeightmapResolution = heightmapWidth;
			
		// Get the Initial grid height.
		float.TryParse(topLevelStrings[++index], out m_InitialHeight);
		
		// Get the terrain extension offset.
		float.TryParse(topLevelStrings[++index], out m_TerrainExtensionOffset);
		
		// Get the projection power.
		float projectionPower;
		float.TryParse (topLevelStrings[++index], out projectionPower);
		SetCurrentProjectionPower( projectionPower );
		
		// Get the grid size.
		string[] gridSizeValues = topLevelStrings[++index].Split (new char[]{'*'});
		
		float gridSizeX, gridSizeY, gridSizeZ;
		float.TryParse(gridSizeValues[0], out gridSizeX);
		float.TryParse(gridSizeValues[1], out gridSizeY);
		float.TryParse(gridSizeValues[2], out gridSizeZ);
		
		SetTerrainSize( gridSizeX );
		
		// Get the Heightmap data.
		string heightmapData = topLevelStrings[++index];
		float[,] heights = new float[heightmapWidth,heightmapHeight];
		
		//-- Heightmap Data processing --//
		// split the string by the '~' delimiter.
		string[] rowHeights = heightmapData.Split (new char[]{'~'});
		
		for ( int i = 0; i < rowHeights.Length; i++ ){
			// split the string by the '$' delimiter.
			string[] cellHeight = rowHeights[i].Split(new char[]{'$'});	
			
			// if the cellHeight string length is one, there were no '$'s. This means that the row was filled with one value.
			if ( cellHeight.Length == 1 ){
				string[] countInfo = rowHeights[i].Split (new char[]{'#'});
				
				// a count must have been provided.
				int count;
				int.TryParse(countInfo[0], out count);
				float height;
				float.TryParse(countInfo[1], out height);
				
				// set the heights.
				for ( int x = 0; x < count; x++ ){
					heights[i,x] = height;	
				}
				
			} else {
				int currentCount = 0;
				for ( int j = 0; j < cellHeight.Length; j++ ){
					// now, to determine if a count has been provided for us or not.
					string[] countInfo = cellHeight[j].Split (new char[]{'#'});
					float height;
					int count;
					if ( countInfo.Length == 1 ){
						// no count has been provided. the cellHeight string is the value.
						count = 1;
						float.TryParse(cellHeight[j], out height);
					} else {
						// a count has been provided
						int.TryParse(countInfo[0], out count);
						
						if ( countInfo[1].Length == 0 ){
							// if the countInfo is equal to 1, that means that the height was dropped, and is such 0.
							height = 0;
						} else {
							float.TryParse(countInfo[1], out height);
						}
					}
					
					// set the heights.
					for ( int x = currentCount; x < currentCount + count; x++ ){
						heights[i,x] = height;
					}
					currentCount += count;
				}
			}
		}
		
		// set the heights into our terrain object.
		currentTerrainInstance.terrainData.SetHeights(0,0,heights);
		
		// Get the Alphamap Width.
		int alphamapWidth;
		int.TryParse(topLevelStrings[++index], out alphamapWidth);
		
		// Get the Alphamap Height.
		int alphamapHeight;
		int.TryParse(topLevelStrings[++index], out alphamapHeight);
		
		// Get the Alphamap Texture count.
		int alphamapTextureCount;
		int.TryParse(topLevelStrings[++index], out alphamapTextureCount);
		
		// Get the Alphamap data.
		string alphamapData = topLevelStrings[++index];
		float[,,] alphas = new float[alphamapWidth, alphamapHeight, alphamapTextureCount];
		
		//-- Alphamap Data processing --//
		// split the string by the '~' delimiter.
		string[] rowAlphas = alphamapData.Split(new char[]{'~'});
		
		for ( int i = 0; i < rowAlphas.Length; i++ ){
			// split the string by the '$' delimiter.
			string[] cellAlpha = rowAlphas[i].Split(new char[]{'$'});	
			
			// if the cellAlpha string length is one, there were no '$'s. This means that the row was filled with one value.
			if ( cellAlpha.Length == 1 ){
				string[] countInfo = rowAlphas[i].Split (new char[]{'#'});
				
				// a count must have been provided.
				int count;
				int.TryParse(countInfo[0], out count);
				
				// get the alphas.
				string[] alphaInfo = countInfo[1].Split(new char[]{'*'});
					
				for ( int x = 0; x < count; x++ ){
					for ( int y = 0; y < alphaInfo.Length; y++ ){
						
						if ( alphaInfo[y].Length == 0 ){
							// if the alpha length is zero, that means a zero has been truncated.
							alphas[i,x,y] = 0;
						} else {
							float alphaValue;
							float.TryParse(alphaInfo[y], out alphaValue); 
							
							alphas[i,x,y] = alphaValue;
						}
					}
				}
			} else {
				int currentCount = 0;
				for ( int j = 0; j < cellAlpha.Length; j++ ){
					// now, to determine if a count has been provided for us or not.
					string[] countInfo = cellAlpha[j].Split (new char[]{'#'});
					int count;
					string[] alphaInfo;
					if ( countInfo.Length == 1 ){
						// no count has been provided. the cellAlpha string is the value.
						count = 1;
						alphaInfo = cellAlpha[j].Split(new char[]{'*'});
						
					} else {
						// a count has been provided
						int.TryParse(countInfo[0], out count);
						
						alphaInfo = countInfo[1].Split(new char[]{'*'});
					}
					
					// set the alphas.
					for ( int x = currentCount; x < currentCount + count; x++ ){
						for ( int y = 0; y < alphaInfo.Length; y++ ){
							if ( alphaInfo[y].Length == 0 ){
								// if the alpha length is zero, that means a zero has been truncated.
								alphas[i,x,y] = 0;
							} else {
								float alphaValue;
								float.TryParse(alphaInfo[y], out alphaValue); 
								
								alphas[i,x,y] = alphaValue;
							}	
						}
					}
					currentCount += count;
				}
			}
		}
		
		// set the alphas into our terrain object.
		currentTerrainInstance.terrainData.SetAlphamaps(0,0,alphas);	
		
		// Get the detail map width.
		int detailmapWidth;
		int.TryParse( topLevelStrings[++index], out detailmapWidth );
		
		// Get the detail map height.
		int detailmapHeight;
		int.TryParse( topLevelStrings[++index], out detailmapHeight );
		currentTerrainInstance.terrainData.SetDetailResolution( detailmapWidth, 8 );
		m_CurrentDetailmapResolution = detailmapWidth;
		
		// Get the detail map count.
		int detailCount;
		int.TryParse( topLevelStrings[++index], out detailCount );
		
		// grab the detail information.
		string detailVegetationData = topLevelStrings[++index];
		string[] vegetationLayerData = detailVegetationData.Split( new char[]{'!'} );
//		print ( detailVegetationData + " " + vegetationLayerData.Length );
		
		m_CurrentDetailLayerSet = new List<DetailLayer>();
		for ( int i = 0; i < vegetationLayerData.Length; i++ ){
			string[] data = vegetationLayerData[i].Split( new char[]{'~'} );
			if ( data.Length > 1 ){
				int id = 0;
				
				m_CurrentDetailLayerSet.Add ( new DetailLayer() );
				m_CurrentDetailLayerSet[i].path = data[id];
				
				float.TryParse( data[++id], out m_CurrentDetailLayerSet[i].minWidth );
				float.TryParse( data[++id], out m_CurrentDetailLayerSet[i].maxWidth );
				float.TryParse( data[++id], out m_CurrentDetailLayerSet[i].minHeight );
				float.TryParse( data[++id], out m_CurrentDetailLayerSet[i].maxHeight );
				
				float r,g,b,a;
				float.TryParse( data[++id], out r );
				float.TryParse( data[++id], out g );
				float.TryParse( data[++id], out b );
				float.TryParse( data[++id], out a );
				
				m_CurrentDetailLayerSet[i].healthyColor = new Color( r,g,b,a );
				
				float.TryParse( data[++id], out r );
				float.TryParse( data[++id], out g );
				float.TryParse( data[++id], out b );
				float.TryParse( data[++id], out a );
				
				m_CurrentDetailLayerSet[i].dryColor = new Color( r,g,b,a );
				
				m_CurrentDetailLayerSet[i].active = (data[++id] == "1");
				
				string[] pathSplit = m_CurrentDetailLayerSet[i].path.Split ( new char[]{'/'} );
				
				SetVegetationTexture( i, m_LoadedVegetation[pathSplit[0]][pathSplit[1]], m_CurrentDetailLayerSet[i].path );
			}
		}
		
		// Get the Detailmap data.
		string detailmapData = topLevelStrings[++index];
		List<int[,]> detailMaps = new List<int[,]>();
		for ( int i = 0; i < detailCount; i++ ){
			detailMaps.Add ( new int[detailmapWidth, detailmapHeight] );
		}
		
		//-- Detailmap Data processing --//
		// split the string by the '!' delimiter.
		string[] detailMapStrings = detailmapData.Split(new char[]{'!'});
		
		for ( int layerID = 0; layerID < detailCount; layerID++ ){ 
			// split the string by the '~' delimiter.
			string[] rowDetails = detailMapStrings[layerID].Split(new char[]{'~'});	
			
			for ( int i = 0; i < rowDetails.Length; i++ ){
				// split the string by the '$' delimiter.
				string[] cellDetail = rowDetails[i].Split(new char[]{'$'});	
				
				// if the cellDetail string length is one, there were no '$'s. This means that the row was filled with one value.
				if ( cellDetail.Length == 1 ){
					string[] countInfo = rowDetails[i].Split (new char[]{'#'});
					
					// a count must have been provided.
					int count;
					int.TryParse(countInfo[0], out count);
					int density;
					int.TryParse(countInfo[1], out density);
					
					// set the densities.
					for ( int x = 0; x < count; x++ ){
						detailMaps[layerID][i,x] = density;	
					}
					
				} else {
					int currentCount = 0;
					for ( int j = 0; j < cellDetail.Length; j++ ){
						// now, to determine if a count has been provided for us or not.
						string[] countInfo = cellDetail[j].Split (new char[]{'#'});
						int density;
						int count;
						if ( countInfo.Length == 1 ){
							// no count has been provided. the cellDetail string is the value.
							count = 1;
							int.TryParse(cellDetail[j], out density);
						} else {
							// a count has been provided
							int.TryParse(countInfo[0], out count);
							
							if ( countInfo[1].Length == 0 ){
								// if the countInfo is equal to 1, that means that the density was dropped, and is such 0.
								density = 0;
							} else {
								int.TryParse(countInfo[1], out density);
							}
						}
						
						// set the densities.
						for ( int x = currentCount; x < currentCount + count; x++ ){
							detailMaps[layerID][i,x] = density;
						}
						currentCount += count;
					}
				}
				
			}
		}
		
		// set the details into our terrain object.
		for ( int i = 0; i < detailCount; i++ ){
//			print ( "SetDetailLayer " + detailMaps[i].GetLength(0) + " " + detailMaps[i].GetLength (1) + " " + currentTerrainInstance.terrainData.detailResolution + " " + currentTerrainInstance.terrainData.detailPrototypes.Length );
			currentTerrainInstance.terrainData.SetDetailLayer( 0,0, i, detailMaps[i] );
		}
		
		// set the vegetation density and draw distance.
		float outFloat;
		float.TryParse( topLevelStrings[++index], out outFloat );
		SetVegetationDensity( outFloat );
		float.TryParse( topLevelStrings[++index], out outFloat );
		SetVegetationDrawDistance( outFloat );
		
		// set the terrain images.
		for ( int i = 0; i < m_CurrentTerrainSet.paths.Length; i++ ){
			string path = topLevelStrings[++index];
			string[] pathSplit = path.Split ( new char[]{'/'} );
			SetTexture( i, m_LoadedColormaps[pathSplit[0]][pathSplit[1]],
						m_LoadedNormals[pathSplit[0]][pathSplit[1]],
						path );
		}
		
		// set the terrain colors.
		for ( int i = 0; i < m_CurrentTerrainSet.tints.Length; i++ ){
			float r,g,b,a;
			float.TryParse( topLevelStrings[++index], out r );
			float.TryParse( topLevelStrings[++index], out g );
			float.TryParse( topLevelStrings[++index], out b );
			float.TryParse( topLevelStrings[++index], out a );
			SetTint ( i, new Color(r,g,b,a) );
		}
		
		// Terrain visibility.
		SetTerrainVisible( topLevelStrings[++index] == "1", true );
		
		// Terrain tiling
		if ( index + 1 < topLevelStrings.Length ){
			float newTiling;
			float.TryParse( topLevelStrings[++index], out newTiling );
			SetTerrainTiling( newTiling, true );
		} else {
			SetTerrainTiling ( 1, true );	
		}
		
		// Terrain shader
		if ( index + 1 < topLevelStrings.Length ){
			if ( topLevelStrings[++index] == "1" ){
				SetCurrentTerrainShaderType( TerrainShaderType.Triplanar );	
			} else {
				SetCurrentTerrainShaderType( TerrainShaderType.Simple );
			}
		} else {
			SetCurrentTerrainShaderType( TerrainShaderType.Triplanar );
		}
	}
	
	public string GetWaterSerializationString(){
		string serializationString = "";
		
		// water height
		serializationString += m_CurrentWaterHeight.ToString() + "*";
		
		// water colors
		Color reflectionColor = m_CurrentWaterSettings.waterReflectionColor;
		Color refractionColor = m_CurrentWaterSettings.waterRefractionColor;
		
		serializationString += reflectionColor.r + "*" + reflectionColor.g + "*" + reflectionColor.b + "*" + reflectionColor.a + "*";
		serializationString += refractionColor.r + "*" + refractionColor.g + "*" + refractionColor.b + "*" + refractionColor.a + "*";
		
		// reflection values
		serializationString += m_CurrentWaterSettings.waterReflectionIntensity.ToString() + "*";	
		serializationString += m_CurrentWaterSettings.waterReflectionWeight.ToString() + "*";
		
		// wave data
		float angle, magnitude;
		GetWaveAValues( out angle, out magnitude );
		serializationString += angle + "*" + magnitude + "*";
	
		GetWaveBValues( out angle, out magnitude );
		serializationString += angle + "*" + magnitude + "*";
		
		GetWaveCValues( out angle, out magnitude );
		serializationString += angle + "*" + magnitude + "*";
		
		GetWaveDValues( out angle, out magnitude );
		serializationString += angle + "*" + magnitude + "*";
		
		serializationString += GetWaveMagnitude() + "*";
		
		// foam data
		serializationString += m_CurrentWaterSettings.waterFoamWaves.ToString() + "*";
		serializationString += m_CurrentWaterSettings.waterFoamIntensity.ToString() + "*";
		serializationString += m_CurrentWaterSettings.waterShoreFade.ToString() + "*";
		
		// water visibility 
		serializationString += m_CurrentWaterSettings.waterVisible ? "1*" : "0*";
		
		return serializationString;
	}
	
	public void SetWaterFromString( string serializationString ){
		string[] split = serializationString.Split (new char[]{'*'});
		
		int i = 0;
		
		// Get the Water height, and create a new water object.
		float waterHeight;
		float.TryParse (split[i], out waterHeight);
		CreateNewWater( waterHeight );
		
		// water colors.
		float r,g,b,a;
		float.TryParse( split[++i], out r );
		float.TryParse( split[++i], out g );
		float.TryParse( split[++i], out b );
		float.TryParse( split[++i], out a );
		SetWaterReflectionColor( new Color(r,g,b,a) );
		
		float.TryParse( split[++i], out r );
		float.TryParse( split[++i], out g );
		float.TryParse( split[++i], out b );
		float.TryParse( split[++i], out a );
		SetWaterRefractionColor( new Color(r,g,b,a) );
		
		// Reflection values
		float reflectionIntensity;
		float.TryParse( split[++i], out reflectionIntensity );
		SetWaterReflectionIntensity( reflectionIntensity );
		
		float reflectionWeight;
		float.TryParse( split[++i], out reflectionWeight );
		SetWaterReflectionWeight( reflectionWeight );
		
		// wave data
		float angle, magnitude;
		float.TryParse( split[++i], out angle );
		float.TryParse( split[++i], out magnitude );
		SetWaveAValues( angle, magnitude );
		
		float.TryParse( split[++i], out angle );
		float.TryParse( split[++i], out magnitude );
		SetWaveBValues( angle, magnitude );
		
		float.TryParse( split[++i], out angle );
		float.TryParse( split[++i], out magnitude );
		SetWaveCValues( angle, magnitude );
		
		float.TryParse( split[++i], out angle );
		float.TryParse( split[++i], out magnitude );
		SetWaveDValues( angle, magnitude );
		
		float.TryParse( split[++i], out magnitude );
		SetWaveMagnitude( magnitude );
		
		// foam data
		float foamWaves, foamIntensity, shoreFade;
		
		float.TryParse( split[++i], out foamWaves );
		SetWaterFoamWaves( foamWaves );
		
		float.TryParse( split[++i], out foamIntensity );
		SetWaterFoamIntensity( foamIntensity );
		
		float.TryParse( split[++i], out shoreFade );
		SetWaterShoreFade( shoreFade );
		
		// water visibility.
		int outInt;
		int.TryParse( split[++i], out outInt );
		SetWaterVisible( outInt == 1, true );
	}
	
	private void OnApplicationQuit(){
		ResetSplatPrototypes();	
	}
}
