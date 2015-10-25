using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(AGF_IntegrationManager))]

public class AGF_IntegrationManagerEditor : Editor {
	
	private AGF_CameraManager m_CameraManager;
	private AGF_IntegrationManager m_IntegrationManager;
	public void OnEnable(){
		AGF_IntegrationManager integrationManager = (AGF_IntegrationManager)target;
		GameObject obj = integrationManager.gameObject;
		
		m_IntegrationManager = integrationManager; //GameObject.Find ("AGF_Integration").GetComponent<AGF_IntegrationManager>();
		 
		List<Transform> cameraComponents = new List<Transform>();
		Main.GetTransformsWithComponentRecursively( obj.transform, "AGF_CameraManager", ref cameraComponents );
		
		if ( cameraComponents.Count == 1 ){
			m_CameraManager = cameraComponents[0].GetComponent<AGF_CameraManager>();
		}
	}
	
	private bool cameraFoldedOut = false;
	private bool customObjFoldedOut = false;
	public override void OnInspectorGUI() {
		
		cameraFoldedOut = EditorGUILayout.Foldout( cameraFoldedOut, "Camera" );
		
		if ( cameraFoldedOut ){
			// store the old color.
			Color prevColor = GUI.color;
			
			// first, assign the main camera prefab.
			bool cameraWasNull = !m_CameraManager.mainCamera;
			m_CameraManager.mainCamera = (Camera)EditorGUILayout.ObjectField("Main Camera", m_CameraManager.mainCamera, typeof(Camera), true);
			if ( cameraWasNull && m_CameraManager.mainCamera != null ){
				// Set the prefab dirty if we detect a change.
				EditorUtility.SetDirty( m_CameraManager );	
			}
			
			// check if the camera is configured.
			bool isCameraConfigured = false;
			if ( m_CameraManager.mainCamera != null ){
				isCameraConfigured = IsCameraConfigured( m_CameraManager.mainCamera );
			}
			
			// if the camera is null or has already been configured, we should not allow the configure button to be pressed.
			if ( !m_CameraManager.mainCamera || isCameraConfigured ){
				GUI.color = Color.grey;
			}
			
			if ( GUILayout.Button ("Configure Camera Prefab") ){
				if ( GUI.color != Color.grey ){
					// first, store the original value of the camera clear flag, so we can reset it if necessary later.
					m_CameraManager.StoreCameraClearFlag();
						
					// add all the necessary image effect scripts to the camera. (order matters)
					DestroyAllComponents( m_CameraManager.mainCamera );
					AddAllComponents( m_CameraManager.mainCamera );
					
					// change the camera clear flag to skybox.
					m_CameraManager.mainCamera.clearFlags = CameraClearFlags.Skybox;
					
					// a change occured, so set dirty.
					EditorUtility.SetDirty( m_CameraManager );
				}
			}
			GUI.color = prevColor;
			
			// if the camera is null or the camera has not yet been configured, we can't press the reset button.
			if ( !m_CameraManager.mainCamera || !isCameraConfigured ){
				GUI.color = Color.grey;
			}
				
			if ( GUILayout.Button ("Reset Camera Prefab") ){
				if ( GUI.color != Color.grey ){
					DestroyAllComponents( m_CameraManager.mainCamera );
					m_CameraManager.ResetCameraClearFlag();
					
					// a change occured, so set dirty.
					EditorUtility.SetDirty( m_CameraManager );
				}
			}
			GUI.color = prevColor;
		}
		
		EditorGUILayout.Separator();
		
		customObjFoldedOut = EditorGUILayout.Foldout( customObjFoldedOut, "Custom Code Object" );
			
		if ( customObjFoldedOut ){
			GameObject prev = m_IntegrationManager.customCodeManager;
			m_IntegrationManager.customCodeManager = (GameObject)EditorGUILayout.ObjectField("Custom Code Manager", m_IntegrationManager.customCodeManager, typeof(GameObject), true);
		
			if ( prev != m_IntegrationManager && m_IntegrationManager ){
				// a change occured, so set dirty.
				EditorUtility.SetDirty( m_IntegrationManager );
			}
		}
	}	
	
	public static bool IsCameraConfigured( Camera cam ){
		if ( cam == null ) return false;
		if ( !cam.GetComponent<SSAOEffect>() ||
			!cam.GetComponent<Skybox>() ||
			!cam.GetComponent<DepthOfField34>() ||
			!cam.GetComponent<GlobalFog>() ||
			!cam.GetComponent<BloomAndLensFlares>() ||
			!cam.GetComponent<ContrastEnhance>() ||
			!cam.GetComponent<AntialiasingAsPostEffect>() ||
			!cam.GetComponent<Vignetting>() ) return false;
		return true;
	}	
	
	private void DestroyAllComponents( Camera cam ){
		Main.DestroyComponentIfExisting( cam.transform, "SSAOEffect" );
		Main.DestroyComponentIfExisting( cam.transform, "Skybox" );
		Main.DestroyComponentIfExisting( cam.transform, "DepthOfField34" );
		Main.DestroyComponentIfExisting( cam.transform, "GlobalFog" );
		Main.DestroyComponentIfExisting( cam.transform, "BloomAndLensFlares" );
		Main.DestroyComponentIfExisting( cam.transform, "ContrastEnhance" );
		Main.DestroyComponentIfExisting( cam.transform, "AntialiasingAsPostEffect" );
		Main.DestroyComponentIfExisting( cam.transform, "Vignetting" );
		
		// this component is added in AGF_AssetBundleResourceExtractor.cs if scene was loaded into unity.
		Main.DestroyComponentIfExisting( cam.transform, "AGF_SkyboxRotator" );
	}
	
	private void AddAllComponents( Camera cam ){
		Main.AddComponentIfMissing( cam.transform, "SSAOEffect" );
		Main.AddComponentIfMissing( cam.transform, "Skybox" );
		Main.AddComponentIfMissing( cam.transform, "DepthOfField34" );
		cam.gameObject.GetComponent<DepthOfField34>().enabled = false;
		Main.AddComponentIfMissing( cam.transform, "GlobalFog" );
		Main.AddComponentIfMissing( cam.transform, "BloomAndLensFlares" );
		Main.AddComponentIfMissing( cam.transform, "ContrastEnhance" );
		Main.AddComponentIfMissing( cam.transform, "AntialiasingAsPostEffect" );
		Main.AddComponentIfMissing( cam.transform, "Vignetting" );
	}
}
