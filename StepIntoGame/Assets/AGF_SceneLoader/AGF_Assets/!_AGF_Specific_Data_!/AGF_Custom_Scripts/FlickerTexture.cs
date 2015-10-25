using UnityEngine;
using System.Collections;

public class FlickerTexture : MonoBehaviour {

	// Used for pumpkin head

	public int percentOn = 50;
	public int framesPerChange;

	int frameCount;


	void Update () {
		bool oldOn = GetComponent<Renderer>().enabled;

		if(frameCount >= framesPerChange){
			if(Random.Range(0,100) < percentOn){
				if(!oldOn){
					if(GetComponent<Renderer>())
						GetComponent<Renderer>().enabled = true;
					if(GetComponent<AudioSource>())
						GetComponent<AudioSource>().Play();
					if(GetComponent<Light>())
						GetComponent<Light>().enabled = true;
				}
			}
			else{
				if(GetComponent<Renderer>())
					GetComponent<Renderer>().enabled = false;
				if(GetComponent<Light>())
					GetComponent<Light>().enabled = false;
			}
			frameCount = 0;
		}
		else
			frameCount++;

		if(!transform.parent)
			Destroy(gameObject);
	}

}
