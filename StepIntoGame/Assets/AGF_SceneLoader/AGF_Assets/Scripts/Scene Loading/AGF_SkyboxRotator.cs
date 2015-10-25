using UnityEngine;
using System.Collections;

public class AGF_SkyboxRotator : MonoBehaviour {
	
	public bool autoRotate = false;
	public float rotation = 0;
	public float autoRotationSpeed = 0;
	public Color tint = Color.white;

	// Update is called once per frame
	private void Update () {
		//if ( autoRotate ) rotation += autoRotationSpeed;
		//Quaternion rot = Quaternion.Euler (0f, rotation, 0f);
        //Matrix4x4 m = Matrix4x4.TRS (Vector3.zero, rot, new Vector3(1,1,1) );
		//this.GetComponent<Skybox>().material.SetMatrix("_Rotation", m);
		//this.GetComponent<Skybox>().material.SetColor("_Tint", tint );
	}
}
