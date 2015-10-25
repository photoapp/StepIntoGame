using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AGF_TileListManager : MonoBehaviour {
	
//	[System.Serializable]
//	public class TileList{
//		public Transform[] tileList;
//		public string categoryName;
//		public int startIdx;
//		public int endIdx;
//	}
	
//	public Transform lightObject;
	public Transform spawnerObject;
	public Transform startObject;
	
	public enum SortMode{
		Category, Bundle,	
	}
	private SortMode m_CurrentSortMode = SortMode.Category;
	
	// tile list sorted by category, and related ordering lists.
	private List<string> m_CategoryOrder;
	private Dictionary<string, List<string>> m_TileOrder;
	private Dictionary<string, Dictionary<string, Dictionary<string, Transform>>> m_TileList; 
	
	// tile list sorted by bundle, and related ordering lists.
	private List<string> m_BundleOrder;
	private Dictionary<string, List<string>> m_TileBundleOrder;
	private Dictionary<string, Dictionary<string, Dictionary<string, Transform>>> m_BundleList;
	
	private void Start(){
		// create the tile list, and ordering structures.
		m_CategoryOrder = new List<string>();
		m_TileOrder = new Dictionary<string, List<string>>();
		m_TileList = new Dictionary<string, Dictionary<string, Dictionary<string, Transform>>>();
		
		m_BundleOrder = new List<string>();
		m_TileBundleOrder = new Dictionary<string, List<string>>();
		m_BundleList = new Dictionary<string, Dictionary<string, Dictionary<string, Transform>>>();
		
		// add the special game objects to the list in a "Special" category.
//		AddToList( lightObject.GetComponent<TileProperties>().tileID, lightObject );	
		AddToList( spawnerObject.GetComponent<TileProperties>().tileID, spawnerObject );	
		AddToList( startObject.GetComponent<TileProperties>().tileID, startObject );
	}
	
	public void EditorInit(){
		// Called while in the editor to initialize this object.
		Start();
	}
	
	// Add To List //
	public void AddToList( string categoryName, string tileName, string bundleName, Transform tile ){
		if ( m_TileList.ContainsKey ( categoryName ) == false ){
			// create a new category in the main list.
			m_TileList[categoryName] = new Dictionary<string, Dictionary<string, Transform>>();
			
			// also place this new category in the ordering lists appropriately.
			m_CategoryOrder.Add ( categoryName );
			m_CategoryOrder.Sort();
			m_TileOrder.Add ( categoryName, new List<string>() );
		}
		
		// add the tile to the category. (if it already exists, replace it with this newer version)
		if ( m_TileList[categoryName].ContainsKey( tileName ) ){
			
			if ( m_TileList[categoryName][tileName].ContainsKey( bundleName ) == false ){
				m_TileOrder[categoryName].Add( tileName + "/" + bundleName );
				m_TileOrder[categoryName].Sort();
			}
			
			m_TileList[categoryName][tileName][bundleName] = tile;
			
		} else {
			m_TileList[categoryName][tileName] = new Dictionary<string, Transform>();
			m_TileList[categoryName][tileName][bundleName] = tile;
			
			m_TileOrder[categoryName].Add( tileName + "/" + bundleName );
			m_TileOrder[categoryName].Sort();
		}
		
		// Also add the tile reference to the bundle-sorted lists.
		if ( m_BundleList.ContainsKey( bundleName ) == false ){
			// create a new bundle in the list.
			m_BundleList[bundleName] = new Dictionary<string, Dictionary<string, Transform>>();
			
			// also place this new bundle in the ordering lists appropriately.
			m_BundleOrder.Add ( bundleName );
			m_BundleOrder.Sort ();
			m_TileBundleOrder.Add ( bundleName, new List<string>() );
		}
		
		// add the tile to the bundle. (if it already exists, replace it with this newer version)
		if ( m_BundleList[bundleName].ContainsKey( tileName )){
			
			if ( m_BundleList[bundleName][tileName].ContainsKey( categoryName ) == false ){
				m_TileBundleOrder[bundleName].Add ( tileName + "/" + categoryName );
				m_TileBundleOrder[bundleName].Sort ();
			}
			
			m_BundleList[bundleName][tileName][categoryName] = tile;
			
		} else {
			m_BundleList[bundleName][tileName] = new Dictionary<string, Transform>();
			m_BundleList[bundleName][tileName][categoryName] = tile;
			
			m_TileBundleOrder[bundleName].Add( tileName + "/" + categoryName );
			m_TileBundleOrder[bundleName].Sort();
		}
	}
	
	public void AddToList( string combinedName, Transform tile ){
		string[] split = combinedName.Split (new char[]{'/'});
		string categoryName = split[0];
		string tileName = split[1];
		string bundleName = split[2];
		
		AddToList( categoryName, tileName, bundleName, tile );
	}
	
	public void AddToList( int setIndex, int tileIndex, Transform tile ){
		if ( m_CurrentSortMode == SortMode.Category ){
			string categoryName = m_CategoryOrder[setIndex];
			string combinedID = GetTileID( categoryName, tileIndex );
			AddToList( combinedID, tile );
		} else {
			string bundleName = m_BundleOrder[setIndex];
			string combinedID = GetTileID( bundleName, tileIndex );
			AddToList( combinedID, tile );
		}
	}
	
	// Get Tile //
	public Transform GetTile( string categoryName, string tileName, string bundleName ){
		print ("Searching for: " + categoryName + "/" + tileName + "/" + bundleName);
		if ( m_TileList.ContainsKey(categoryName) && m_TileList[categoryName].ContainsKey (tileName) && m_TileList[categoryName][tileName].ContainsKey(bundleName) ){
			return m_TileList[categoryName][tileName][bundleName];
		} else if(GameObject.Find(tileName)){
			return GameObject.Find (tileName).transform;
		}
		else
			return null;
		
	} 
	
	public Transform GetTile( string combinedName ){
		string[] split = combinedName.Split (new char[]{'/'});
		
		if(split.Length < 3){
			if(combinedName.Contains("(Clone)"))
				combinedName = combinedName.Substring(0,combinedName.Length - 7);
			print("Get custom tile: " + combinedName);
			foreach(GameObject tile in FindObjectOfType<AGF_AssetLoader>().customObjects){
				if(tile.name == combinedName)
					return tile.transform;
			}
			print("Custom tile not found: " + combinedName);
			return null;
		}
		
		string categoryName = split[0];
		string tileName = split[1];
		string bundleName = split[2];
		return GetTile( categoryName, tileName, bundleName );
	}
	
	public Transform GetTile( int setIndex, int tileIndex ){
		if ( m_CurrentSortMode == SortMode.Category ){
			string categoryName = m_CategoryOrder[setIndex];
			string combinedName = GetTileID ( categoryName, tileIndex );
			return GetTile ( combinedName );
		} else {
			string bundleName = m_BundleOrder[setIndex];
			string combinedName = GetTileID ( bundleName, tileIndex );
			return GetTile ( combinedName );
		}
	}
	
	public Transform GetTile( string setName, int tileIndex ){
		return GetTile ( GetTileID ( setName, tileIndex ) );
	}

	
	// Get Set //
	public string GetSetFromID( string tileID ){
		string[] split = tileID.Split ( new char[]{'/'} );
		
		string categoryName = split[0];
//		string tileName = split[1];
		string bundleName = split[2];
		
		if ( m_CurrentSortMode == SortMode.Category ){
			return categoryName;	
		} else {
			return bundleName;
		}
	}
	
	// Get Tile ID //
	public string GetTileID( string setName, int tileID ){
		if ( m_CurrentSortMode == SortMode.Category ){
			string categoryName = setName;
			string tileBundleName = m_TileOrder[categoryName][tileID];
			string[] split = tileBundleName.Split( new char[]{'/'} );
			string tileName = split[0];
			string bundleName = split[1];
			return categoryName + "/" + tileName + "/" + bundleName;
		} else {
			string bundleName = setName;
			string tileCategoryName = m_TileBundleOrder[bundleName][tileID];
			string[] split = tileCategoryName.Split( new char[]{'/'} );
			string tileName = split[0];
			string categoryName = split[1];
			return categoryName + "/" + tileName + "/" + bundleName;
		}
	}
	
	public string GetTileID( string categoryName, string bundleName, int tileID ){
		if ( m_CurrentSortMode == SortMode.Category ){
			return GetTileID ( categoryName, tileID );
		} else {
			return GetTileID ( bundleName, tileID );
		}
	}
	
	// Get Next/Prev Tile ID (Within the set)
	public string GetNextTileID( string categoryName, string tileName, string bundleName ){
		int currentNumber = GetTileIndex( categoryName, tileName, bundleName );
		currentNumber++;
		
		if ( m_CurrentSortMode == SortMode.Category ){
			if ( currentNumber >= m_TileOrder[categoryName].Count ){
				currentNumber = 0;
			}
		} else if ( m_CurrentSortMode == SortMode.Bundle ){
			if ( currentNumber >= m_TileBundleOrder[bundleName].Count ){
				currentNumber = 0;
			}
		}
		
		return GetTileID( categoryName, bundleName, currentNumber );
	}
	
	public string GetPrevTileID( string categoryName, string tileName, string bundleName ){
		int currentNumber = GetTileIndex( categoryName, tileName, bundleName );
		currentNumber--;
		
		if ( m_CurrentSortMode == SortMode.Category ){
			if ( currentNumber < 0 ){
				currentNumber = m_TileOrder[categoryName].Count - 1;
			}
		} else if ( m_CurrentSortMode == SortMode.Bundle ){
			if ( currentNumber < 0 ){
				currentNumber = m_TileBundleOrder[bundleName].Count - 1;
			}
		}
		
		return GetTileID( categoryName, bundleName, currentNumber );
	}
	
	// Get Tile Index
	public int GetTileIndex( string categoryName, string tileName, string bundleName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			return m_TileOrder[categoryName].IndexOf( tileName + "/" + bundleName );
		} else {
			return m_TileBundleOrder[bundleName].IndexOf( tileName + "/" + categoryName );
		}
	}
	
	// Get Number of Tiles in Set //
	public int GetNumberOfTilesInSet( string setName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			string categoryName = setName;
			
			int tileCount = m_TileList[categoryName].Count();
			foreach ( KeyValuePair<string, Dictionary<string,Transform>> pair in m_TileList[categoryName] ){
				tileCount += (pair.Value.Count - 1);
			}
			
			return tileCount;
		} else {
			string bundleName = setName;
			
			int tileCount = m_BundleList[bundleName].Count();
			foreach ( KeyValuePair<string, Dictionary<string,Transform>> pair in m_BundleList[bundleName] ){
				tileCount += (pair.Value.Count - 1);
			}
			
			return tileCount;
		}
	}
	
	public int GetNumberOfTilesInSet( int setIndex ){
		string setName = GetSetName(setIndex);
		return GetNumberOfTilesInSet( setName );
	}
	
	public int GetNumberOfTilesInSet( string categoryName, string bundleName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			return GetNumberOfTilesInSet( categoryName );
		} else {
			return GetNumberOfTilesInSet( bundleName );
		}
	}
	
	// Is Tile Loaded
	public bool IsTileLoaded( string tileID ){
		string[] split = tileID.Split( new char[]{'/'} );
		if ( m_TileList.ContainsKey( split[0] ) &&
			m_TileList[split[0]].ContainsKey( split[1] ) &&
			m_TileList[split[0]][split[1]].ContainsKey( split[2] ) ){
			return true;
		}
		return false;
	}
	
	public int GetNumberOfSets(){
		if ( m_CurrentSortMode == SortMode.Category ){
			return m_CategoryOrder.Count();	
		} else {
			return m_BundleOrder.Count();	
		}
	}
	
//	public void RemoveCategory( string categoryName ){
//		m_TileList.Remove ( categoryName );
//		
//		m_CategoryOrder.Remove ( categoryName );
//		m_CategoryOrder.Sort();
//		
//		m_TileOrder.Remove ( categoryName );
//	}
	
	public void RemoveBundle( string bundleName ){
		List<string> bundlesToRemove = new List<string>();
		
		// as we do not have a quick-lookup list for bundles, scan through the entire tile list.
		foreach( KeyValuePair<string, Dictionary<string, Dictionary<string, Transform>>> category in m_TileList ){
			foreach( KeyValuePair<string, Dictionary<string, Transform>> name in category.Value ){
				foreach( KeyValuePair<string, Transform> bundle in name.Value ){
					if ( bundle.Key == bundleName ){
						bundlesToRemove.Add ( category.Key + "/" + name.Key + "/" + bundle.Key );
					}
				}
			}
		}
		
		for( int i = 0; i < bundlesToRemove.Count; i++ ){
			string[] split = bundlesToRemove[i].Split( new char[]{'/'} );
			string category = split[0];
			string tile = split[1];
			string bundle = split[2];
			
			// remove the bundle from the category lists. (hard)
			m_TileList[category][tile].Remove( bundle );
			if ( m_TileList[category][tile].Count == 0 ){
				m_TileList[category].Remove(tile);
				m_TileOrder[category].Remove(tile);
			}
			if ( m_TileList[category].Count == 0 ){
				m_TileList.Remove(category);	
				m_TileOrder.Remove(category);
				m_CategoryOrder.Remove(category);
			}
			
			// remove the bundle from the bundle lists. (easy)
			m_BundleList.Remove( bundleName );
			m_BundleOrder.Remove( bundleName );
			m_TileBundleOrder.Remove ( bundleName );
		}
	}
	
	public string GetSetName( int setIndex ){
		if ( m_CurrentSortMode == SortMode.Category ){
			int categoryIndex = setIndex;
			return m_CategoryOrder[categoryIndex];	
		} else {
			int bundleIndex = setIndex;
			return m_BundleOrder[bundleIndex];	
		}
	}
	
	// Get Next/Prev Set Name //
	public string GetNextSetName( string categoryName, string bundleName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			return GetNextSetName( categoryName );
		} else {
			return GetNextSetName( bundleName );	
		}
	}
	
	public string GetNextSetName( string setName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			string categoryName = setName;
			int categoryIndex = m_CategoryOrder.IndexOf( categoryName );
		
			categoryIndex++;
			if ( categoryIndex >= m_CategoryOrder.Count ){
				categoryIndex = 0;	
			}
			
			return m_CategoryOrder[categoryIndex];
		} else {
			string bundleName = setName;
			int bundleIndex = m_BundleOrder.IndexOf( bundleName );
		
			bundleIndex++;
			if ( bundleIndex >= m_BundleOrder.Count ){
				bundleIndex = 0;	
			}
			
			return m_BundleOrder[bundleIndex];
		}
		
	}
	
	public string GetPrevSetName( string categoryName, string bundleName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			return GetPrevSetName( categoryName );
		} else {
			return GetPrevSetName( bundleName );	
		}
	}
	
	public string GetPrevSetName( string setName ){
		if ( m_CurrentSortMode == SortMode.Category ){
			string categoryName = setName;
			int categoryIndex = m_CategoryOrder.IndexOf( categoryName );
			
			categoryIndex--;
			if ( categoryIndex < 0 ){
				categoryIndex = m_CategoryOrder.Count-1;	
			}
			
			return m_CategoryOrder[categoryIndex];
		} else {
			string bundleName = setName;
			int bundleIndex = m_BundleOrder.IndexOf( bundleName );
			
			bundleIndex--;
			if ( bundleIndex < 0 ){
				bundleIndex = m_BundleOrder.Count-1;	
			}
			
			return m_BundleOrder[bundleIndex];
		}
	}
	
	// Sort mode
	public void SetSortMode( SortMode newSortMode ){
		if ( m_CurrentSortMode != newSortMode ){
			m_CurrentSortMode = newSortMode;
		}
	}
	public SortMode GetSortMode(){
		return m_CurrentSortMode;	
	}
	
	// Special tile accessor functions
//	public string GetLightBlockID(){
//		return lightObject.GetComponent<TileProperties>().tileID;
//	}
	
	public string GetSpawnerBlockID(){
		return spawnerObject.GetComponent<TileProperties>().tileID;
	}
	
	public string GetStartBlockID(){
		return startObject.GetComponent<TileProperties>().tileID;
	}
	
	public string GetFirstBlockID(){
		string categoryName = m_CategoryOrder[0];
		return GetTileID ( categoryName, 0 );
	}
}
