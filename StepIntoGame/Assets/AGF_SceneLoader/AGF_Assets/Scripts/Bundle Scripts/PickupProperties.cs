using UnityEngine;
using System.Collections;

public class PickupProperties : TileProperties {
	
	// Pickups function similarly to all other tiles, except for their additional in-game functions:
	// 1. When colliding with the player, they play a sound, add to a score, and are deleted.
	// 2. While in game, they constantly rotate.
	public Texture2D pickupTexture;
	public float lifeTimer = -1.0f;
	public int pointValue;
	
	private bool m_IsInGame = false;
	private float m_UnpickupableTimer = -1.0f;
	private float m_CurrentPickupRotation = 0f;
	private float m_PickupRotationSpeed = 2.0f;
	private System.Func<Transform,System.Void> m_DeathCallback;
	
	// references 
	private AGF_CustomCodeManager m_CustomCodeManager;
	
	public override void Init( bool kinematicsEnabled, bool hasParent ){
		base.Init ( kinematicsEnabled, hasParent );
		
		m_CurrentPickupRotation = Random.Range (0f,360f);
		
		GameObject obj = GameObject.Find ("AGF_Integration").GetComponent<AGF_IntegrationManager>().customCodeManager;
		if ( obj != null ){
			m_CustomCodeManager = obj.GetComponent<AGF_CustomCodeManager>();
		}
		
		if ( this.GetComponent<Rigidbody>() != null ){
			this.GetComponent<Rigidbody>().sleepVelocity = 1.0f;
			this.GetComponent<Rigidbody>().sleepAngularVelocity = 1.0f;
		}
	}
	
	public void SetLifeTimer( float timer ){
		lifeTimer = timer;	
	}
	
	
	public void SetUnpickupableTimer( float timer ){
		m_UnpickupableTimer = timer;
	}
	
	protected override void CustomUpdate(){
		// tick down the life timer.
		if ( lifeTimer > 0 ){
			lifeTimer -= Time.deltaTime;
			if ( lifeTimer < 1 ){
//				SetMaterialColors(new Color(lifeTimer,lifeTimer,lifeTimer,lifeTimer));	
			}
			if ( lifeTimer <= 0 ){
				// destroy the block.	
				CleanUpPickup();
			}
		}
		
		// tick down the unpickupabletimer
		if ( m_UnpickupableTimer > 0 ){
			m_UnpickupableTimer -= Time.deltaTime;
		}
		
		// while in game, slowly rotate the pickup.
		if ( GetParent().GetComponent<PickupProperties>() && GetParent().GetComponent<PickupProperties>().IsInGame() ){
			
			if ( this.GetComponent<Rigidbody>() != null ) {
				if ( this.GetComponent<Collider>().isTrigger == true ){
					Vector3 rot = GetInitialRotation();
					m_CurrentPickupRotation += m_PickupRotationSpeed;
					rot.y += m_CurrentPickupRotation;
					this.transform.eulerAngles = rot;
				}
			}
		
		}
		
	}
	
	protected override void OnCollisionEnter( Collision collisions ){
		// when the object collides with something that isn't the player or another projectile, and we are in game, become a trigger and kinematic.
		if ( collisions.collider.GetComponent("PlayerController") == null || !GetParent().GetComponent<PickupProperties>().IsInGame() || this.GetComponent<Collider>().isTrigger 
			|| collisions.collider.GetComponent<PickupProperties>() ) return;
		
		SetIsTrigger(true);
		SetIsKinematic(true);
	}
	
	public void SetIsInGame( bool isInGame ){
		m_IsInGame = isInGame;	
	}
	
	public bool IsInGame(){
		return m_IsInGame;	
	}
	
	public bool CanPickUp(){
		return m_IsInGame && m_UnpickupableTimer < 0;
	}

	public override void ActivateChar(){
		print ("ActivateChar() from PickupProperties");
		
		// make the collision a trigger collision.
		SetIsTrigger ( true );
		
		m_IsInGame = true;
	}
	
	public override void DeactivateChar(){
		print ("DeactivateChar() from PickupProperties");
		
		// make the collision a non-trigger collision.
		SetIsTrigger( false );
		
		m_IsInGame = false;
	}
	
	protected override void OnTriggerEnter (Collider colliderObj)
	{
		base.OnTriggerEnter ( colliderObj );
		
//		print (string.Format ("OnTriggerEnter() {0}, {1}", m_IsInGame, m_UnpickupableTimer));
		
		bool canPickup = false;
		
		// if we are currently in game, perform an additional task.
		if ( GetParent().GetComponent<PickupProperties>() ) {
			if ( GetParent().GetComponent<PickupProperties>().CanPickUp() ){
				canPickup = true;
			}
		} else {
			if ( CanPickUp() ){
				canPickup = true;
			}
		}
		
		if ( canPickup ){
			if ( m_CustomCodeManager != null ){
				bool result = m_CustomCodeManager.OnPickupTriggerEnter( this.gameObject, colliderObj );
				// only cause the pickup if it is the character that is intersecting with the trigger.
				if ( result ){
					OnPickup(colliderObj.gameObject);
				}
			}
		}
	}
	
	// note: this function is not the same as "OnControllerColliderHit()", a unity callback.
	public void OnCharacterControllerHit( GameObject character ){
		if ( m_UnpickupableTimer < 0 ){
			OnPickup(character);	
		}
	}
	
	private void OnPickup(GameObject character){
		print ("OnPickup()");
		
		EventHandler.instance.dispatchEvent( new AGFEventObj( AGFEventObj.PickupCollected ) );
		
		// play a sound.
//		AudioSource.PlayClipAtPoint(sounds[Random.Range(0, sounds.Length)], Camera.mainCamera.transform.position);
//		GameObject.Find ("AGF_EffectsManager").GetComponent<AGF_EffectsManager>().PlaySound("Pickup", false, Camera.mainCamera.transform.position);
		
		// play a particle effect.
//		GameObject.Find ("AGF_PickupManager").GetComponent<AGF_PickupManager>().PlayParticleEffect( this.transform.position );
		
		// add to score. (but only if the player picking this up is local)
//		if ( character.GetComponent<CharacterProperties>().IsLocalCharacter() ){
//			int result = GameObject.Find ("AGF_PickupManager").GetComponent<AGF_PickupManager>().ModifyCounter(0, GetParent().GetComponent<PickupProperties>().pointValue);
//		}
		
		// change the player's current pickup name to this one.
//		string noClone = Main.TrimEndFromString( GetParent().name, "(Clone)");
//		character.GetComponent<CharacterProperties>().SetLastPickupName( noClone );
		
		// destroy the tile.
		CleanUpPickup();
	}
	
	public void SetDeathCallback( System.Func<Transform,System.Void> deathCallback ){
		m_DeathCallback = deathCallback;
	}
	
	private void CleanUpPickup(){
		// delete this tile.
		Transform parent = GetParent();
		
		GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>().DeleteTile(parent, false, true);
		
		if ( GameObject.Find ("Main").GetComponent<Main>().demoMode ){
			GameObject.Find ("AGF_GridManager").GetComponent<AGF_GridManager>().RemoveFromDestroyedTileList( parent );
		}
		
		// call the death callback.
		if ( m_DeathCallback != null ){
			m_DeathCallback( this.transform );
		}
	}
}
