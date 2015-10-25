using UnityEngine;
using System.Collections.Generic;

public class AGF_GridManager : MonoBehaviour{
	
	public float cellSize;
	
	public Vector3 initialDimensions; 
	private Vector3 m_CurrentDimensions;
	
	private List<Transform> m_DestroyedTiles = new List<Transform>();
	private Dictionary<int,Transform> m_CurrentPlacedTileList;
	private List<AGF_TileDataStruct> m_CurrentRecordedActions;
	
	private int m_CurrentRecordedActionsPointer = 0;
//	private int m_GameStartPointer = 0;
	private Vector3 m_CurrentRotation = new Vector3(0,0,0);
	
	// For collision detection purposes, all grid pieces will be scaled down by this amount.
	private float scaleModifier = 0.003f;
	
	public enum PivotType{
		Bottom, Center,	
	}
	private PivotType m_CurrentPivotType = PivotType.Center;
	
	// scene references
	private AGF_TileListManager m_TileListManager;
	private AGF_CustomCodeManager m_CustomCodeManager;

	private int m_CurrentOperationGroupID = 0;

	
	private void Start(){
		m_CurrentDimensions = initialDimensions;	
		m_CurrentPlacedTileList = new Dictionary<int, Transform>();
		m_CurrentRecordedActions = new List<AGF_TileDataStruct>();
		
		m_TileListManager = GameObject.Find ("AGF_TileListManager").GetComponent<AGF_TileListManager>();
		GameObject obj = GameObject.Find ("AGF_Integration").GetComponent<AGF_IntegrationManager>().customCodeManager;
//		print ( "Custom Code Manager = " + obj );
		if ( obj != null ){
			m_CustomCodeManager = obj.GetComponent<AGF_CustomCodeManager>();
		}
	}
	
	public void EditorInit(){
		// Called while in the editor to initialize this object.
		Start();
	}
	
	public Vector3 GetWorldDimensions(){
		Vector3 result = m_CurrentDimensions;
		
		result.x *= cellSize;
		result.z *= cellSize;
		
		return result;
	}
	
	public void SetCurrentDimensions( Vector3 newDimensions ){
		m_CurrentDimensions = newDimensions;
	}
	
	public Vector3 GetCurrentDimensions(){
		return m_CurrentDimensions;	
	}
	
	public Dictionary<int,Transform> GetPlacedTileList(){
		return m_CurrentPlacedTileList;	
	}
	
	public void AddToDestroyedTileList( Transform t ){
		m_DestroyedTiles.Add (t);	
	}
	
	public void RemoveFromDestroyedTileList( Transform t ){
		m_DestroyedTiles.Remove(t);	
	}
	
	public void DeleteAll(){
		// delete everything, and start completely fresh.
		foreach( var pair in m_CurrentPlacedTileList ){
			pair.Value.GetComponent<TileProperties>().OnTileRemoved();
			Destroy (pair.Value.gameObject);
		}
		m_CurrentPlacedTileList.Clear();
		m_CurrentRecordedActions.Clear();
		m_CurrentRecordedActionsPointer = -1;
	}
	
	public void SetRecordedActions( List<AGF_TileDataStruct> recordedActions ){
		m_CurrentRecordedActions = recordedActions;
		m_CurrentRecordedActionsPointer = -1;
	}
	
	public bool PerformPlaybackStep(){
		if ( m_CurrentRecordedActions.Count - 1 > m_CurrentRecordedActionsPointer ) {
			OnRedo( true, true );
			return true;
		} else {
			return false;	
		}
	}
	
	public void OnRedo( bool instantDelete, bool setText ){ // optional parameter!
		// move the pointer up one, and then redo the action at that position.
		m_CurrentRecordedActionsPointer++;
		
		// ensure we have not fallen off the end of the list.
		if ( m_CurrentRecordedActions.Count > m_CurrentRecordedActionsPointer ){
			
			AGF_TileDataStruct tileData = m_CurrentRecordedActions[m_CurrentRecordedActionsPointer];
			
			if ( tileData.operation == OperationID.Add ){
				// add this tile.
				Transform tile = CreateNewTile(tileData.tileID, tileData.position, tileData.scale, tileData.rotation);
//				print ("Redo Add: " + tileData.tileID);
				PlaceTile(tile, 0, 0, true, tileData.instanceID);
				
			} else if ( tileData.operation == OperationID.Delete){
				// delete this tile.
//				print (string.Format("Redo Delete: Deleting tile with instance ID: {0}. [{1}]", tileData.instanceID, m_CurrentRecordedActionsPointer));
				if(m_CurrentPlacedTileList.ContainsKey(tileData.instanceID))
					DeleteTile( m_CurrentPlacedTileList[tileData.instanceID], true, instantDelete );
				
			} else if ( tileData.operation == OperationID.Modify ){
				
				// find the next location of the tile in the list, and set the transform data to that.
			if(m_CurrentPlacedTileList.ContainsKey(tileData.instanceID)){
				Transform tileToRevert = m_CurrentPlacedTileList[tileData.instanceID];
				tileToRevert.position = tileData.position;
				tileToRevert.rotation = tileData.rotation;
				tileToRevert.GetComponent<TileProperties>().SetScale( tileData.scale );
				}
			}
			
		} else {
			// we were already at the end, so do nothing.
			m_CurrentRecordedActionsPointer = m_CurrentRecordedActions.Count - 1;
		}
	}
	
	private Transform CreateNewTile( string blockID, Vector3 pos = default(Vector3), Vector3 size = default(Vector3), Quaternion rot = default(Quaternion) ){ //optional parameters!

		Transform tile;
		if(m_TileListManager.GetTile( blockID) != null)
			tile= (Transform)Instantiate(m_TileListManager.GetTile( blockID ));
		else{
			print(blockID + " missing.");
			return null;
//			tile = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
//			tile.gameObject.AddComponent<TileProperties>();
//			
//			string[] split = blockID.Split ( new char[]{'/'} );
//			tile.name = split[1];
//			tile.GetComponent<TileProperties>().tileID = blockID;

		}
		
		if ( !rot.Equals(default(Quaternion)) ){
			tile.rotation = rot;
		} else {
			tile.GetComponent<TileProperties>().SetRotation( m_CurrentRotation, m_CurrentPivotType );
		}
			
		tile.position = pos;
				
		// we always want it to be kinematic/trigger.
		tile.gameObject.GetComponent<TileProperties>().Init( true, false );
		
		tile.gameObject.SetActive( true );
		
		// Apply any stored scale values.
		tile.GetComponent<TileProperties>().ApplyScaleRecursively();
		
		if ( tile.GetComponent<TileProperties>().scalable ){
			Vector3 scale = Vector3.Scale(tile.localScale, size);
			scale.x = scale.x - scaleModifier;
			scale.y = scale.y - scaleModifier;
			scale.z = scale.z - scaleModifier;
			tile.GetComponent<TileProperties>().SetScale( scale );
		}
		
		return tile;
	}
	
	public float GetCellSize(){
		return cellSize;	
	}
	
	public void PlaceTile( Transform tile, float x = 0.0f, float z = 0.0f, bool preventAddToRecord = false, int oldInstanceID = 0, bool createNewTile = true ){ // optional parameters!

		if (tile == null)
			return;

		float y = tile.position.y - scaleModifier;

		// do not allow the object to be placed if we are currently under the drawn grid.
//		if ( y < GameObject.Find ("DrawnGridManager").GetComponent<DrawnGridManager>().GetCurrentHeight() ){
//			return;
//		}
		
		// do not allow the object to be placed if we are targeting a block beyond our current dimensions.
		if ( (Mathf.Abs(x) > (m_CurrentDimensions.x/2) ) || Mathf.Abs (y) > m_CurrentDimensions.y || (Mathf.Abs (z) > (m_CurrentDimensions.z/2)) ){
			return;
		} 
		
		// If this tile has a limit, ensure that the limit has not been reached. If it has, delete a tile to make room.
		if ( tile.GetComponent<TileProperties>().tileLimit > -1 ){
			int count = 1;
			
			List<int> tilesToDelete = new List<int>();
			foreach( var pair in m_CurrentPlacedTileList ){
				int instanceID = pair.Key;
				Transform tileData = pair.Value;
				if ( tileData.name == tile.name ){
					count++;
					if ( count > tile.GetComponent<TileProperties>().tileLimit ){
						tilesToDelete.Add (instanceID);
					}
				}
			}
			
			// destroy all the keys we have collected in the list.
			foreach (int instanceID in tilesToDelete){
				Transform tileToDelete;
				m_CurrentPlacedTileList.TryGetValue(instanceID, out tileToDelete);
				RemoveOperationFromList( tileToDelete.GetInstanceID() );
				Destroy (tileToDelete.gameObject);
				m_CurrentPlacedTileList.Remove (instanceID);
			}
			
		}
		
		tile.GetComponent<TileProperties>().SetRendererEnabled(true);
		
		// now that the tile has been placed and has a finalized rotation, set it here.
		tile.GetComponent<TileProperties>().SetInitialRotation( tile.rotation.eulerAngles );
		tile.GetComponent<TileProperties>().SetInitialRotationQuat( tile.rotation );
	
		// add the new tile.
		m_CurrentPlacedTileList.Add( tile.GetInstanceID(), tile );
	
		// add the operation to the recorded actions list.
		if ( !preventAddToRecord ){
//			AddOperationToList( OperationID.Add, tile, tile.GetComponent<TileProperties>().tileID );
		}
		
		// if a old instance ID has been passed in, we want to modify the operation list to use the new id.
		if ( oldInstanceID != 0 ){
			ModifyOperationListInstanceID( oldInstanceID, tile.GetInstanceID() );
		}

		Vector3 scale = tile.localScale;
		scale.x = scale.x + scaleModifier;
		scale.y = scale.y + scaleModifier;
		scale.z = scale.z + scaleModifier;
		tile.localScale = scale;
		
		// set the kinematics type.
		tile.GetComponent<TileProperties>().SetIsKinematic( true );
		
		// when placing the tile, make it not a trigger.
		tile.GetComponent<TileProperties>().SetIsTrigger( false ); 
		
		// if this is the start tile, inform the character manager that the start pos has changed.
		if ( tile.name.StartsWith( "Start_Locator" ) == true ) {
			Vector3 startPos = tile.position;
			startPos.y += tile.localScale.y;
			
			AGFEventObj eventObj = new AGFEventObj( AGFEventObj.SetStartPosAndRot );
			eventObj.arguments["startPos"] = startPos;
			eventObj.arguments["startRot"] = tile.rotation;
			EventHandler.instance.dispatchEvent( eventObj );
		}
		
		// Update this tile with the necessary data from its TileDataStruct.
		tile.GetComponent<TileProperties>().SetFromSaveString( m_CurrentRecordedActions[m_CurrentRecordedActionsPointer].customString );
		
		if ( m_CustomCodeManager != null ){
			m_CustomCodeManager.OnTileInstantiated( tile.gameObject );
		}
		
		tile.GetComponent<TileProperties>().OnTilePlaced();
		
		// if we're placing tiles while in the editor, trim off the "clone" modifier.
		#if UNITY_EDITOR
			tile.name = Main.TrimEndFromString( tile.name, "(Clone)" );	
		#endif
	}
	
	public bool DeleteTile( Transform tileToDelete, bool preventAddToRecord = false, bool instantDelete = false, Vector3 explosionPoint = default(Vector3), bool inGameExplosion = false ){ //optional parameter!
		//remove the entry from the dictionary. (make sure we are not trying to delete the grid.)
		if ( tileToDelete.name.StartsWith("Grid") ) return false;
		if ( tileToDelete.name.StartsWith("BigGibs") ) return false;
		if ( tileToDelete.name.StartsWith("Gibs") ) return false;
		if ( tileToDelete.name.StartsWith("UnityTerrain") ) return false;
		
		Transform targetTile = tileToDelete.GetComponent<TileProperties>().GetParent();
		
		if ( !instantDelete && targetTile.GetComponent<TileProperties>().IsIndestructible() ) return false;
		
		// remove the tile (if it has already been removed, do not add the operation to the tile list.
//		bool result = 
			m_CurrentPlacedTileList.Remove( targetTile.GetInstanceID() ); 
		
		// if this is the start tile, inform the character manager that the start pos should be reset to zero.
		if ( targetTile.name.StartsWith( "Start_Locator" ) == true ) {
//			CharacterManager.SetStartPosition( CharacterManager.GetInitialStartPos(1), 1 );
//			CharacterManager.SetStartRotation( CharacterManager.GetInitialRotation() );
		}
		
		targetTile.GetComponent<TileProperties>().OnTileRemoved();	
		
		// Begin the explosion. (the object will be [eventually] destroyed as a result of this process)
		if ( instantDelete ){
			targetTile.transform.GetComponent<TileProperties>().InstantDelete();
		} else {
			targetTile.transform.GetComponent<TileProperties>().Explode( false );
		}
		
		return true;
	}
	
	private void RemoveOperationFromList( int instanceID ){
		// find the struct with this instanceID, and remove it.
		int index = 0;
		for (int i = 0; i < m_CurrentRecordedActions.Count; i++){
			if ( m_CurrentRecordedActions[i].instanceID == instanceID ){
				index = i;
				break;
			}
		}
		// remove the operation.
		m_CurrentRecordedActions.RemoveAt(index);
		// decrement the pointer.
		m_CurrentRecordedActionsPointer--;
	}
	
	/*
	private void AddOperationToList( OperationID op, Transform tile, string id ){
		// if we are currently not at the end of our list, wipe everything from our current point to the end.
		if ( m_CurrentRecordedActionsPointer != (m_CurrentRecordedActions.Count-1) ){
			while( (m_CurrentRecordedActions.Count-1) > m_CurrentRecordedActionsPointer){
				m_CurrentRecordedActions.RemoveAt (m_CurrentRecordedActions.Count-1);
			}
		}
		
		m_CurrentRecordedActions.Add ( new AGF_TileDataStruct( op, id, tile.GetInstanceID(), tile.position, tile.localScale, tile.GetComponent<TileProperties>().GetInitialRotationQuat(), tile.GetComponent<TileProperties>().GetSaveString() ) );
		m_CurrentRecordedActionsPointer = m_CurrentRecordedActions.Count-1;
		
//		print (string.Format("AddOperationToList(): {0}, {1}, {2}, {3}", m_CurrentRecordedActions[m_CurrentRecordedActionsPointer].operation, 
//			m_CurrentRecordedActions[m_CurrentRecordedActionsPointer].tileID,
//			m_CurrentRecordedActions[m_CurrentRecordedActionsPointer].position,
//			m_CurrentRecordedActions[m_CurrentRecordedActionsPointer].rotation ));
	}
	*/
	
	private void ModifyOperationListInstanceID( int oldID, int newID ){
		// find the recorded action that used the oldID.
		// and set that ID to the newID.
		for (int i = 0; i < m_CurrentRecordedActions.Count; i++){
			AGF_TileDataStruct tileData = m_CurrentRecordedActions[i];
			if ( tileData.instanceID == oldID ){
//				print (string.Format("Changing ID {0} to {1} at [{2}].", oldID, newID, i));
				tileData.instanceID = newID;
				m_CurrentRecordedActions[i] = tileData;
//				print (m_CurrentRecordedActions[i].instanceID);
			}
		}
	}
	
	public void ActivateChar(){
//		m_GameStartPointer = m_CurrentRecordedActionsPointer;
		
		SetGameObjectVisibility( false );
		InformTilesOfActivation( true );
	}
	
	private void MakeAllBlocksKinematic( bool kinematicStatus ){
		foreach (var pair in m_CurrentPlacedTileList ){
			Transform tile = pair.Value;
			tile.GetComponent<TileProperties>().SetIsKinematic( kinematicStatus );
		}	
	}
	
	private void SetGameObjectVisibility( bool visible ){
		// scan through the tile list, searching for any game objects that need to have their visibility changed.
		foreach ( KeyValuePair<int, Transform> pair in m_CurrentPlacedTileList ){
			string name = pair.Value.transform.name;
			if ( name.StartsWith ("Start_Locator") || name.StartsWith ("SphereLight") || name.StartsWith ("Monster_Locator") 
				|| pair.Value.GetComponent<TileProperties>().category == "Light_Point" ){
				pair.Value.transform.GetComponent<Renderer>().enabled = visible;	
				pair.Value.GetComponent<Collider>().enabled = visible;
			}
		}
	}
	
	private void InformTilesOfActivation( bool activate ){
		foreach ( KeyValuePair<int, Transform> pair in m_CurrentPlacedTileList ){
			if ( activate ){
				pair.Value.GetComponent<TileProperties>().ActivateChar();	
			} else {
				pair.Value.GetComponent<TileProperties>().DeactivateChar();
			}
		}
	}
	
	public int GetCurrentOperationGroupID(){
		return m_CurrentOperationGroupID;	
	}

}