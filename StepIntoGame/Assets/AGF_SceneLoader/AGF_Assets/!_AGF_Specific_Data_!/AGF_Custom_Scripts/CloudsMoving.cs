using UnityEngine;
using System.Collections;

public class CloudsMoving : MonoBehaviour {
	
	public float windSpeed = 2f;
	public float directionFactor = 0.25f;
	public float distance = 1000f;
	private bool endPoint = true;
	private float initialX;
	
	void Start() {
		initialX = transform.position.x;
	}

	void Update () {
		
		if (endPoint) {
			if (transform.position.x >= initialX - distance)
				endPoint = true;
		}
		if (transform.position.x < initialX - distance)
			endPoint = false;
		
		if (!endPoint) {
			if (transform.position.x <= initialX + distance)
				endPoint = false;
		}
		if (transform.position.x > initialX + distance)
			endPoint = true;
		
		//
		
		if (endPoint) {
			transform.Translate(-windSpeed * Time.deltaTime, 0, (-windSpeed * directionFactor) * Time.deltaTime);
		}
		else {
			transform.Translate(windSpeed * Time.deltaTime, 0, (windSpeed * directionFactor) * Time.deltaTime);
		}
	}
}
