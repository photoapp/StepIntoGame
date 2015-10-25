using UnityEngine;
using System.Collections;

public class GibProperties : MonoBehaviour {
	
	private float m_DeathTimer;
	private bool m_PersistOnDeath;
	private bool m_HasInit = false;
	[HideInInspector]public string category;
	[HideInInspector]public string bundle;
	
	public void Init( Transform parent ){
		GibSettings gibSettings = GameObject.Find ("AGF_GibManager").GetComponent<AGF_GibManager>().GetGibSettings(category, bundle);
		
		if ( gibSettings != null ){
			m_DeathTimer = gibSettings.deathTimer;
			m_PersistOnDeath = gibSettings.persistOnDeath;
		} else {
			m_DeathTimer = 5.0f;	
			m_PersistOnDeath = false;
		}
		m_HasInit = true;
	}
	
	private void Update(){
		if ( m_HasInit && !m_PersistOnDeath ){
			m_DeathTimer -= Time.deltaTime;
			if ( m_DeathTimer <= 0 ){
				Destroy (this.gameObject);
			}
		}
	}
}