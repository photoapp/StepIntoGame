using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCProperties : TileProperties {
	
	public enum DialogMode{
		StoryText, IdleText,	
	}
	private DialogMode m_CurrentDialogMode = DialogMode.IdleText;
	
	public string displayName = "";
	public string storyText = "";
	public string idleText = "";
	public float distanceFromPlayer = -1.0f;
	
//	private float sightRange = 5.0f;
//	private AGF_QuestManager m_QuestManager;
//	private bool m_HasBeenApproached = false;

	private List<Transform> childList = new List<Transform>();
	
	protected override void CustomStart (){
//		m_QuestManager = GameObject.Find ("AGF_QuestManager").GetComponent<QuestManager>();
	}
	
	public override void Init( bool kinematicsEnabled, bool hasParent ){
		// npcs are unique from normal tiles, because they have a lot of useless children. (they weren't motivated)
		// so, scan through the hierarchy, finding the children with the renderers. these are the most important children. (but don't tell the others that.)
		List<Transform> npcChildList = new List<Transform>();
		Main.GetTransformsWithComponentRecursively( this.transform, "NPCRendererChild", ref npcChildList );
		
		foreach( Transform child in npcChildList ){
			child.GetComponent<NPCRendererChild>().Init();
			childList.Add (child);
		}
		
		// even though we actually do have children, treat the npc as if it doesn't. (i think i'll pass on this one.)
		m_HasChildren = false;
		m_IsKinematic = kinematicsEnabled;
		m_InitialScale = transform.localScale;
		
//		m_HasBeenApproached = false;
		distanceFromPlayer = -1.0f;
	}
	
	protected override void CustomUpdate(){
		// check to see if the player has come within X range of this npc.
//		distanceFromPlayer = Vector3.Distance( CharacterManager.GetCurrentCharacter().localPosition, this.transform.localPosition );
//		if ( distanceFromPlayer < sightRange ){
//			if ( m_HasBeenApproached == false ){
//				m_HasBeenApproached = true;
//				m_QuestManager.OnNPCApproached( this.transform );
//			}
//		} else {
//			if ( m_HasBeenApproached == true ){
//				m_HasBeenApproached = false;
//				m_QuestManager.OnNPCLeft( this.transform );
//			}
//		}
	}
	
	public override void SetRendererEnabled( bool enabled ){
		foreach ( Transform t in childList ){
			t.GetComponent<NPCRendererChild>().SetRendererEnabled( enabled );	
		}
	}
	
	public override void SetLayer( int layer ){
		foreach ( Transform t in childList ){
			t.GetComponent<NPCRendererChild>().SetLayer( layer );	
		}
		this.gameObject.layer = layer;
	}
	
	public override string GetSaveString(){
		return displayName + ";" + storyText + ";" + idleText;
	}
	
	public override void SetFromSaveString( string saveString ){
		string[] split = saveString.Split(new char[]{';'});
		displayName = split[0];
		storyText = split[1];
		idleText = split[2];
	}
				
	// -- Interface -- //
	public void SetActiveText( DialogMode newMode ){
		m_CurrentDialogMode = newMode;
	}
	
	public string GetActiveText(){
		if ( m_CurrentDialogMode == DialogMode.IdleText ){
			return idleText;
		} else {
			return storyText;	
		}
	}
	
	public DialogMode GetActiveTextMode(){
		return m_CurrentDialogMode;
	}
	
	// -- Callbacks -- //
	protected override void OnTriggerEnter (Collider colliderObj)
	{
	}
	
	protected override void OnTriggerExit (Collider colliderObj)
	{
	}
	
	protected override void OnTriggerStay (Collider colliderObj)
	{
	}
	
	public override void OnTilePlaced(){
		// register this npc with the npc manager.
//		GameObject.Find ("QuestManager").GetComponent<QuestManager>().RegisterNPC( this.transform );
		
		// set the display name (the name used in the NPC Manager and for speaking) to the default value.
		if ( displayName == "" ){
			displayName = Main.TrimEndFromString( this.name, "(Clone)" );
		}
	}
	
	public override void OnTileRemoved(){
//		GameObject.Find ("QuestManager").GetComponent<QuestManager>().UnRegisterNPC( this.transform );
	}
	
}
