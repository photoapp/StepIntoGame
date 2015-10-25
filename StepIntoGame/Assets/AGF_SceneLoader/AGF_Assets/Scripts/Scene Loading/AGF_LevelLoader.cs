using UnityEngine;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

public class AGF_LevelLoader : MonoBehaviour {
	
	public static string themeVersionNumber = "0.7.9a";
	public static string versionNumber = "0.7.9a";
//	private bool useEncryption = false;
	private bool? m_SceneLoaded = false;
	public static string LatestErrorMessage = "";
	
	public enum LoadSceneMode{
		CameraOnly, All,	
	}
	private LoadSceneMode m_LoadSceneMode = LoadSceneMode.All;
	
	private Dictionary<string, bool> currentActiveModels = new Dictionary<string, bool>();
	private Dictionary<string, AssetBundle> currentActiveBundles = new Dictionary<string, AssetBundle>();
	
	private List<string> m_RequiredBundlesForLoadScene = new List<string>();
	private List<string> m_RequiredBundlesForLoadModel = new List<string>();
	private string m_RemainingSceneLoadString;
	
	private string m_RemainingModelLoadString;
	private string m_RemainingModelName;
	
	// Editor Asset bundle loading
	private Queue<KeyValuePair<WWW, string>> m_AssetBundleQueue;
	
	// Scene References
	private AGF_GridManager m_GridManager;
	
	private string m_CustomModelLoadString = "";

	private void Start(){
		m_GridManager = GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>();
	}
	
	public void EditorInit(){
		// Called while in the editor to initialize this object.
		Start();
		m_AssetBundleQueue = new Queue<KeyValuePair<WWW, string>>();
	}
	
	private void Update(){
		// if we are currently loading a scene, wait until all necessary asset bundles have been loaded, then trigger the second part of the load.
		if ( m_RequiredBundlesForLoadScene.Count > 0 ){
			bool allBundlesLoaded = true;
			for ( int i = 0; i < m_RequiredBundlesForLoadScene.Count; i++ ){	
				if ( currentActiveBundles.ContainsKey( m_RequiredBundlesForLoadScene[i] ) == false ){
//					print ("bundle " + m_RequiredBundlesForLoadScene[i] + " not loaded.");	
					allBundlesLoaded = false;
				}
			}
			
			if ( allBundlesLoaded == true ){
				m_RequiredBundlesForLoadScene = new List<string>();
				int returnCode;
				if ( m_LoadSceneMode == LoadSceneMode.All ){
					returnCode = FinishLoadScene();
					if ( returnCode != AGF_ReturnCode.Success ){
						// the scene load has failed. clean up the scene.
						CleanUpSceneLoadFailure();
					}
				} else if ( m_LoadSceneMode == LoadSceneMode.CameraOnly ){
					returnCode = FinishLoadCameraFromScene();
					if ( returnCode != AGF_ReturnCode.Success ){
						// the scene load has failed. clean up the scene.
						CleanUpSceneLoadFailure();
					}
				}
				
			}
		}
		
		if ( m_RequiredBundlesForLoadModel.Count > 0 ){
			bool allBundlesLoaded = true;
			for ( int i = 0; i < m_RequiredBundlesForLoadModel.Count; i++ ){	
				if ( currentActiveBundles.ContainsKey( m_RequiredBundlesForLoadModel[i] ) == false ){
					allBundlesLoaded = false;
//					print ("bundle " + m_RequiredBundlesForLoadModel[i] + " not loaded.");	
				}
			}
			
			if ( allBundlesLoaded == true ){
				m_RequiredBundlesForLoadModel = new List<string>();
				int returnCode = FinishLoadModel();
				if ( returnCode != AGF_ReturnCode.Success ){
					// the scene load has failed. clean up the scene.
					CleanUpSceneLoadFailure();
				}
			}
		}
	}
	
	public void EditorUpdate(){
		// Called while in the editor to check for asset bundle loads.
		Update();
		
		if ( m_AssetBundleQueue.Count > 0 ){
			if ( m_AssetBundleQueue.Peek().Key.assetBundle != null ){	
				KeyValuePair<WWW,string> pair = m_AssetBundleQueue.Dequeue();
				ApplyLoadedBundle( pair.Key.assetBundle, pair.Value );
			}
			
		}
	}
	
	// ------------------------------------- //
	// 			Scene Loading 				 //
	// ------------------------------------- //
	
	private string m_CurrentProjectDirectory = "";
	public void SetCurrentProjectDirectory( string filepath ){
		m_CurrentProjectDirectory = filepath;
	}
	
	public string GetCurrentProjectDirectory(){
		return m_CurrentProjectDirectory;
	}
	
	private string m_CurrentActiveSceneName = "";
	public void SetCurrentActiveSceneName( string sceneName ){
		m_CurrentActiveSceneName = sceneName;
	}
	
	public string GetCurrentActiveSceneName(){
		return m_CurrentActiveSceneName;
	}
	
	public int LoadScene( string filename, LoadSceneMode mode = LoadSceneMode.All, string overrideUserDataHomeFolder = ""  ){
		int returnCode = AGF_ReturnCode.Success;
		m_LoadSceneMode = mode;
		
		m_SceneLoaded = false;
		
		string[] split = filename.Split ( new char[]{'/'} );
//		string projectName = split[split.Length-3];
		
		string projectFolder = "";
		for ( int i = 0; i < split.Length-2; i++ ){
			projectFolder += split[i] + "/";
		}
		projectFolder = Main.TrimEndFromString( projectFolder, "/" );
		print ( "Now loading: " + projectFolder );
		
		// set the project directory to this folder.
		SetCurrentProjectDirectory( projectFolder );
		SetCurrentActiveSceneName( Main.TrimEndFromString(split[split.Length-1], ".agfs") );
		
		// read the filestring, and decrypt it.
		string fileString = "";
		string encryptedFileString = File.ReadAllText(filename);
//		if ( useEncryption ){
//			fileString = RijndaelSimple.Decrypt(encryptedFileString);
//		} else {
			fileString = encryptedFileString;
//		}
		
		int index = 0;
		string[] fileStrings = fileString.Split ( new char[]{':'} );
		
		print ( string.Format ("Scene Version number = {0}", fileStrings[0]) );
		if ( fileStrings[0].CompareTo(versionNumber) != 0 ){
			// the version number is different. print an error message, for now.
//			EventManager.InformLoadFailure();
//			m_SceneLoaded = null;
			LatestErrorMessage = "Error: Attempting to load a scene from an earlier version of the tool. Abort.";
			print ( LatestErrorMessage );
//			return AGF_ReturnCode.VersionMismatch;
		}
		
		// test to see if a prefabs folder exists. 
		if ( System.IO.Directory.Exists( m_CurrentProjectDirectory + "/Prefabs" ) == false ){
			if ( System.IO.Directory.Exists ( m_CurrentProjectDirectory + "/Models" ) == true ){
				// !BC
				System.IO.Directory.Move ( m_CurrentProjectDirectory + "/Models", m_CurrentProjectDirectory + "/Prefabs" );
			} else {
//				m_SceneLoaded = null;
				LatestErrorMessage = "Error: Prefab folder does not exist within " + m_CurrentProjectDirectory ;
				print ( LatestErrorMessage );
//				return AGF_ReturnCode.MissingModelFolder;
			}	
		}
		
		// first, save the grid dimensions.
		float dimension;
		bool result = float.TryParse(fileStrings[++index], out dimension);
		
		if ( result ){
			m_GridManager.SetCurrentDimensions(new Vector3(dimension, m_GridManager.GetCurrentDimensions().y, dimension));
		}
		
		// second, set the atmosphere info.
		AGF_AtmosphereManager atmosphereManager = GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>();
		atmosphereManager.SetFogFromString( fileStrings[++index] );
		atmosphereManager.SetLightFromString( fileStrings[++index] );
		
		// clear out all bundles.
		UnloadAllUserBundles();
		
		string userDataHomeFolder = (overrideUserDataHomeFolder != "") ? overrideUserDataHomeFolder : Main.GetUserDataHomeFolder();
		
		// attempt to load all asset bundles that the scene relies on.
		print ("Required Asset Bundles:");
		string[] requiredBundles = fileStrings[++index].Split ( new char[]{'*'} );
		m_RequiredBundlesForLoadScene = new List<string>();
		bool allBundlesLoaded = true;
		if ( requiredBundles[0] != "" ){
			for ( int i = 0; i < requiredBundles.Length; i++ ){
				print (requiredBundles[i]);
				
				if ( File.Exists( userDataHomeFolder + "/Asset Packs/" + requiredBundles[i] ) == false ){
//					m_RequiredBundlesForLoadScene = new string[0];
//					m_SceneLoaded = null;
					LatestErrorMessage = "Asset Bundle missing: " + userDataHomeFolder + "/Asset Packs/" + requiredBundles[i];
					print ( LatestErrorMessage );
//					return AGF_ReturnCode.AssetBundleDoesNotExist;	
				}
				else{
					m_RequiredBundlesForLoadScene.Add(requiredBundles[i]);
					bool bundleNotYetLoaded = LoadAssetBundle( userDataHomeFolder + "/Asset Packs/" + requiredBundles[i] );	
					if ( bundleNotYetLoaded ) allBundlesLoaded = false;
				}

			}
		}
		
		// we will have to wait until this load is complete before moving forward. store the remaining filestring into a temp variable.
		m_RemainingSceneLoadString = "";
		for ( int i = ++index; i < fileStrings.Length; i++ ){
			m_RemainingSceneLoadString += fileStrings[i] + ":"; 
		}
		m_RemainingSceneLoadString.TrimEnd(':');
			
		if ( allBundlesLoaded ){
			// we can immediately proceed if no bundles need loading.
			m_RequiredBundlesForLoadScene = new List<string>();
			returnCode = FinishLoadScene();
		}
		
		return returnCode;
	}
	
	private int FinishLoadScene(){
		int returnCode = AGF_ReturnCode.Success;
		
		int index = 0;
		string[] fileStrings = m_RemainingSceneLoadString.Split ( new char[]{':'} );
		
		// then, attempt to load all models that the scene relies on.
		string[] requiredModels = fileStrings[index].Split( new char[]{'*'} );
		
		Dictionary<string,bool> missingModels = new Dictionary<string, bool>();
		for (int i = 0; i < requiredModels.Length; i++){
			if (requiredModels[i].Length == 0 ){
				continue;
			}
			// test to see if the files exist. add any files that do not to a list.
			if ( File.Exists( m_CurrentProjectDirectory + "/Prefabs/" + requiredModels[i] ) == false ){
				print (string.Format ("Adding model to missing model list: {0}", requiredModels[i]));
				missingModels.Add ( requiredModels[i], true );	
			}
		}
		
		if ( missingModels.Count > 0 ) {
//			m_SceneLoaded = null;
			LatestErrorMessage = "Some prefabs were missing from \"" + m_CurrentProjectDirectory + "/Prefabs\":";
			foreach( KeyValuePair<string,bool> modelName in missingModels ){
				LatestErrorMessage += "\n" + modelName.Key;
			}
			print ( LatestErrorMessage );
//			return AGF_ReturnCode.MissingModels;
		}
		
		// At this point, we know we are successful, so wipe the previous scene entirely.
		
		// delete all current tiles.
		m_GridManager.DeleteAll();
		
		// clear the load list.
		ClearLoadList();
		
		// create a new recorded action list.
		List<AGF_TileDataStruct> recordedActions = new List<AGF_TileDataStruct>();
		
		// we now know that all the necessary models exist, so load them.
		for (int i = 0; i < requiredModels.Length; i++){
			if (requiredModels[i].Length == 0 ){
				continue;
			}
			
			// do not load models that were missing.
			if ( missingModels.ContainsKey(requiredModels[i]) ){
				
			} else {
				returnCode = LoadModel( m_CurrentProjectDirectory + "/Prefabs/" + requiredModels[i] );
				if ( returnCode != AGF_ReturnCode.Success ){
					print("Scene not loaded");
					m_SceneLoaded = null;
					return returnCode;	
				}
			}
		}

		// parse the filestring, and add its data to the new recorded actions list.		
		string[] structStrings = fileStrings[++index].Split( new char[]{'*'} );
		
		for (int i = 0; i < structStrings.Length; i++){
			if ( structStrings[i].Length > 2 ){
				AGF_TileDataStruct tileData = new AGF_TileDataStruct( structStrings[i] );
				recordedActions.Add (tileData);
			}
		}
		
		// before we load the scene, make sure that all objects within the asset packs are valid.
		AGF_TileListManager tileListManager = GameObject.Find ("AGF_TileListManager").GetComponent<AGF_TileListManager>();
		List<string> missingAssets = new List<string>();
		foreach ( AGF_TileDataStruct tileData in recordedActions ){
			if ( tileListManager.IsTileLoaded( tileData.tileID ) == false ){
				missingAssets.Add ( tileData.tileID );
			}
		}
		
		if ( missingAssets.Count > 0 ){
			LatestErrorMessage = "Some asset packs have changed since saving the \"" + GetCurrentActiveSceneName() + "\" scene. Here are some assets that were missing: ";
			for ( int i = 0; i < missingAssets.Count && i < 5; i++ ){
				LatestErrorMessage += "\n" + missingAssets[i];
			}
			print ( LatestErrorMessage );
//			m_SceneLoaded = null;
//			return AGF_ReturnCode.AssetBundleDataMismatch;	
		}
		
		// if the scene is valid, instantiate everything.
		
		GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>().SetSkyboxFromString( fileStrings[++index] );
		GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>().SetTerrainFromString( fileStrings[++index] );
		GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>().SetWaterFromString( fileStrings[++index] );
		index++; // ingore the grid settings.
		index++; // ignore the GUIManager settings.
		
		if(fileStrings.Length > index+1)
			FindObjectOfType<AGF_AssetLoader> ().LoadPaintSerializationString(fileStrings [++index]);
		if(fileStrings.Length > index+1)
			FindObjectOfType<AGF_AssetLoader> ().LoadVegetationSerializationString(fileStrings [++index]);
		if(fileStrings.Length > index+1)
			index++;
		if(fileStrings.Length > index+1)
			FindObjectOfType<AGF_AssetLoader> ().LoadCustomObjectSerializationString(fileStrings [++index]);



		// Copy the list over.
		m_GridManager.SetRecordedActions( recordedActions );
		
		// Perform all operations in the list.
		while ( m_GridManager.PerformPlaybackStep() ){
			// do nothing.
		}

		foreach(Light lightobject in FindObjectsOfType<Light>()){
        if(lightobject.GetComponent<Renderer>())
				lightobject.GetComponent<Renderer>().enabled=false;
		}

	 // Inform of load success.
		EventHandler.instance.dispatchEvent( new AGFEventObj( AGFEventObj.SceneLoaded ) );
		m_SceneLoaded = true;
		
		return returnCode;
	}
	
	private int FinishLoadCameraFromScene(){
		int returnCode = AGF_ReturnCode.Success;
		print ("FinishLoadCameraFromScene");
		
		// start the index at the skybox string.
		int index = 2;
		string[] fileStrings = m_RemainingSceneLoadString.Split ( new char[]{':'} );
		
		GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>().SetSkyboxFromString( fileStrings[index] );
		
		// Inform of load success.
		EventHandler.instance.dispatchEvent( new AGFEventObj( AGFEventObj.SceneLoaded ) );
		m_SceneLoaded = true;
		
		return returnCode;
	}
	
	public bool? IsSceneLoaded(){
		return m_SceneLoaded;	
	}
	
	public void UnloadScene(){
		AGF_GridManager gridManager = GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>();
			
		// delete all current tiles and recorded actions.
		gridManager.DeleteAll();
	}
	
	private void CleanUpSceneLoadFailure(){
		UnloadAllUserBundles();
		ClearLoadList();
		m_GridManager.DeleteAll();
	}
	
	// --------------------------------- //
	// 			Model Loading 			 //
	// --------------------------------- //
	public int LoadModel( string filename ){
		int returnCode = AGF_ReturnCode.Success;
		
		string[] split = filename.Split ( new char[]{'/'} );
		string modelName = split[split.Length-1];
		
		m_RemainingModelName = modelName;
		
		//		bool alreadyLoaded = false;
		
		print ("Loading model: " + modelName);
		
		// add this model to the dictionary.
		// if the model has already been loaded, we need to handle it specially.
		if ( currentActiveModels.ContainsKey(modelName) ){
			//			print (string.Format("Model is already loaded! {0}", modelName));
			//			alreadyLoaded = true;
		} else {
			//			print(string.Format ("Model has not been loaded yet! {0}", modelName));
			currentActiveModels.Add (modelName, true);
		}
		
		// read the filestring, and decrypt it.
		string fileString = "";
		string encryptedFileString = File.ReadAllText(filename);
		//		if ( useEncryption ){
		//			fileString = RijndaelSimple.Decrypt(encryptedFileString);
		//		} else {
		fileString = encryptedFileString;
		//		}
		string[] fileStrings = fileString.Split ( new char[]{':'} );
		
		int fsIndex = 0;
		
		print (string.Format("Model Version Number = {0}", fileStrings[fsIndex]));
		if ( fileStrings[fsIndex].CompareTo(versionNumber) != 0 ){
			// the version number is different. print an error message, for now.
			LatestErrorMessage = "Error: Attempting to load a model from an earlier version of the tool. Abort.";
			print (LatestErrorMessage);
			//			return AGF_ReturnCode.VersionMismatch;
		}
		
		// first, grab the grid size, but ignore it.
		float dimension;
		//		bool result = 
		float.TryParse(fileStrings[++fsIndex], out dimension);
		
		// attempt to load all asset bundles that the model relies on.
		string[] requiredBundles = fileStrings[++fsIndex].Split ( new char[]{'*'} );
		m_RequiredBundlesForLoadModel = new List<string>();
		bool allBundlesLoaded = true;
		if ( requiredBundles[0] != "" ){
			for ( int i = 0; i < requiredBundles.Length; i++ ){
				if ( requiredBundles[i] != "" ){
					if ( File.Exists( Main.GetUserDataHomeFolder() + "/Asset Packs/" + requiredBundles[i] ) == false ){
						LatestErrorMessage = "Asset Bundle missing: " + Main.GetUserDataHomeFolder() + "/Asset Packs/" + requiredBundles[i];
						//						m_RequiredBundlesForLoadModel = new string[0];
						print ( LatestErrorMessage );
						//						return AGF_ReturnCode.AssetBundleDoesNotExist;	
					}
					else{					
						m_RequiredBundlesForLoadModel.Add(requiredBundles[i]);
						bool bundleNotYetLoaded = LoadAssetBundle( Main.GetUserDataHomeFolder() + "/Asset Packs/" + requiredBundles[i] );	
						if ( bundleNotYetLoaded ) allBundlesLoaded = false;
					}
				}
			}
		}
		
		// we will have to wait until this load is complete before moving forward. store the remaining filestring into a temp variable.
		m_RemainingModelLoadString = "";
		for ( int i = ++fsIndex; i < fileStrings.Length; i++ ){
			m_RemainingModelLoadString += fileStrings[i] + ":"; 
		}
		m_RemainingModelLoadString.TrimEnd(':');
		
		if ( allBundlesLoaded ){
			// we can immediately proceed if no bundles need loading.
			m_RequiredBundlesForLoadModel = new List<string>();
			return FinishLoadModel();	
		}
		
		return returnCode;
	}
	
	private int FinishLoadModel(){
		GameObject sceneObjectParent = new GameObject ();
		sceneObjectParent.name = "Scene Objects";

		AGF_TileListManager tileListManager = GameObject.Find ("AGF_TileListManager").GetComponent<AGF_TileListManager>();
		
		int fsIndex = 0;
		string[] fileStrings = m_RemainingModelLoadString.Split ( new char[]{':'} );
		
		string[] tileStrings = fileStrings[fsIndex].Split( new char[]{'*'} );
		
		// before we load the model, make sure that all objects within the asset packs are valid.
		List<string> missingAssets = new List<string>();
		for (int i = 0; i < tileStrings.Length; i++){
			string[] tileDataString = tileStrings[i].Split( new char[]{'~'} );
			
			if ( tileListManager.IsTileLoaded( tileDataString[0] ) == false ){
				missingAssets.Add ( tileDataString[0] );
			}
		}
		
		if ( missingAssets.Count > 0 ){
			LatestErrorMessage = "Error: Some asset packs have changed since saving the \"" + m_RemainingModelName + "\" prefab. Here are some assets that were missing:";
			for ( int i = 0; i < missingAssets.Count && i < 5; i++ ){
				LatestErrorMessage += "\n" + missingAssets[i];
			}
			print ( LatestErrorMessage );
			//			m_SceneLoaded = null;
			currentActiveModels.Remove ( m_RemainingModelName );
			//			return AGF_ReturnCode.AssetBundleDataMismatch;	
		}
		
		// create a new gameobject.
		GameObject model = new GameObject();
		model.name = m_RemainingModelName;
		model.transform.parent = sceneObjectParent.transform;
		
		// parse the filestring, and add it's data to the newly created transform as children.
		for (int i = 0; i < tileStrings.Length; i++){
			if ( tileStrings[i].Length > 2 ){
				string[] tileDataString = tileStrings[i].Split( new char[]{'~'} );
				int index = 0;
				
				// grab the tileID.
				string tileID = tileDataString[index];
				//				print ( tileID );
				
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
				
				// !BC!
				//				string customString = tileDataString[++index];
				
				// create the new child.
				if(tileListManager.GetTile(tileID) != null){
					GameObject child = (GameObject)Instantiate( tileListManager.GetTile (tileID).gameObject );
					child.transform.position = position;
					child.transform.rotation = rotation;
					child.transform.parent = model.transform;
					
					child.GetComponent<TileProperties>().StoreScale( scale );
					
					// disable the new child, and all of it's children.
					//				child.SetActive( false );
				}
			}
		}
		model.AddComponent<TileProperties>();
		model.GetComponent<TileProperties>().category = "_Prefabs";
		model.GetComponent<TileProperties>().tileID = "_Prefabs/" + model.name + "/_Prefabs";
		
		// add the size and category here as well.
		if ( model.GetComponent<Renderer>() ){
			Vector3 size = model.GetComponent<Renderer>().bounds.extents * 2;
			model.gameObject.GetComponent<TileProperties>().size = size;
			model.gameObject.GetComponent<TileProperties>().tileBounds = model.GetComponent<Renderer>().bounds;
		} else {
			// if there is no renderer, that means this object has several children. determine the size of the object
			// based on the size of the children.
			Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
			Main.GetBoundsRecursively(model.transform, ref bounds);
			
			Vector3 size = bounds.extents * 2;
			model.gameObject.GetComponent<TileProperties>().size = size;
			model.gameObject.GetComponent<TileProperties>().tileBounds = bounds;
		}
		
		// add the new loaded model to the tile list. (if it already exists, it will be replaced automatically)
		tileListManager.AddToList( model.GetComponent<TileProperties>().tileID, model.transform );
		
		// turn off the model so that it doesn't exist in the world while also in the list.
		model.SetActive( false );
		
		// Inform of load success.
		EventHandler.instance.dispatchEvent( new AGFEventObj( AGFEventObj.ModelLoaded ) );
		
		return AGF_ReturnCode.Success;
	}
	
	public void ClearLoadList(){
		AGF_TileListManager tileListManager = GameObject.Find ("AGF_TileListManager").GetComponent<AGF_TileListManager>();
		
		foreach ( KeyValuePair<string,bool> pair in currentActiveModels ){
			Transform t = tileListManager.GetTile( "_Prefabs", pair.Key, "_Prefabs" );
			if(t != null)
				Main.SmartDestroy( t.gameObject );
			else
				print("t is null");
		}
		currentActiveModels.Clear();
	}
	
	// ------------------------------------- //
	// 			Asset Bundles 				 //
	// ------------------------------------- //
	
	public bool LoadAssetBundle( string selectedFile ){
		string bundleURL = "file:///" + selectedFile;
		string[] split = bundleURL.Split( new char[]{'/'} );
		string bundleName = split[split.Length-1];
		
		if ( currentActiveBundles.ContainsKey( bundleName ) ){
			return false;	
		}
		
		// coroutines do not work properly in the editor.
		if ( Application.isPlaying ){
			StartCoroutine( DownloadAssetBundle( bundleURL, bundleName ) );
		} else {
			QueueDownloadAssetBundle( bundleURL, bundleName );
		}
		
		return true;
	}
	
	public Object LoadAssetFromBundle( string bundleName, string assetName ){
		return currentActiveBundles[bundleName].LoadAsset ( assetName );
	}
	
	public void UnloadBundle( string bundleName, bool removeFromList = true ){
		// remove all assets belonging to the target bundle from the tile list.
		// clear out the bundle from the active bundles list.
		if ( bundleName.EndsWith( "_w.unity3d" ) || bundleName.EndsWith( "_wl.unity3d" ) ) {
			GameObject.Find ("AGF_TileListManager").GetComponent<AGF_TileListManager>().RemoveBundle( bundleName );
		}
		if ( bundleName.EndsWith( "_t.unity3d" ) || bundleName.EndsWith( "_tl.unity3d" ) ) {
			GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>().UnloadTexturesFromBundle( bundleName );
		}
		if ( bundleName.EndsWith( "_v.unity3d" ) || bundleName.EndsWith( "_vl.unity3d" ) ){
			GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>().UnloadVegetationTexturesFromBundle( bundleName );	
		}
		if ( bundleName.EndsWith( "_s.unity3d" ) || bundleName.EndsWith( "_sl.unity3d" ) ) {
			GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>().UnloadMaterialsFromBundle( bundleName );	
		}
		
		if ( currentActiveBundles.ContainsKey( bundleName ) ){
			currentActiveBundles[bundleName].Unload(false);
		}
		
		if ( removeFromList ){
			currentActiveBundles.Remove( bundleName );
		} else {
			Resources.UnloadUnusedAssets();
		}
		
	}
	
	public void UnloadAllUserBundles(){
		foreach( KeyValuePair<string,AssetBundle> bundle in currentActiveBundles ){
			UnloadBundle( bundle.Key, false );
		}
		
		currentActiveBundles.Clear();
		Resources.UnloadUnusedAssets();
	}
	
	private void ApplyLoadedBundle( AssetBundle bundle, string bundleName ){
		// disambiguation. 	
		print ("Apply Loaded Bundle: " + bundleName);
		if ( bundleName.EndsWith( "_w.unity3d" ) || bundleName.EndsWith( "_wl.unity3d" ) ){
			ApplyLoadedWarehouseBundle( bundle, bundleName );
		}
		if ( bundleName.EndsWith( "_t.unity3d" ) || bundleName.EndsWith( "_tl.unity3d" ) ){
			ApplyLoadedTerrainBundle( bundle, bundleName );	
		}
		if ( bundleName.EndsWith( "_v.unity3d" ) || bundleName.EndsWith( "_vl.unity3d" ) ){
			ApplyLoadedVegetationBundle( bundle, bundleName );	
		}
		if ( bundleName.EndsWith( "_s.unity3d" ) || bundleName.EndsWith( "_sl.unity3d" ) ){
			ApplyLoadedSkyboxBundle( bundle, bundleName );	
		}
		
//		bundle.Unload(false); // all objects have been copied out of the bundle, so unload the bundle itself.
		currentActiveBundles.Add ( bundleName, bundle );
		Resources.UnloadUnusedAssets();
		EventHandler.instance.dispatchEvent( new AGFEventObj( AGFEventObj.AssetBundleLoaded ) );
	}
	
	private void ApplyLoadedWarehouseBundle( AssetBundle bundle, string bundleName ){
		// grab every element of the bundle, and add it to the tileList.
		AGF_TileListManager tileListManager = GameObject.Find ("AGF_TileListManager").GetComponent<AGF_TileListManager>();
		AGF_GibManager gibManager = GameObject.Find ("AGF_GibManager").GetComponent<AGF_GibManager>();
		Object[] objects = bundle.LoadAllAssets(typeof(GameObject));
		for ( int i = 0; i < objects.Length; i++ ){ 
			GameObject obj = (GameObject)objects[i];
			
			if ( obj.GetComponent<GibProperties>() != null ){
				gibManager.AddGibTolist( obj.GetComponent<GibProperties>().category, obj.GetComponent<GibProperties>().bundle, obj.transform );
				
			} else if ( obj.GetComponent<GibSettings>() != null ){
				gibManager.AddGibSettingsToList( obj.GetComponent<GibSettings>().category, obj.GetComponent<GibSettings>().bundle, obj.transform );
			
			} else if ( obj.GetComponent<TileProperties>() != null ){
				obj.GetComponent<TileProperties>().tileID += "/" + bundleName;
				tileListManager.AddToList( obj.GetComponent<TileProperties>().tileID, obj.transform );
			}
		}
	}
	
	private void ApplyLoadedTerrainBundle( AssetBundle bundle, string bundleName ){
		Object[] objects = bundle.LoadAllAssets (typeof(Texture2D));
		for ( int i = 0; i < objects.Length; i++ ){
			Texture2D tex = (Texture2D)objects[i];	
			GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>().AddTextureToList( tex, bundleName );
		}
	}
	
	private void ApplyLoadedVegetationBundle( AssetBundle bundle, string bundleName ){
		Object[] objects = bundle.LoadAllAssets (typeof(Texture2D));
		for ( int i = 0; i < objects.Length; i++ ){
			Texture2D tex = (Texture2D)objects[i];	
			GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>().AddVegetationTextureToList( tex, bundleName );
		}
	}	
	
	private void ApplyLoadedSkyboxBundle( AssetBundle bundle, string bundleName ){
		Object[] objects = bundle.LoadAllAssets (typeof(Texture2D));
		for ( int i = 0; i < objects.Length; i++ ){
			Texture2D tex = (Texture2D)objects[i];
			GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>().AddTextureToList( tex, bundleName );
		}
	}
	
	private IEnumerator DownloadAndCacheAssetBundle( string url, string bundleName, int version ){
		// Wait for the Caching system to be ready
		while (!Caching.ready)
			yield return null;

		// Load the AssetBundle file from Cache if it exists with the same version or download and store it in the cache
		using (WWW www = WWW.LoadFromCacheOrDownload (url, 1) ) { //, version)){
			yield return www;
			if (www.error != null)
				throw new System.Exception("WWW download had an error:" + www.error);
			ApplyLoadedBundle( www.assetBundle, bundleName );
		}
	}
	
	private IEnumerator DownloadAssetBundle( string url, string bundleName ){
		// Download the file from the URL. It will not be saved in the Cache
	   	using (WWW www = new WWW(url)) {
		   yield return www;
		   if (www.error != null){
			   throw new System.Exception("WWW download had an error:" + www.error);
			} else {
				// check to see if the bundle is encrypted.
				if ( bundleName.EndsWith("l.unity3d") ){
					// this bundle is encrypted!
					print ("Encrypted bundle detected!");
					
					if ( GameObject.Find ("AGF_Decryptor") != null ){
						if ( GameObject.Find ("AGF_Decryptor").GetComponent<AGF_Decryptor>().CanDecrypt() ){
							byte[] decryptedBytes = GameObject.Find ("AGF_Decryptor").GetComponent<AGF_Decryptor>().Decrypt( www.bytes );
							AssetBundleCreateRequest acr = AssetBundle.CreateFromMemory( decryptedBytes );
							yield return acr;
							
							ApplyLoadedBundle( acr.assetBundle, bundleName );
						} else {
							// we can't decrypt this.
						}
					} else {
						// we can't decrypt this.
					}
					
				} else {
					ApplyLoadedBundle( www.assetBundle, bundleName );
				}
			}
	   }	
	}
	
	private void QueueDownloadAssetBundle( string url, string bundleName ){
		// check to see if the bundle is encrypted.
		if ( bundleName.EndsWith("l.unity3d") ){
			// this bundle is encrypted! do not add it to the queue.
			print ("Encrypted bundle detected!");
			
			// also remove it from the required bundle lists.
			bool sceneBundleFound = false;
			for ( int i = 0; i < m_RequiredBundlesForLoadScene.Count; i++ ){
				if ( m_RequiredBundlesForLoadScene[i] == bundleName ){
					sceneBundleFound = true;
				}
			}

			// remove the locked bundle from the list
			if ( sceneBundleFound ){
				List<string> newRequiredSceneBundles = new List<string>();
				
				for ( int i = 0; i < m_RequiredBundlesForLoadScene.Count; i++ ){
					if ( m_RequiredBundlesForLoadScene[i] != bundleName ){
						newRequiredSceneBundles.Add(m_RequiredBundlesForLoadScene[i]);	
					}
				}
				m_RequiredBundlesForLoadScene = newRequiredSceneBundles;
			}
			
			bool modelBundleFound = false;
			for ( int i = 0; i < m_RequiredBundlesForLoadModel.Count; i++ ){
				if ( m_RequiredBundlesForLoadModel[i] == bundleName ){	
					modelBundleFound = true;
				}
			}
			
			if ( modelBundleFound ){
				List<string> newRequiredModelBundles = new List<string>();
				int count = 0;
				
				for ( int i = 0; i < m_RequiredBundlesForLoadModel.Count; i++ ){
					if ( m_RequiredBundlesForLoadModel[i] != bundleName ){
						newRequiredModelBundles.Add(m_RequiredBundlesForLoadModel[i]);	
						count++;
					}
				}
				m_RequiredBundlesForLoadModel = newRequiredModelBundles;
			}
			
		} else {
			m_AssetBundleQueue.Enqueue( new KeyValuePair<WWW,string>( new WWW(url), bundleName ) );
		}
	}
}
