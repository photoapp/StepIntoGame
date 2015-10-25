using UnityEngine;
using System.Collections;

public abstract class AGF_CustomCodeManager : MonoBehaviour {
	// This script provides a convienent place for all agf-related user defined code to live.
	
	// Whenever a collider enters into the trigger of a pickup, this function will be called.
	// return true if you detect that your player has collided with the pickup.
	public abstract bool OnPickupTriggerEnter( GameObject pickupObj, Collider colliderObj );

	// This function is called whenever a warehouse object is instantiated. you can use this time to make layer changes,
	// add scripts, etc. 
	public abstract void OnTileInstantiated( GameObject tile ); 
}
