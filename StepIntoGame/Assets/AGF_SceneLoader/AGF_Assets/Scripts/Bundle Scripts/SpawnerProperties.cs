using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnerProperties : TileProperties {

	// a spawner is a special type of tile that manages it's own list of monsters.
	// it should receive events on game activate and deactivate to start/stop spawning mobs.
	
//	private int m_CurrentMonsterID;
	private Dictionary<int, Transform> m_CurrentActiveMonsters;
	private float m_SpawnTimer = -1;
	private bool m_IsActive = false;
	
	public override void Init( bool kinematicesEnabled, bool hasParent ){
		m_CurrentActiveMonsters = new Dictionary<int, Transform>();
		base.Init (kinematicesEnabled, hasParent);
	}
	
	public override void ActivateChar(){
//		print ("Activate Char! Spawn the first monster!");
//		m_CurrentMonsterID = 0;
		
		m_IsActive = true;
		
//		SpawnMonster();
		base.ActivateChar();	
	}
	
	public override void DeactivateChar(){
		print ("Deactivate char! Destroy all monsters owned by this spawner.");
		
		foreach ( KeyValuePair<int,Transform> pair in m_CurrentActiveMonsters ){
			// destroy each transform.
			Destroy ( pair.Value.gameObject );
		}
		
		m_IsActive = false;
		m_SpawnTimer = -1;
		
		m_CurrentActiveMonsters.Clear();
		
		base.DeactivateChar();	
	}
	
	protected override void CustomUpdate ()
	{
		if ( m_IsActive ){
			if ( m_SpawnTimer != -1 ){
				m_SpawnTimer -= Time.deltaTime;
				if ( m_SpawnTimer <= 0 ){
					m_SpawnTimer = -1;
					SpawnMonster();	
				}
			}
		}
		
		base.CustomUpdate();
	}
	
	public void InformMonsterDeath( int monsterID ){
		// get the monster in the dictionary, and destroy it. (or play a death anim first?)
		Destroy( m_CurrentActiveMonsters[monsterID].gameObject );
		m_CurrentActiveMonsters.Remove(monsterID);
		
		// start the spawn timer.
		m_SpawnTimer = 5.0f;
	}
	
	// -- private functions -- //
	private void SpawnMonster(){
		/*
		// instantiate a new monster, using the player controller.
		CharacterManager characterManager = GameObject.Find ("CharacterManager").GetComponent<CharacterManager>();
		
		int monsterID = Random.Range (0, characterManager.characters.Length);
		int texID = -1; // for now, offer no texture swaps.
		Transform newCharacter = (Transform)Instantiate(characterManager.characters[monsterID]);
		
		// indicate the the character is an enemy character, so that the controller will not read our input.
		newCharacter.GetComponent<CharacterProperties>().enemyCharacter = true;
		// add the monster controller component.
		newCharacter.gameObject.AddComponent<MonsterController>();
		newCharacter.gameObject.GetComponent<MonsterController>().Init( this.gameObject, m_CurrentMonsterID );
		
		newCharacter.GetComponent<CharacterEquipment>().Init();
		newCharacter.gameObject.AddComponent<ARPG_Controller>(); // FIX THIS! (we will have more than 1 type of controller soon)
		
		if ( texID > -1 ){
			newCharacter.GetComponent<CharacterProperties>().SetMainTexture( texID );
		}
		
		// TEMP! FIX LATER
		newCharacter.gameObject.AddComponent<MeleeSweepAbility>(); 
		newCharacter.gameObject.AddComponent<JumpToTargetAbility>(); 
		newCharacter.gameObject.AddComponent<SingleProjectileAbility>();
		newCharacter.gameObject.AddComponent<ShieldBlockAbility>();
		
		// init all abilities.
		PlayerAbility[] instantiatedAbilities = newCharacter.gameObject.GetComponents<PlayerAbility>();
		for ( int i = 0; i < instantiatedAbilities.Length; i++ ){
			instantiatedAbilities[i].Init ();	
		}
		// init the player controller.
		newCharacter.gameObject.GetComponent<PlayerController>().Init();
		
		// set the name.
		newCharacter.GetComponent<CharacterProperties>().characterName = GameObject.Find("CharacterManager").GetComponent<CharacterManager>().characters[monsterID].name;
		
		// set the character's initial transform information.
		newCharacter.position = this.transform.position;
		newCharacter.rotation = this.transform.rotation;
		
		// insert the new transform into the list.
		m_CurrentActiveMonsters.Add ( m_CurrentMonsterID, newCharacter );
		
		// play a particle effect.
		GameObject.Find ("EffectsManager").GetComponent<EffectsManager>().PlayUnmanagedEffect("HelixHealing", this.transform.position);
		
		m_CurrentMonsterID++;
		*/
	}
}
