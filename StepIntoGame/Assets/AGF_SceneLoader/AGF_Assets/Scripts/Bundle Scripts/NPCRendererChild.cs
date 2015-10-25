using UnityEngine;
using System.Collections;

public class NPCRendererChild : MonoBehaviour {
	
	public void Init(){
		
	}
	
	public void SetRendererEnabled( bool enabled ){
		this.GetComponent<Renderer>().enabled = enabled;	
	}
	
	public void SetLayer( int layer ){
		this.gameObject.layer = layer;
	}
}
