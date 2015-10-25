using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TileProperties : MonoBehaviour {
	public int tileLimit = -1;
	public bool useYOffset = false;
	
	public Vector3 size;
	[HideInInspector] public Vector3 storedScale = new Vector3(1,1,1); // scale attribute. custom blocks may be saved higher or lower than 1,1,1. Default blocks will always be 1,1,1.
	public bool scalable = true;
	public bool keepUpright = false;
	public bool placeableInBuildMode = true;
	
	public float yOffset = 0.0f;
	public string category;
	public bool isIndestructible = false;
	public Texture2D tileTexture;
	public Bounds tileBounds;
	
	public Vector3 m_InitialCenterOffset;
	public Vector3 m_CenterOffset;
	private Vector3 placementOffset = new Vector3(0,0,0);
	protected Vector3 m_InitialScale;
	
	public string tileID;
	
	protected Vector3 m_InitialRotation;
	protected Quaternion m_InitialRotationQuat;
	private bool m_IsColliding = false;
	private float m_JustBecameDynamicTimer = -1.0f;
	
	protected bool m_HasChildren;
	protected bool m_IsGreyedOut = false;
	
	protected bool m_HasBeenDestroyed = false;
	protected bool m_HasParent;
	protected float m_JustAppliedImpulseTimer = -1.0f;
	protected int m_NumberOfChildren = 0;
//	private bool m_IsModel = false;
	
	// default values.
	protected bool m_IsTrigger = true;
	protected bool m_IsRendererEnabled = false;
	protected bool m_IsKinematic;
	
	public int textureIndex = -1;
	public int normalIndex = -1;
	public float specularLevel = .5f;
	public Color colorMap;
	public Color normalMap;
	
	void Awake(){
		// this initial rotation will be used for pre-placement calculations.
		m_InitialRotation = transform.rotation.eulerAngles;	
		m_InitialRotationQuat = transform.rotation;
		
		// set the center offset, to be used when rotating blocks around the center of the block, rather than the default pivot.
		m_CenterOffset = Vector3.zero - tileBounds.center;
		m_InitialCenterOffset = m_CenterOffset;
	}
	
	void Start(){
		CustomStart();	
	}
	
	protected virtual void CustomStart(){
		// do nothing.	
	}
	
	// Called when the tile is instantiated.
	// NOTE: the tile is instantiated with the renderer OFF! if you wish to create a tile
	// and have it immediately visible, the renderer must be enabled after the Init() call.
	public virtual void Init( bool kinematicsEnabled, bool hasParent ){
		
		// store the initial scale, for use in applying with the stored scale.
		m_InitialScale = transform.localScale;
		
		m_HasParent = hasParent;
		
		// determine if we have children or not.
		m_NumberOfChildren = this.transform.childCount;
		m_HasChildren = (m_NumberOfChildren > 0);
		
		// Call Init() on all children.
		foreach (Transform child in this.transform ){
			if ( this.transform == child ) {
				continue;
			}
			if(!child.gameObject.GetComponent<TileProperties>()){
				print("Adding TileProperties to " + child.name);
				child.gameObject.AddComponent<TileProperties>();
			}
			child.gameObject.GetComponent<TileProperties>().Init( kinematicsEnabled, true );
		}
		
		if ( this.GetComponent<Renderer>() ){
			this.GetComponent<Renderer>().enabled = false;
		}
		if ( this.GetComponent<Rigidbody>() ){
			this.GetComponent<Rigidbody>().isKinematic = kinematicsEnabled;
			if ( !kinematicsEnabled ){
				m_JustBecameDynamicTimer = 2.0f;	
			}
		}
			
		m_IsKinematic = kinematicsEnabled;
	}
	
	public void Explode( bool hasParent ){
		m_HasBeenDestroyed = true;
		
		if ( m_HasChildren ) {
			// inform the grid manager that we will need to be destroyed upon exiting the game.
			GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>().AddToDestroyedTileList( this.transform );
				
			// call explode on each child.
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().Explode( true );	
			}
		} else if ( hasParent == false ){
			//Create gibs!
			string[] split = tileID.Split( new char[]{'/'} );
			string bundleName = split[split.Length-1];
			print ( this.category + " " + bundleName );
			GameObject.Find ("AGF_GibManager").GetComponent<AGF_GibManager>().SpawnGibs( this.category, bundleName, this.transform );
			
			// uninit the object.
			UnInit ();
			
		} else {
			// send this block flying in a random direction.
			SetIsKinematic( false );
			SetIsTrigger( false );
			Vector3 randomImpulse = Random.onUnitSphere;
			if ( this.GetComponent<Rigidbody>() ){
				this.GetComponent<Rigidbody>().AddForce(randomImpulse * 10, ForceMode.Impulse);
			}
			
			// begin a timer, at the end of which destroy the game object.
			m_JustAppliedImpulseTimer = 2.0f;
		}
	}
	
	public void InstantDelete(){
		// if this object has a parent, call the parent version of InstantDelete().
		if ( m_HasParent ) {
			GetParent().GetComponent<TileProperties>().InstantDelete();	
		} else {
			// kill all your children, and then yourself. (or maybe just yourself. with no one to take care of the kids, they don't have long anyway.)
			Main.SmartDestroy(gameObject);
		}
	}
	
	// Called when the tile is destroyed.
	public void UnInit(){
		// check to see if the parent object has no children remaining. if so, destroy the parent object.
		if ( m_HasParent ) {
			TileProperties tileProps = GetParent().GetComponent<TileProperties>();
			tileProps.SetChildCount( tileProps.GetChildCount() - 1 );
			if (  tileProps.GetChildCount() == 1 ) {
				Main.SmartDestroy( GetParent().gameObject );
			}
			Main.SmartDestroy(gameObject);
		} else if ( m_HasChildren == false ){
			Main.SmartDestroy(gameObject);	
		}
	}
	
	public Vector3 GetPlacementOffset(){
		return placementOffset;	
	}
	
	public virtual string GetSaveString(){
		return "";
	}
	
	public virtual void SetFromSaveString( string saveString ){
		// do nothing.
	}
	
	public bool IsColliding(){
		return m_IsColliding;
	}
	
	public bool IsTrigger(){
		return m_IsTrigger;
	}
	
	public bool IsKinematic(){
		return m_IsKinematic;	
	}
	
	public void DirectlySetIsKinematicFlag( bool isKinematic ){
		m_IsKinematic = isKinematic;	
	}
	
	public void SetIsColliding( bool isColliding ){
		m_IsColliding = isColliding;	
	}
	
	// Store the modified scale value, for application later.
	public void StoreScale( Vector3 newScale ){
		storedScale = newScale;	
	}
	
	public Vector3 GetStoredScale(){
		return storedScale;
	}
	
	public Vector3 GetWorldSize(){
		return Vector3.Scale (GetBounds().size, GetStoredScale());
	}
	
	public virtual void SetScale( Vector3 scale ){
		storedScale = scale;
		this.transform.localScale = Vector3.Scale ( storedScale, m_InitialScale );
		m_CenterOffset = Vector3.Scale (m_InitialCenterOffset, storedScale);
	}
	
	public virtual bool SetRotation( Vector3 rotation, AGF_GridManager.PivotType pivotType ){
		if ( this.keepUpright == false ){
			this.transform.eulerAngles = m_InitialRotation + rotation;
		} else {
			Vector3 newRot = m_InitialRotation;
			newRot.y += rotation.y;
			this.transform.eulerAngles = newRot;
		}
		
		// if we are rotating around the center of the object, adjust the offset position of the object as well.
		if ( pivotType == AGF_GridManager.PivotType.Center ){
			placementOffset = ( Quaternion.Euler( rotation ) * m_CenterOffset - m_CenterOffset );	
		} else if ( pivotType == AGF_GridManager.PivotType.Bottom ){
			placementOffset = Vector3.zero;
		}
		
		return this.keepUpright;
	}
	
	// Apply the scale value.
	public virtual void ApplyScaleRecursively(){
		if ( m_HasChildren ) {
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().ApplyScaleRecursively();	
			}
		} 
		
		SetScale( storedScale );
	}
	
	public Vector3 GetInitialRotation(){
		return m_InitialRotation;	
	}
	
	public Quaternion GetInitialRotationQuat(){
		return m_InitialRotationQuat;	
	}
	
	public void SetInitialRotation( Vector3 rotation ){
		m_InitialRotation = rotation;	
	}
	
	public void SetInitialRotationQuat( Quaternion rotation ){
		m_InitialRotationQuat = rotation;
	}
	
	public Bounds GetBounds(){
		if ( transform.parent ){
			return GetParent ().GetComponent<TileProperties>().GetBounds();
		} else {
			return this.tileBounds;	
		}
	}
	
	public Transform GetParent(){
		if ( transform.parent ) {
			return transform.parent.GetComponent<TileProperties>().GetParent ();	
		} else {
			return transform;	
		}
	}
	
	public virtual Renderer GetRenderer(){
		return this.GetComponent<Renderer>();
	}
	
	public bool IsIndestructible(){
		if ( transform.parent ){
			return GetParent().GetComponent<TileProperties>().IsIndestructible();
		} else {
			return isIndestructible;	
		}
	}
	
	public bool IsRendererEnabled(){
		if ( transform.parent ){
			return GetParent().GetComponent<TileProperties>().IsRendererEnabled();
		} else {
			return m_IsRendererEnabled;	
		}
	}
	
	public int GetChildCount(){
		return m_NumberOfChildren;
	}
	
	public void SetChildCount( int newCount ){
		m_NumberOfChildren = newCount;	
	}
	
	void Update(){
		CustomUpdate();	
	}
	
	protected virtual void CustomUpdate(){
		// if we are currently being destroyed, tick down the timer.
		if ( m_JustAppliedImpulseTimer > 0 ) {
			m_JustAppliedImpulseTimer -= Time.deltaTime;
			if ( m_JustAppliedImpulseTimer <= 0 ) {
//				UnInit ();
			}
		} else {
			// if we are non-kinematic, and we have settled, 
			if ( this.transform.GetComponent<Rigidbody>() && this.transform.GetComponent<Rigidbody>().isKinematic == false ) {
				if ( m_JustBecameDynamicTimer > 0 ){
					m_JustBecameDynamicTimer -= Time.deltaTime;	
				} else if ( this.transform.GetComponent<Rigidbody>().IsSleeping() ){
					
					if ( GameObject.Find("Main").GetComponent<Main>().demoMode == false ){
						// once the rigidbody falls asleep, set it to kinematic.
						this.transform.GetComponent<Rigidbody>().isKinematic = true;
					}
					
//					EventManager.InformBlockSettled( this.transform.GetInstanceID() );
					m_JustBecameDynamicTimer = -1.0f;
				}
			}	
		}
	}
	
	public Vector3 GetSize(){
		return size;	
	}
	
	public float GetYOffset(){
		return yOffset;	
	}
	
	public string GetCategory(){
		return category;	
	}
	
	// ------------------------- //
	// -- Collision Functions -- //
	// ------------------------- //
	
	protected virtual void OnCollisionEnter( Collision collisions ){
//		m_IsColliding = true;
	}
	
	protected virtual void OnCollisionExit( Collision collisions ){
//		m_IsColliding = false;
	}
	
	protected virtual void OnCollisionStay( Collision collisions ){
	}
	
	protected virtual void OnTriggerEnter( Collider colliderObj ){
		TileProperties tileProperties = colliderObj.GetComponent<TileProperties>();
		if ( GetComponent<Rigidbody>().isKinematic || colliderObj.name != "GridPlane(Clone)" ) m_IsColliding = true;
		if ( m_IsColliding && m_HasParent && tileProperties && tileProperties.GetParent() != this.GetParent() ) {
			this.transform.parent.GetComponent<TileProperties>().SetIsColliding( true );
		}
	}
	
	protected virtual void OnTriggerExit( Collider colliderObj ){
		TileProperties tileProperties = colliderObj.GetComponent<TileProperties>();
		if ( GetComponent<Rigidbody>().isKinematic || colliderObj.name != "GridPlane(Clone)" ) m_IsColliding = false;
		if ( m_IsColliding == false && m_HasParent && tileProperties && tileProperties.GetParent() != this.GetParent() ) {
			this.transform.parent.GetComponent<TileProperties>().SetIsColliding( false );
		}
	}
	
	protected virtual void OnTriggerStay( Collider colliderObj ){
		TileProperties tileProperties = colliderObj.GetComponent<TileProperties>();
		if ( GetComponent<Rigidbody>().isKinematic || colliderObj.name != "GridPlane(Clone)" ) m_IsColliding = true;
		if ( m_IsColliding && m_HasParent && tileProperties && tileProperties.GetParent() != this.GetParent() ) {
			this.transform.parent.GetComponent<TileProperties>().SetIsColliding( true );
		}
	}
	
	public virtual void SetRendererEnabled( bool enabled ){
		if ( m_IsRendererEnabled != enabled ){
			if ( m_HasChildren ) {
				List<Transform> children = new List<Transform>();
				Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
				foreach (Transform child in children){
					if ( this.transform == child ){
						continue;
					}
					child.GetComponent<TileProperties>().SetRendererEnabled( enabled );	
				}
			} else {
				if ( this.GetComponent<Renderer>() ){
					this.GetComponent<Renderer>().enabled = enabled;
				}
			}
		}
		
		m_IsRendererEnabled = enabled;
	}
	
	public virtual void SetLayer( int layer ){
		if ( m_HasChildren ) {
			
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().SetLayer( layer );	
			}
			
		} else {
			this.gameObject.layer = layer;
		}		
	}
	
	public virtual void SetIsKinematic( bool isKinematic ){
		if ( m_HasChildren ) {
			
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().SetIsKinematic( isKinematic );	
			}
		} else {
			if ( this.GetComponent<Rigidbody>() ){
				this.GetComponent<Rigidbody>().isKinematic = isKinematic;
			}
		}
		m_IsKinematic = isKinematic;	
	}
	
	public virtual void SetIsTrigger( bool isTrigger ){
		if ( m_HasChildren ) {
			
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().SetIsTrigger( isTrigger );	
			}
		} else {
			if ( this.GetComponent<Collider>() ){
				this.GetComponent<Collider>().isTrigger = isTrigger;
			}
		}		
		m_IsTrigger = isTrigger;
	}
	
	// ActivateChar() and DeactivateChar() are called from the GridManager, on the highest-level parent of the object.
	// from here, OnActivateChar() and OnDeactivateChar() are called on every child.
	public virtual void ActivateChar(){
		if ( m_HasChildren ) {
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().ActivateChar();
			}
		}
		
		m_HasBeenDestroyed = false;
		
		OnActivateChar();
	}
	
	public virtual void DeactivateChar(){
		if ( m_HasChildren ) {
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().DeactivateChar();
			}
		}
		
		m_HasBeenDestroyed = false;
		
		OnDeactivateChar();
	}
			
	public virtual void OnActivateChar(){
		// do nothing.
	}
	
	public virtual void OnDeactivateChar(){
		// do nothing.
	}
	
	public virtual void EnterDelete(){
		if ( m_HasChildren ) {
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().EnterDelete();
			}
		}
		
		OnEnterDelete();
	}
	
	public virtual void ExitDelete(){
		if ( m_HasChildren ) {
			List<Transform> children = new List<Transform>();
			Main.GetTransformsWithComponentRecursively(this.transform, "Transform", ref children);
			foreach (Transform child in children){
				if ( this.transform == child ){
					continue;
				}
				child.GetComponent<TileProperties>().ExitDelete();
			}
		}
		
		OnExitDelete();
	}
	
	public virtual void OnEnterDelete(){
		// do nothing.
	}
	
	public virtual void OnExitDelete(){
		// do nothing.
	}
	
	public virtual void OnTilePlaced(){
		// do nothing.
	}
	
	public virtual void OnTileRemoved(){
		// do nothing.
	}
	
	public void SetCustomTexture(int newTexture, int newNormal, float newSpecular, Color newColorMap, Color newNormalMap){
		
		textureIndex = newTexture;
		normalIndex = newNormal;
		specularLevel = newSpecular;
		colorMap = newColorMap;
		normalMap = newNormalMap;
		
		
		if(gameObject.name.EndsWith("(Clone)"))
			gameObject.name = Main.TrimEndFromString(gameObject.name, "(Clone)");
		
		if(!GetComponent<Renderer>()){
			gameObject.AddComponent<MeshRenderer>();
			GetComponent<Renderer>().material.shader = Shader.Find("Bumped Specular");
		}
		else if (GetComponent<Renderer>().material.shader == Shader.Find ("Diffuse")) {
			GetComponent<Renderer>().material.shader = Shader.Find("Bumped Specular");
		}
		
		// call this function on children
		foreach (Transform tile in GetComponentsInChildren<Transform>()) {
			if(tile.GetComponent<Renderer>() && tile != transform){
				if(!tile.GetComponent<TileProperties>())
					tile.gameObject.AddComponent<TileProperties>();
				
				tile.GetComponent<TileProperties>().SetCustomTexture(newTexture, newNormal, newSpecular, colorMap, normalMap);
			}
			
		}
		
		if(GetComponent<Renderer>()){
			AGF_AssetLoader assetLoader = FindObjectOfType<AGF_AssetLoader>();
			
			if(textureIndex >= 0)
				GetComponent<Renderer>().material.SetTexture("_MainTex", assetLoader.GetCustomObjectTexture(textureIndex));
			
			Texture2D oldTexture = assetLoader.normalMap;
			
			if(normalIndex >= 0){
				//				oldTexture = assetLoader.objectNormalMap;
				oldTexture = assetLoader.GetCustomObjectNormal(newNormal);
				
				GetComponent<Renderer>().material.SetTexture("_BumpMap", oldTexture);
				//				renderer.material.SetTexture("_BumpMap", assetLoader.NormalMap(oldTexture, 5));
			}
			
			
			//			Texture2D oldTexture = FindObjectOfType<AssetLoader>().normalMap;
			//			if(renderer.material.GetTexture("_BumpMap") && normalIndex >= 0){
			//				oldTexture = (Texture2D)renderer.material.GetTexture("_BumpMap");
			//				Texture2D replaceTexture = assetLoader.GetCustomObjectTexture(textureIndex);
			//
			//				oldTexture.Resize(replaceTexture.width, replaceTexture.height);
			//				oldTexture.SetPixels(0,0,oldTexture.width, oldTexture.height, replaceTexture.GetPixels(0,0,oldTexture.width, oldTexture.height));
			//
			//				oldTexture.Apply(false);
			//
			//			}
			
			GetComponent<Renderer>().material.SetColor("_Color", colorMap);
			GetComponent<Renderer>().material.SetColor("_SpecColor", normalMap);
			GetComponent<Renderer>().material.SetFloat("_Shininess", specularLevel);
			
		}
	}

}
 