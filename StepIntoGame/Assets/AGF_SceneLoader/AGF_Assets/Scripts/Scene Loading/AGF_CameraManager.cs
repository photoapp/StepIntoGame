using UnityEngine;
using System.Collections;

public class AGF_CameraManager : MonoBehaviour {
	
	public Camera mainCamera;
	[HideInInspector]public CameraClearFlags oldClearFlag;

	// Use this for initialization
	void Start () {
		InitCamera();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public Camera GetMainCamera(){
		return mainCamera;	
	}
	
	public void StoreCameraClearFlag(){
		oldClearFlag = mainCamera.clearFlags;	
	}
	
	public void ResetCameraClearFlag(){
		mainCamera.clearFlags = oldClearFlag;	
	}
	
	public void InitCamera(){
	}
}
