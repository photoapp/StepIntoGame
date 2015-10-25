using UnityEngine;
using System.Collections;

public class CloudsGenerator : MonoBehaviour {
	
	public GameObject mainClouds;
	public GameObject scatteringClouds;
	public GameObject cloudShadows;
	public int generationTime = 15;
	private bool switchOn = true;
	private bool CloudsGenerated = false;
	private Renderer[] renderers;
	private Renderer[] renderersMain;
	
	void Start() {
		StartCoroutine(Generate());
	}
	
	IEnumerator Generate() {
		cloudShadows.gameObject.SetActive(false);
		renderers = transform.gameObject.GetComponentsInChildren<Renderer>();
	    
	    foreach (Renderer renderer in renderers) {
	        renderer.enabled = false;
	    }
		
		yield return new WaitForSeconds(generationTime);
		
		renderersMain = mainClouds.GetComponentsInChildren<Renderer>();
		
		foreach (Renderer renderer in renderersMain) {
	        renderer.GetComponent<ParticleEmitter>().enabled = false;
	    }
	    
		cloudShadows.gameObject.SetActive(true);
		
		foreach (Renderer renderer in renderers) {
	        renderer.enabled = true;
	    }
		CloudsGenerated = true;
	}
	
	void Update () {
		if (CloudsGenerated) {
			if(Input.GetKeyDown ("c")){
				
				switchOn = !switchOn;
				
				if(switchOn) {
					cloudShadows.gameObject.SetActive(true);
				    renderers = transform.gameObject.GetComponentsInChildren<Renderer>();
				    
				    foreach (Renderer renderer in renderers) {
				        renderer.enabled = true;
				    }
			   	}
			    else{
			     	cloudShadows.gameObject.SetActive(false);
				    renderers = transform.gameObject.GetComponentsInChildren<Renderer>();
				    
				    foreach (Renderer renderer in renderers) {
				        renderer.enabled = false;
				    }
			    }
			}
		}
	}
}