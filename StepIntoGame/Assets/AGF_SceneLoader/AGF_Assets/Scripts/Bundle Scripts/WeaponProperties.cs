using UnityEngine;
using System.Collections;

public class WeaponProperties : TileProperties {
	
	public override void Init( bool kinematicsEnabled, bool hasParent ){
		// even though we actually do have children, treat the npc as if it doesn't. (i think i'll pass on this one.)
		m_HasChildren = false;
		m_NumberOfChildren = 0;
		m_InitialScale = transform.localScale;
		
		if ( this.GetComponent<Renderer>() ){
			this.GetComponent<Renderer>().enabled = false;
		}
		
		if ( this.GetComponent<Rigidbody>() ){
			this.GetComponent<Rigidbody>().isKinematic = kinematicsEnabled;
			if ( !kinematicsEnabled ){
//				m_JustBecameDynamicTimer = 2.0f;	
			}
		}
		
		m_IsKinematic = kinematicsEnabled;
	}
	
	public override void OnActivateChar(){
	}
	
	public override void OnDeactivateChar(){
	}
}