using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightProperties : TileProperties
{
	private float m_LightRange;
	private float m_LightIntensity;
	
	private float minRange = 0.0f;
	private float minIntensity = 0.0f;
	private float maxIntensity = 8.0f;
	
	private Transform m_LightChild;
	
	public override void Init (bool kinematicsEnabled, bool hasParent){
		base.Init (kinematicsEnabled, hasParent);
		
		List<Transform> lightObjects = new List<Transform>();
		Main.GetTransformsWithComponentRecursively( this.transform, "Light", ref lightObjects );
		
		if ( lightObjects.Count > 0 ){
			m_LightChild = lightObjects[0];
			m_LightRange = m_LightChild.GetComponent<Light>().range;	
			m_LightIntensity = m_LightChild.GetComponent<Light>().intensity;	
		}
	}
	
	public override void SetScale( Vector3 scale ){
		
	}
	
	public void SetRange( float newRange ){
		if ( newRange != m_LightRange ){
			if ( newRange < minRange ){
				newRange = minRange;
			}
			
			m_LightRange = newRange;
			
			if ( m_LightChild != null ){
				m_LightChild.GetComponent<Light>().range = newRange;	
			}
		}
	}
	
	public float GetRange(){
		return m_LightRange;
	}
	
	public void SetIntensity( float newIntensity ){
		if ( newIntensity != m_LightIntensity ){
			if ( newIntensity < minIntensity ){
				newIntensity = minIntensity;
			}
			
			if ( newIntensity > maxIntensity ){ 
				newIntensity = maxIntensity;	
			}
			
			m_LightIntensity = newIntensity;
			
			if ( m_LightChild != null ){
				m_LightChild.GetComponent<Light>().intensity = newIntensity;	
			}
		}
	}
	
	public float GetIntensity(){
		return m_LightIntensity;	
	}
	
	public override string GetSaveString (){
		return m_LightRange + "&" + m_LightIntensity;
	}
	
	public override void SetFromSaveString (string saveString){
		string[] split = saveString.Split ( new char[]{'&'} );
		if(split.Length>=2){
		float outFloat;
		float.TryParse( split[0], out outFloat );
		SetRange( outFloat );
		
		float.TryParse( split[1], out outFloat );
		SetIntensity( outFloat );
	}
	}

	public override void ActivateChar (){
		base.ActivateChar ();
		
		// turn off the renderer.
		if ( this.GetComponent<Renderer>() != null ){
			this.GetComponent<Renderer>().enabled = false;	
		}
		
		// turn off the collider.
		if ( this.GetComponent<Collider>() != null ){
			this.GetComponent<Collider>().enabled = false;	
		}
	}
	
	public override void DeactivateChar (){
		base.DeactivateChar ();
		
		// turn on the renderer.
		if ( this.GetComponent<Renderer>() != null ){
			this.GetComponent<Renderer>().enabled = true;	
		}
			
		// turn on the collider.
		if ( this.GetComponent<Collider>() != null ){
			this.GetComponent<Collider>().enabled = true;	
		}
	}
}

