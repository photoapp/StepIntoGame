using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AGF_GibManager : MonoBehaviour {
	
//	[System.Serializable]
//	public class GibList{
//		public Transform[] gibs;	
//		public string categoryName;
//		public Transform gibSettings;
//		public Transform remains;
//		public Transform deathEffect;
//	}
//	
//	public GibList[] gibList;
	
//	private Dictionary<string, GibList> m_GibLookupTable;
	private Dictionary<string,Dictionary<string,List<Transform>>> m_GibLookupTable;
	private List<Transform> m_ActiveGibs;
	public Transform smallGibObject;
	public Transform largeGibObject;
	
	private void Start(){
		m_GibLookupTable = new Dictionary<string, Dictionary<string,List<Transform>>>();
		m_ActiveGibs = new List<Transform>();
		
//		for ( int i = 0; i < gibList.Length; i++ ){
//			m_GibLookupTable.Add ( gibList[i].categoryName, gibList[i] );
//		}
		
//		EventManager.DeactivateChar += DeactivateChar;
	}
	
	public void EditorInit(){
		// Called while in the editor to initialize this object.
		Start();
	}
	
	public void RemoveBundle( string bundleName ){
		// grab all elements of this bundle, and systematically remove them from their appropriate categories here.
		// once a category list has only 1 element in it, remove the list.
//		List<Transform> tiles = GameObject.Find ("TileListManager").GetComponent<TileListManager>().GetTilesInBundle( bundleName );
		
		if ( m_GibLookupTable.ContainsKey( bundleName ) ){
			m_GibLookupTable[bundleName].Clear();
			m_GibLookupTable.Remove( bundleName );
		}
	}	
	
	public void AddGibTolist( string categoryName, string bundleName, Transform newGib ){
		if ( m_GibLookupTable.ContainsKey(bundleName) == false ){	
			m_GibLookupTable.Add ( bundleName, new Dictionary<string,List<Transform>>() );	
		}
		if ( m_GibLookupTable[bundleName].ContainsKey(categoryName) == false ){
			m_GibLookupTable[bundleName][categoryName] = new List<Transform>();
		}
		
		m_GibLookupTable[bundleName][categoryName].Add( newGib );
	}
	
	public void AddGibSettingsToList( string categoryName, string bundleName, Transform newSettings ){
		if ( m_GibLookupTable.ContainsKey(bundleName) == false ){	
			m_GibLookupTable.Add ( bundleName, new Dictionary<string,List<Transform>>() );	
		}
		if ( m_GibLookupTable[bundleName].ContainsKey(categoryName) == false ){
			m_GibLookupTable[bundleName][categoryName] = new List<Transform>();
		}
		
		m_GibLookupTable[bundleName][categoryName].Insert( 0, newSettings );
	}
	
	public Transform[] GetGibList( string categoryName, string bundleName ){
		int length = 0;
		if ( m_GibLookupTable.ContainsKey( bundleName ) ){
			if ( m_GibLookupTable[bundleName].ContainsKey( categoryName ) ){	
				length = m_GibLookupTable[bundleName][categoryName].Count-1;
			}
		}
		if ( length > 0 ){
			Transform[] result = new Transform[length-1];
			for ( int i = 1; i < length; i++ ){
				result[i-1] = m_GibLookupTable[bundleName][categoryName][i];	
			}
			return result;
		} else {
			return new Transform[0];
		}
	}
	
	public GibSettings GetGibSettings( string categoryName, string bundleName ){
		if ( m_GibLookupTable.ContainsKey( bundleName ) ){
			if ( m_GibLookupTable[bundleName].ContainsKey( categoryName ) ){	
				return m_GibLookupTable[bundleName][categoryName][0].GetComponent<GibSettings>();
			}
		}
		return null;
	}
	
	public Transform GetRemains( string categoryName, string bundleName ){
		GibSettings gibSettings = GetGibSettings(categoryName,bundleName); 
		if ( gibSettings != null ){
			return gibSettings.remains;
		}
		return null;
	}
	
	public Transform GetDeathEffect( string categoryName, string bundleName ){
		GibSettings gibSettings = GetGibSettings(categoryName,bundleName); 
		if ( gibSettings != null ){
			return gibSettings.deathEffect;
		}
		return null;
	}
	
	public void SpawnGibs( string categoryName, string bundleName, Transform parent ){
		GibSettings gibSettings = this.GetGibSettings( categoryName, bundleName );
		Transform gibObject;
		
		// determine how many gibs will spawn, and with what force.
		Vector3 currentSize = parent.GetComponent<TileProperties>().GetSize();
		float forceScalar = 10;
		
		float numberOfGibs = currentSize.x * currentSize.y * currentSize.z * 2;
		if ( numberOfGibs < 2 ) numberOfGibs = 2;
		
		if ( numberOfGibs < 20 ) {
			gibObject = smallGibObject;
		} else {
			gibObject = largeGibObject;
			numberOfGibs = numberOfGibs/4;
			forceScalar = forceScalar/4;
		}
		
		// control the maximum number of gibs.
		if ( gibSettings != null ){
			if ( numberOfGibs > gibSettings.maxNumber ) {
				numberOfGibs = gibSettings.maxNumber;
			}	
		} else {
			if ( numberOfGibs > 50 ) {
				numberOfGibs = 50;
			}	
		}
		
		// spawn the gibs.
		Transform[] gibList = this.GetGibList(categoryName,bundleName);
		Vector3 transformCenter = parent.GetComponent<Renderer>().bounds.center;
		
		for (int i = 0; i < numberOfGibs; i++){
			Transform gib;
			if ( gibList.Length > 0 ){
				gib = (Transform)Instantiate(gibList[Random.Range (0,gibList.Length)]);
			} else {
				gib = (Transform)Instantiate(gibObject);
			}
			
			Vector3 newPos = new Vector3(0,0,0);
			newPos.x = Random.Range(transformCenter.x - currentSize.x/2, transformCenter.x + currentSize.x/2);
			newPos.y = Random.Range(transformCenter.y - currentSize.y/2, transformCenter.y + currentSize.y/2);
			newPos.z = Random.Range(transformCenter.z - currentSize.z/2, transformCenter.z + currentSize.z/2);
			
			gib.position = newPos;
			
			Vector3 randomImpulse = Random.onUnitSphere;
			gib.GetComponent<Rigidbody>().AddForce(randomImpulse * 5, ForceMode.Impulse);
			
			gib.GetComponent<GibProperties>().Init ( parent );
			gib.transform.parent = this.transform;
			
			// if the gib should not destroy itself, add it to the active gib list.
			if ( gibSettings != null && gibSettings.persistOnDeath ){
				m_ActiveGibs.Add ( gib );
			}
		}
		
		// spawn remains, if necessary.
		if ( this.GetRemains(categoryName,bundleName) != null ){
			Transform remains = (Transform)Instantiate(this.GetRemains(categoryName,bundleName));	
			remains.position = parent.position;
			remains.localScale = parent.localScale;
			remains.rotation = parent.rotation;
			remains.parent = this.transform;
			m_ActiveGibs.Add ( remains );
		}
		
		// play the death effect, if necessary.
		if ( this.GetDeathEffect(categoryName,bundleName) != null ){
			Transform deathEffect = (Transform)Instantiate(this.GetDeathEffect(categoryName,bundleName));
			deathEffect.position = parent.position;
			deathEffect.rotation = parent.rotation;
			deathEffect.parent = this.transform;
		}
	}
	
	// -- Callbacks -- //
	public void ClearActiveGibs(){
		foreach ( Transform t in m_ActiveGibs ){
			Destroy ( t.gameObject );
		}	
		
		m_ActiveGibs.Clear();
	}
}
