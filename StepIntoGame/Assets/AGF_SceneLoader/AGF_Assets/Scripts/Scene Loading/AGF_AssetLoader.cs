using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AGF_AssetLoader : MonoBehaviour
{
	// Used in editor only
	public enum FileTypes
	{
		None, Audio, Brushes, DefaultBrushes, Extras,
		Images, Modules, Terrain, Vegetation,
	}

	//Used in both

	// default normal map
	public Texture2D normalMap;

	List<int> loadedPaint = new List<int> ();
	List<int> loadedVegetation = new List<int> ();
	public List<Texture2D> customPaintImages = new List<Texture2D>();
	public List<Texture2D> customPaintNormals = new List<Texture2D>();
	public List<Texture2D> customVegetation = new List<Texture2D>();
	public List<GameObject> customObjects = new List<GameObject>();
	public List<Texture2D> customObjectTextures = new List<Texture2D>();
	public List<Texture2D> customObjectNormals = new List<Texture2D>();
	public List<GameObject> placedCustomObjects = new List<GameObject>();
	public List<Texture2D> placedObjectTextures = new List<Texture2D>();

	// Used in externals
	string externalPath = "";
	string internalPath = "";

	public void LoadAssets(string newFilePath){
		externalPath = "";
		internalPath = Main.GetHomeFolder ();

		string[] pathSplit = newFilePath.Split ( new char[]{'/'} );

		List<string> pathArray = new List<string>();
		foreach(string split in pathSplit){
			pathArray.Add(split);
		}

		for(int i = 0; i < pathArray.Count -2; i++){
			if(externalPath == "")
				externalPath += pathArray[i];
			else
				externalPath += "/"+ pathArray[i];
		}

		internalPath += "/Assets/Resources/"/*+ pathArray[pathArray.Count - 3]*/;

		LoadPaint(externalPath+"/Textures/Terrain", internalPath /*+"/Textures/Terrain"*/);
		LoadVegetation(externalPath+"/Textures/Vegetation", internalPath/* +"/Vegetation"*/);
		LoadObjectTextures(externalPath+"/ObjectTextures", internalPath/* +"/Vegetation"*/);
		LoadObjectNormals(externalPath+"/ObjectNormals", internalPath/* +"/Vegetation"*/);
		LoadObjects(externalPath+"/Objects", internalPath/* +"/Vegetation"*/);
	}
	
	void LoadPaint (string getDirectory, string setDirectory)
	{
		if (Directory.Exists (getDirectory)) {
			var info = new DirectoryInfo (getDirectory);
			var fileInfo = info.GetFiles ();
			customPaintImages = new List<Texture2D> ();
			customPaintNormals = new List<Texture2D> ();

			if(!Directory.Exists(setDirectory))
				System.IO.Directory.CreateDirectory(setDirectory);
			
			foreach (FileInfo file in fileInfo) {
				if(File.Exists(setDirectory +"/"+ file.Name))
					File.Delete(setDirectory +"/"+ file.Name);

				file.CopyTo(setDirectory +"/"+ file.Name);
				
#if UNITY_EDITOR			
				AssetDatabase.Refresh ();
#endif

				string fileName = file.Name;
				int fileExtPos = fileName.LastIndexOf(".");
				if (fileExtPos >= 0 )
					fileName= fileName.Substring(0, fileExtPos);

				Texture2D texTmp = Resources.Load<Texture2D>(fileName);
				
				if(file.Name.Contains("_c")){
					customPaintImages.Add (texTmp);
					customPaintNormals.Add (NormalMap(texTmp,5));
				}
			}
		}
	}
	
	void LoadVegetation (string getDirectory, string setDirectory)
	{		
		if (Directory.Exists (getDirectory)) {
			var info = new DirectoryInfo (getDirectory);
			var fileInfo = info.GetFiles ();
			customVegetation = new List<Texture2D> ();
			
			if(!Directory.Exists(setDirectory))
				System.IO.Directory.CreateDirectory(setDirectory);
			
			foreach (FileInfo file in fileInfo) {
				if(File.Exists(setDirectory +"/"+ file.Name))
					File.Delete(setDirectory +"/"+ file.Name);
							
				file.CopyTo(setDirectory +"/"+ file.Name);

#if UNITY_EDITOR	
				AssetDatabase.Refresh ();
#endif				

				string fileName = file.Name;
				int fileExtPos = fileName.LastIndexOf(".");
				if (fileExtPos >= 0 )
					fileName= fileName.Substring(0, fileExtPos);
				
				Texture2D texTmp = Resources.Load<Texture2D>(fileName);

				customVegetation.Add(texTmp);
			}
		}
	}
	
	public void LoadObjects (string getDirectory, string setDirectory)
	{		
		if (Directory.Exists (getDirectory)) {
			var info = new DirectoryInfo (getDirectory);
			var fileInfo = info.GetFiles ();
			customObjects = new List<GameObject> ();
			
			if(!Directory.Exists(setDirectory))
				System.IO.Directory.CreateDirectory(setDirectory);
			
			foreach (FileInfo file in fileInfo) {
				if(File.Exists(setDirectory +"/"+ file.Name))
					File.Delete(setDirectory +"/"+ file.Name);
				
				file.CopyTo(setDirectory +"/"+ file.Name);
				
				#if UNITY_EDITOR	
				AssetDatabase.Refresh ();
				#endif				

				string objName = file.Name.Substring(0, file.Name.Length - 4);
				print("Adding " + objName);
				customObjects.Add(Resources.Load(objName, typeof(GameObject)) as GameObject);
				if(customObjects[customObjects.Count-1])
					customObjects[customObjects.Count-1].name = objName;
			}
		}
	}
	
	public void LoadObjectTextures (string getDirectory, string setDirectory)
	{		
		if (Directory.Exists (getDirectory)) {
			var info = new DirectoryInfo (getDirectory);
			var fileInfo = info.GetFiles ();
			customObjectTextures = new List<Texture2D> ();
			
			if(!Directory.Exists(setDirectory))
				System.IO.Directory.CreateDirectory(setDirectory);
			
			foreach (FileInfo file in fileInfo) {
				if(File.Exists(setDirectory +"/"+ file.Name))
					File.Delete(setDirectory +"/"+ file.Name);
				
				file.CopyTo(setDirectory +"/"+ file.Name);
				
				#if UNITY_EDITOR	
				AssetDatabase.Refresh ();
				#endif				
				
				string fileName = file.Name;
				int fileExtPos = fileName.LastIndexOf(".");
				if (fileExtPos >= 0 )
					fileName= fileName.Substring(0, fileExtPos);
				
				Texture2D texTmp = Resources.Load<Texture2D>(fileName);
				
				customObjectTextures.Add(texTmp);
			}
		}
	}
	
	public void LoadObjectNormals (string getDirectory, string setDirectory)
	{		
		if (Directory.Exists (getDirectory)) {
			var info = new DirectoryInfo (getDirectory);
			var fileInfo = info.GetFiles ();
			customObjectNormals = new List<Texture2D> ();
			
			if(!Directory.Exists(setDirectory))
				System.IO.Directory.CreateDirectory(setDirectory);
			
			foreach (FileInfo file in fileInfo) {
				if(File.Exists(setDirectory +"/"+ file.Name))
					File.Delete(setDirectory +"/"+ file.Name);
				
				file.CopyTo(setDirectory +"/"+ file.Name);
				
				#if UNITY_EDITOR	
				AssetDatabase.Refresh ();
				#endif				
				
				string fileName = file.Name;
				int fileExtPos = fileName.LastIndexOf(".");
				if (fileExtPos >= 0 )
					fileName= fileName.Substring(0, fileExtPos);
				
				Texture2D texTmp = Resources.Load<Texture2D>(fileName);
				
				customObjectNormals.Add(texTmp);
				#if UNITY_EDITOR
				customObjectNormals.Add(texTmp);
				#else
				customObjectNormals.Add (NormalMap(texTmp, 5));
				#endif
			}
		}
	}





	void UpdateCustomTerrainTextures ()
	{
		for(int i = 0; i < loadedPaint.Count; i++){
			if(loadedPaint[i] >= 0 && customPaintImages.Count >= loadedPaint[i])
				// Set the custom Paint
			if(customPaintNormals.Count > i && customPaintNormals[i])
				FindObjectOfType<AGF_TerrainManager>().SetCustomTexture(i, GetCustomPaint(loadedPaint[i]),customPaintNormals[i], loadedPaint[i]);
			else if(normalMap)
				FindObjectOfType<AGF_TerrainManager>().SetCustomTexture(i, GetCustomPaint(loadedPaint[i]), normalMap, loadedPaint[i]);
			else
				FindObjectOfType<AGF_TerrainManager>().SetCustomTexture(i, GetCustomPaint(loadedPaint[i]), null, loadedPaint[i]);
			
		}
	}
	
	void UpdateCustomVegetationTextures (){
		// For each loaded vegetation index
		for(int i = 0; i < loadedVegetation.Count; i++){
			// if we have a texture to match the loaded vegetation index
			if(loadedVegetation[i] >= 0 && customVegetation.Count >= loadedVegetation[i])
				// Set the custom vegetation
				FindObjectOfType<AGF_TerrainManager>().SetCustomVegetationTexture(i, GetCustomVegetation(loadedVegetation[i]), loadedVegetation[i]);
		}
	}

	
	
	
	
	
	public string SavePaintSerializationString(){
		StringBuilder builder = new StringBuilder();
		
		if(loadedPaint.Count > 1){
			do{
				print(loadedPaint.Count);
				loadedPaint.RemoveAt(loadedPaint.Count -1);
			}while (loadedPaint[loadedPaint.Count - 1] == -1 && loadedPaint.Count > 1);
		}
		
		foreach(int index in loadedPaint){
			builder.Append(index + "!");
		}
		print("Paint Builder:" + builder.ToString());
		
		return builder.ToString();
	}
	
	public void LoadPaintSerializationString( string str ){
		print("Paint Loader:" + str);
		string[] splitString = str.Split (new char[]{'!'});
		if (splitString.Length <= 1)
			return;
		
		loadedPaint = new List<int> ();
		
		for(int i = 0; i < splitString.Length-1; i++){
			loadedPaint.Add(int.Parse(splitString[i]));
		}
		UpdateCustomTerrainTextures ();
	}
	
	public void SetUsedCustomPaint(int id, int lastID, int newLength){
		do{
			loadedPaint.Add(-1);
		}while(loadedPaint.Count <= newLength);
		
		loadedPaint [lastID] = id;
	}
	
	
	
	public void SetUsedCustomVegetation(int id, int lastID, int newLength){
		do{
			loadedVegetation.Add(-1);
		}while(loadedVegetation.Count <= newLength);
		
		loadedVegetation [lastID] = id;
	}
	
	public void LoadVegetationSerializationString( string str ){
		print("Vegetation Loader:" + str);
		string[] splitString = str.Split (new char[]{'!'});
		if (splitString.Length <= 1)
			return;
		
		loadedVegetation = new List<int> ();
		
		for(int i = 0; i < splitString.Length-1; i++){
			loadedVegetation.Add(int.Parse(splitString[i]));
		}
		UpdateCustomVegetationTextures ();
	}

	
	
	public void LoadCustomObjectSerializationString( string str ){
		string[] splitString1 = str.Split (new char[]{'$'});
		if (splitString1.Length <= 1)
			return;

		GameObject customObjectParent = new GameObject ();
		customObjectParent.name = "Custom Objects";
		
		for(int i = 0; i < splitString1.Length; i++){
			string[] tileDataString = splitString1[i].Split (new char[]{'!'});
			if (tileDataString.Length > 0){
				int index = 0;
				foreach(GameObject customObject in customObjects){
					if(customObject && tileDataString[index] == customObject.name){
						GameObject newObject = GameObject.Instantiate(customObject) as GameObject;
						
						// grab the position.
						float posX = 0, posY = 0, posZ = 0;
						float.TryParse(tileDataString[++index], out posX);
						float.TryParse(tileDataString[++index], out posY);
						float.TryParse(tileDataString[++index], out posZ);
						Vector3 position = new Vector3(posX, posY, posZ);
						
						// grab the rotation.
						float rotX = 0, rotY = 0, rotZ = 0, rotW = 0;
						float.TryParse(tileDataString[++index], out rotX);
						float.TryParse(tileDataString[++index], out rotY);
						float.TryParse(tileDataString[++index], out rotZ);
						float.TryParse(tileDataString[++index], out rotW);
						Quaternion rotation = new Quaternion(rotX, rotY, rotZ, rotW);
						
						// grab the scale.
						float scaleX = 0, scaleY = 0, scaleZ = 0;
						float.TryParse(tileDataString[++index], out scaleX);
						float.TryParse(tileDataString[++index], out scaleY);
						float.TryParse(tileDataString[++index], out scaleZ);
						Vector3 scale = new Vector3(scaleX, scaleY, scaleZ);
						
						int customTexture = -1;
						if(tileDataString.Length > index + 1)
							int.TryParse(tileDataString[++index], out customTexture);
						
						int customNormal = -1;
						if(tileDataString.Length > index + 1)
							int.TryParse(tileDataString[++index], out customNormal);
						
						float customSpecular = -.5f;
						if(tileDataString.Length > index + 1){
							float.TryParse(tileDataString[++index], out customSpecular);
						}
						
						Color colorMap = Color.gray;
						if(tileDataString.Length > index + 4){
							float.TryParse(tileDataString[++index], out colorMap.r);
							float.TryParse(tileDataString[++index], out colorMap.g);
							float.TryParse(tileDataString[++index], out colorMap.b);
							float.TryParse(tileDataString[++index], out colorMap.a);
							
						}
						Color normalMap = Color.gray;
						if(tileDataString.Length > index + 4){
							float.TryParse(tileDataString[++index], out normalMap.r);
							float.TryParse(tileDataString[++index], out normalMap.g);
							float.TryParse(tileDataString[++index], out normalMap.b);
							float.TryParse(tileDataString[++index], out normalMap.a);
							
						}
						
						if(newObject != null){
							foreach(Transform child in newObject.GetComponentsInChildren<Transform>()){
								if(child != newObject.transform && child.GetComponent<Renderer>())
									child.gameObject.AddComponent<BoxCollider>();
							}

							newObject.AddComponent<TileProperties>();
							newObject.GetComponent<TileProperties>().StoreScale( scale );
							newObject.GetComponent<TileProperties>().SetCustomTexture(customTexture, customNormal, customSpecular, colorMap, normalMap);
							
							newObject.transform.position = position;
							newObject.transform.rotation = rotation;
							newObject.transform.localScale = scale;

							newObject.transform.parent = customObjectParent.transform;
						}
						else
							print ("Failed to create: " + tileDataString[0]);
					}
				}
				
				
			}
			
		}
	}

	
	public List<Texture2D> GetCustomVegetations(){
		return customVegetation;	
	}
	
	public Texture2D GetCustomVegetation( int id ){
		if(id < customVegetation.Count)
			return customVegetation[id];
		else
			return null;
	}
	
	public Material GetTileTexture(int textureIndex, int normalIndex){
		Material newMaterial = new Material(Shader.Find("Bumped Specular"));
		if(textureIndex >= 0)
			newMaterial.SetTexture("_MainTex", customObjectTextures[textureIndex]);
		if (normalIndex >= 0)
			newMaterial.SetTexture("_BumpMap", customObjectNormals[normalIndex]);
		return newMaterial;
	}
	
	public Texture2D NormalMap(Texture2D source,float strength) {
		strength=Mathf.Clamp(strength,0.0F,10.0F);
		Texture2D result;
		float xLeft;
		float xRight;
		float yUp;
		float yDown;
		float yDelta;
		float xDelta;
		result = new Texture2D (source.width, source.height, TextureFormat.ARGB32, true);
		for (int by=0; by<result.height; by++) {
			for (int bx=0; bx<result.width; bx++) {
				xLeft = source.GetPixel(bx-1,by).grayscale*strength;
				xRight = source.GetPixel(bx+1,by).grayscale*strength;
				yUp = source.GetPixel(bx,by-1).grayscale*strength;
				yDown = source.GetPixel(bx,by+1).grayscale*strength;
				xDelta = ((xLeft-xRight)+1)*0.5f;
				yDelta = ((yUp-yDown)+1)*0.5f;
				result.SetPixel(bx,by,new Color(xDelta,yDelta,1.0f,yDelta));
			}
		}
		result.Apply();
		return result;
	}		

	public Texture2D GetCustomPaint( int id ){
		return customPaintImages[id];	
	}
	
	public List<Texture2D> GetCustomPaintNormals(){
		return customPaintNormals;	
	}


	public List<Texture2D> GetCustomObjectNormals(){
		return customObjectNormals;	
	}
	
	public Texture2D GetCustomObjectNormal( int id ){
		return customObjectNormals [id];
	}
	
	public List<Texture2D> GetCustomObjectTextures(){
		return customObjectTextures;	
	}
	
	public Texture2D GetCustomObjectTexture( int id ){
		return customObjectTextures [id];
	}

}
