using UnityEngine;
using System.Collections;

public class CloudsBehindTrees : MonoBehaviour {

	void Start () {
		GetComponent<Renderer>().material.renderQueue = 2900;
	}
}
