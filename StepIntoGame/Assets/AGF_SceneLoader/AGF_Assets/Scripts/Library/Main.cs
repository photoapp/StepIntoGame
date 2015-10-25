using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {
	
	public int frameRateLimit = 60;
	public bool demoMode = false;
	public bool steamMode = false;
	public static string defaultSceneLocation = "";
	
	// Use this for initialization
	void Awake () {
		Application.targetFrameRate = frameRateLimit;
		
		bool sceneFlagFound = false;
		string[] args = System.Environment.GetCommandLineArgs();	
		print ("Current command line arguments:");
		for ( int i = 0; i < args.Length; i++ ){
			if ( sceneFlagFound ){
				defaultSceneLocation += args[i] + " ";
			}
			print ( args[i] );	
			if ( args[i] == "-s" ){
				if ( i+1 < args.Length ){
					sceneFlagFound = true;
				}
			}
			if ( args[i] == "-r" ){
				string[] split = args[i+1].Split( new char[]{'x'} );
				int width, height;
				int.TryParse( split[0], out width );
				int.TryParse( split[1], out height );
				Screen.SetResolution( width, height, false );
			}
			if ( args[i] == "-q" ){
				int quality;
				int.TryParse( args[i+1], out quality );
				QualitySettings.SetQualityLevel( quality, true );
			}
		}
		print ( "Default Scene Location = " + defaultSceneLocation );
		defaultSceneLocation = TrimEndFromString( defaultSceneLocation, " " );
		print ("/end command line args");
		
		print ( "Default Scene Location = " + defaultSceneLocation );
	}
	
	private void OnGUI(){
		if ( demoMode == true ){
			string demoText = "Demo Version";
			int prevSize = GUI.skin.label.fontSize;
			FontStyle prevStyle = GUI.skin.label.fontStyle;
			
			GUI.skin.label.fontSize = 25;
			GUI.skin.label.fontStyle = FontStyle.Bold;
			Vector3 textSize = GUI.skin.label.CalcSize(new GUIContent(demoText));
			GUI.Label ( new Rect(Screen.width/2.0f - textSize.x/2.0f, 20.0f, textSize.x, textSize.y), demoText );
			
			GUI.skin.label.fontSize = prevSize;
			GUI.skin.label.fontStyle = prevStyle;
		}
	}
	
	public static void SetLayerRecursively( Transform t, int layer ){
		foreach (Transform child in t){
			if ( t.transform == child ) {
				continue;
			}
			SetLayerRecursively( child, layer );
		}
		
		t.gameObject.layer = layer;
	}
	
	public static void GetBoundsRecursively( Transform t, ref Bounds bounds ){
		foreach (Transform child in t){
			if ( t.transform == child ) {
				continue;
			}
			GetBoundsRecursively( child, ref bounds );
		}
		if ( t.GetComponent<Renderer>() ){
			bounds.Encapsulate(t.GetComponent<Renderer>().bounds);
		}
	}
	
	public static void AttachSkyboxCubemapRecursively( Transform t, Cubemap cubemap ){
		foreach ( Transform child in t ){
			if ( child == t.transform ){
				continue;	
			}
			
			AttachSkyboxCubemapRecursively( child, cubemap );
		}
		
		if ( t.GetComponent<Renderer>() ){
			for (int i = 0; i < t.GetComponent<Renderer>().sharedMaterials.Length; i++){
				if ( t.GetComponent<Renderer>().sharedMaterials[i].HasProperty("_Cube") ){
					t.GetComponent<Renderer>().sharedMaterials[i].SetTexture("_Cube", cubemap);
				}
			}
		}	
	}
	
	public static void AddComponentRecursively( Transform t, string componentType ){
		foreach ( Transform child in t ) {
			if ( child == t.transform ){
				continue;	
			}
			
			AddComponentRecursively( child, componentType );
		}
		
		if ( t.gameObject.GetComponent(componentType) == null ){
			UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(t.gameObject, "Assets/AGF_SceneLoader/AGF_Assets/Scripts/Library/Main.cs (116,4)", componentType);
		}
	}
	
	public static void AddComponentIfMissing( Transform t, string componentType ){
		if ( t.gameObject.GetComponent(componentType) == null ){
			UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(t.gameObject, "Assets/AGF_SceneLoader/AGF_Assets/Scripts/Library/Main.cs (122,4)", componentType);
		}	
	}
	
	public static void DestroyComponentIfExisting( Transform t, string componentType ){
		if ( t.gameObject.GetComponent(componentType) != null ){
			DestroyImmediate( t.gameObject.GetComponent(componentType) );
		}	
	}
	
	public static void AddSphereColliderRecursively( Transform t, float radiusScale ){
		foreach ( Transform child in t ) {
			if ( child == t.transform ){
				continue;	
			}
			
			AddSphereColliderRecursively( child, radiusScale );
		}
		
		if ( t.gameObject.GetComponent<SphereCollider>() == null ){
			t.gameObject.AddComponent<SphereCollider>();
			t.gameObject.GetComponent<SphereCollider>().radius *= radiusScale;
		}
	}
	
	public static void SetMeshColliderConvexRecursively( Transform t, bool convex ){
		foreach ( Transform child in t ){
			if ( child == t.transform ){
				continue;	
			}
			
			SetMeshColliderConvexRecursively( child, convex );
		}
		
		if ( t.gameObject.GetComponent<MeshCollider>() != null ){
			t.gameObject.GetComponent<MeshCollider>().convex = convex;	
		}
	}
	
	public static void SetTriggerRecursively( Transform t, bool isTrigger ){
		foreach( Transform child in t ){
			if ( child == t.transform ){
				continue;	
			}
			
			SetTriggerRecursively( child, isTrigger );
		}
		
		if ( t.GetComponent<Collider>() ){
			t.GetComponent<Collider>().isTrigger = isTrigger;	
		}
	}
	
	public static void GetTransformsWithComponentRecursively( Transform t, string componentType, ref List<Transform> result ){
		foreach( Transform child in t ){
			if ( child == t.transform ){
				continue;	
			}
			
			GetTransformsWithComponentRecursively( child, componentType, ref result );
		}
		
		if ( t.GetComponent(componentType) != null ){
			result.Add (t);	
		}
	}
	
	public static string TrimEndFromString( string s, string trimString ){
		string result = "";
		if ( s.EndsWith(trimString) ){
			result = s.Substring (0, s.LastIndexOf(trimString) );
		} else {
			result = s;	
		}
		return result;
	}
	
	public static string ConvertAbsoluteToLocalPath( string s ){
		string[] split = s.Split(new char[]{'/'});
		
		int assetPos = 0;
		for ( int i = 0; i < split.Length; i++ ){
			if ( split[i] == "Assets" ){
				assetPos = i;
				break;	
			}
		}
		
		string localPath = "";
		for ( int i = assetPos; i < split.Length; i++ ){
			localPath += split[i] + "/";
		}
		localPath = TrimEndFromString(localPath, "/");	
		
		return localPath;
	}
	
	// static and non-static versions.
	public static float CalculateDifferenceBetweenAngles(float firstAngle, float secondAngle){
	    float difference = secondAngle - firstAngle;
	    while (difference < -180) difference += 360;
	    while (difference > 180) difference -= 360;
	    return difference;
	}
	
	public float Ref_CalculateDifferenceBetweenAngles(float firstAngle, float secondAngle){
	    float difference = secondAngle - firstAngle;
	    while (difference < -180) difference += 360;
	    while (difference > 180) difference -= 360;
	    return difference;
	}
	
	public static bool CalculateAngleToHitTarget( Vector3 startPos, Vector3 targetPos, float impulseAmount, float gravity, ref float angle1, ref float angle2 ){
		// initially, we have two points in 3d space. we need to simulate this being in 2d space.
		Vector3 angleCalcVec = targetPos - startPos;
		angleCalcVec.y = 0;
		
		// rotate this vector so that it lines up with the x axis.
		
		// determine how much we need to rotate the vector in order to do so.
		float angle = Vector3.Angle (angleCalcVec, new Vector3(1,0,0));
		float dotProduct = Vector3.Dot (angleCalcVec, new Vector3(0,0,-1));
		
		if ( dotProduct > 0 ) angle *= -1;
		
		// rotate the vector so that it's aligned with the x-axis.
		Vector3 target = targetPos - startPos;
		target = Quaternion.AngleAxis(angle, new Vector3(0,1,0)) * target;
		
		// now, we can assume that this is a 2D problem. (z should now be 0.)
		if ( Mathf.Abs (target.z) > 0.001 ){
			Debug.LogError ("Z IS NOT ZERO! " + target.z);	
		}
		
		float v = impulseAmount;
		float x = target.x;
		float y = target.y;
		float g = -gravity;
		
		float tmp = Mathf.Pow(v, 4) - g * (g * Mathf.Pow(x, 2) + 2 * y * Mathf.Pow(v, 2));
		
		if ( tmp < 0 ){
		    // no solution
			return false;
		} else {
			// one or two possible solutions.
			angle1 = Mathf.Rad2Deg * Mathf.Atan2(Mathf.Pow(v, 2) + Mathf.Sqrt(tmp), g * x);
			angle2 = Mathf.Rad2Deg * Mathf.Atan2(Mathf.Pow(v, 2) - Mathf.Sqrt(tmp), g * x);
			return true;
		}
	}
	
	public static int RoundToNearestDivisibleNumber( ref float input, float divisibleNumber ){
		if ( input % divisibleNumber > divisibleNumber * 0.5f ){
			int division = (int)(input/divisibleNumber)+1;
			input = division * divisibleNumber;
			return division;
		} else {
			int division = (int)(input/divisibleNumber);
			input = division * divisibleNumber;
			return division;
		}
	}
	
	public static int RoundToNextDivisibleNumber( ref float input, float divisibleNumber ){
		int division = (int)(input/divisibleNumber);
		float high = divisibleNumber * (division + 1);
		float low = divisibleNumber * (division);
		
		if ( input % divisibleNumber == 0 ){
//			input = input;
			return division;
		} else if ( input % divisibleNumber > divisibleNumber/2 ){
			input = low;
			return division;
		} else {
			input = high;
			return division + 1;
		}
	}
	
	public static string TruncateString( string s, int length ){
		if ( s.Length > length ){
			s = s.Substring(0, length);	
		}
		return s;
	}
	
	public static string TruncateStringFloat( string s, int length ){
		if ( s.Length > length ){
			s = s.Substring(0, length);	
		}
		
		if ( s[s.Length-1] == '.' ){
			s = s.Substring(0, s.Length-1);
		}
		return s;
	}
	
	public static string AbridgeString( string text, int characterCount ){
		if ( text.Length > characterCount ){
			string result;
			result = text.Substring( text.Length - characterCount, characterCount );
			result = "..." + result;
			return result;
		} else {
			return text;	
		}
	}
	
	/*
	public static void SetDefaultFrameCameraRotation( Camera cam, Vector3 lookAt ){
		// this step is optional, and only serves to appropriately place the camera in preparation for a photo with rotation 45,45.
		cam.transform.position = lookAt + Vector3.one;
		//Quaternion.AngleAxis(-45, new Vector3(1,0,0)) * new Vector3(0,0,1);
		//cam.transform.position = Quaternion.AngleAxis(45, new Vector3(0,0,1)) * cam.transform.position;
	}
	
	public static bool FrameCameraToBounds( Camera cam, Transform target, Vector3 lookAt, Bounds bounds ){
		// begin by looking at the target, then positioning the camera 100 units backward from it.
		cam.transform.LookAt(lookAt);
		cam.transform.position = lookAt - ( cam.transform.forward * 100.0f );
		
		// grab all the bounding box points.
		Vector3[] boundingBoxPoints = new Vector3[8];
		boundingBoxPoints[0] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, 1)));
		boundingBoxPoints[1] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, 1)));
		boundingBoxPoints[2] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, -1)));
		boundingBoxPoints[3] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, -1)));
		boundingBoxPoints[4] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, 1)));
		boundingBoxPoints[5] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, 1)));
		boundingBoxPoints[6] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, -1)));
		boundingBoxPoints[7] = target.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, -1)));
		
		// -- determine if the height or width is greater. -- //
		int highestYid = -1;
		float hiY = 0.0f;
		for ( int i = 0; i < boundingBoxPoints.Length; i++ ){
			Vector3 viewPoint = cam.WorldToViewportPoint(boundingBoxPoints[i]);
			if ( viewPoint.y >= hiY || i == 0){
				highestYid = i;
				hiY = viewPoint.y;
			}
		}
		
		int lowestYid = -1;
		float lowY = 1.0f;
		for ( int i = 0; i < boundingBoxPoints.Length; i++ ){
			Vector3 viewPoint = cam.WorldToViewportPoint(boundingBoxPoints[i]);
			if ( viewPoint.y <= lowY || i == 0){
				lowestYid = i;
				lowY = viewPoint.y;
			}
		}
		
		float height = hiY - lowY;
		
		int highestXid = -1;
		float hiX = 0.0f;
		for ( int i = 0; i < boundingBoxPoints.Length; i++ ){
			Vector3 viewPoint = cam.WorldToViewportPoint(boundingBoxPoints[i]);
			if ( viewPoint.x >= hiX || i == 0){
				highestXid = i;
				hiX = viewPoint.x;
			}
		}
		
		int lowestXid = -1;
		float lowX = 1.0f;
		for ( int i = 0; i < boundingBoxPoints.Length; i++ ){
			Vector3 viewPoint = cam.WorldToViewportPoint(boundingBoxPoints[i]);
			if ( viewPoint.x <= lowX || i == 0){
				lowestXid = i;
				lowX = viewPoint.x;
			}
		}
		
		float width = hiX - lowX;
		
		// now, we know which value is higher. move the camera closer until the IDs associated with this value are offscreen.
		Vector3 direction = cam.transform.position - lookAt;
		direction.Normalize();
		direction *= 0.5f;
		
		int inc = 0;
		if ( width > height ){
			while ( cam.WorldToViewportPoint(boundingBoxPoints[highestXid]).x < 1.0f &&
				cam.WorldToViewportPoint(boundingBoxPoints[lowestXid]).x > 0.0f ){
				cam.transform.position = cam.transform.position - direction;
				inc++;
				if ( inc > 200 ){
					Debug.LogError("ERROR! Unable to generate image for " + target.name );
					return false;
				}
			}
		} else {
			while ( cam.WorldToViewportPoint(boundingBoxPoints[highestYid]).y < 1.0f &&
				cam.WorldToViewportPoint(boundingBoxPoints[lowestYid]).y > 0.0f ){
				cam.transform.position = cam.transform.position - direction;
				inc++;
				if ( inc > 200 ){
					Debug.LogError("ERROR! Unable to generate image for " + target.name );
					return false;
				}
			}
		}
		
		// move back several steps.
		cam.transform.position = cam.transform.position + ( direction * inc/80 );
		return true;
	}
	*/
	
	/*
	public static float FrustumHeightAtDistance( float fov, float distance ) {
		return 2.0f * distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
	}
	
	public static float DistanceForFrustumHeight( float fov, float frustumHeight ){
		return frustumHeight/(2.0f * Mathf.Tan (fov * 0.5f * Mathf.Deg2Rad));
	}
	
	public static float EaseInEaseOut( float x ){
		return ( 1 - Mathf.Sin (Mathf.PI/2 + x * Mathf.PI) )/2;
	}
	*/
	
	public static void SmartDestroy( GameObject obj ){
		if ( Application.isPlaying ){
			Destroy( obj );	
		} else {
			DestroyImmediate( obj );
		}
	}
	
	/*
	private static string focus = "";
	public static float FloatParseTextField( Rect layoutInfo, string controlName, ref string inputString, string defaultString, float clampMin, float clampMax ){
		focus = GUI.GetNameOfFocusedControl();
		
		GUI.SetNextControlName( controlName );
		if ( focus == controlName ){
			inputString = GUI.TextField( layoutInfo, inputString );	
		} else {
			inputString = GUI.TextField( layoutInfo, defaultString );	
		}
		
		float result;
		float.TryParse( inputString, out result );
		
		result = Mathf.Clamp (result, clampMin, clampMax);
		return result;
	}
	
	public static Rect ClampRectToScreen( Rect rect ){
		rect.x = Mathf.Clamp(rect.x,0,Screen.width-rect.width);
		rect.y = Mathf.Clamp(rect.y,0,Screen.height-rect.height);
		return rect;
//		GUI.Window ( id, clientRect, func, GUIContent );
	}
	*/
	
	public static float LerpFloat( float start, float to, float t ){
		return start + (t * (to - start));
	}
	
	public static float GetMaxFloat( params float[] numbers ){
		float currentMax = numbers[0];
		
		for ( int i = 0; i < numbers.Length; i++ ){
			if ( numbers[i] >= currentMax ){
				currentMax = numbers[i];	
			}
		}
		
		return currentMax;
	}
	
	/*
	public static bool IsValidEmail(string email){
	    try {
	        System.Net.Mail.MailAddress addr = new System.Net.Mail.MailAddress(email);
			Debug.Log ( addr.Address );
	        return true;
	    }
	    catch {
	        return false;
	    }
	}
	
	public static bool dragEnabled = true;
	public static void DragWindow(){
		// make the window draggable.
		if ( Event.current.button == 0 && dragEnabled ) {
			GUI.DragWindow (new Rect (0,0, 10000, 30.0f));
		}	
	}
	
	public static Rect CenterWindow( Rect prevRect ){
		return new Rect( Screen.width/2.0f - prevRect.width/2.0f, Screen.height/2.0f - prevRect.height/2.0f, prevRect.width, prevRect.height );	
	}
	*/
	
	public static float GetAxisNumber(){
		float axisNumber = Input.GetAxis ("Mouse ScrollWheel");
		
		axisNumber += (Input.GetKey (KeyCode.UpArrow) ? 0.1f : 0.0f);
		axisNumber += (Input.GetKey (KeyCode.RightArrow) ? 0.1f : 0.0f);
		axisNumber -= (Input.GetKey (KeyCode.LeftArrow) ? 0.1f : 0.0f);
		axisNumber -= (Input.GetKey (KeyCode.DownArrow) ? 0.1f : 0.0f);
		
		return axisNumber;
	}
	
	public static string GetParentDirectoryPath( string path ){
		path = path.Replace( "\\", "/" );
		
		string[] split = path.Split( new char[]{'/'} );
		
		string parentDirectory = "";
		if ( split.Length > 1 ){
			for ( int i = 0; i < split.Length - 1; i++ ){
				if ( split[i] != "" ) {
					parentDirectory += split[i] + "/";
				}
			}
			parentDirectory = parentDirectory.TrimEnd(new char[]{'/'});
		}
		
		return parentDirectory;
	}
	
	
	public static string GetHomeFolder(){
		if ( Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ){
			return GetParentDirectoryPath( Application.dataPath );
		} else if ( Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxPlayer ){
			return "/" + GetParentDirectoryPath( Application.dataPath );
		} else if ( Application.platform == RuntimePlatform.OSXPlayer ){
			return "/" + GetParentDirectoryPath( GetParentDirectoryPath( Application.dataPath ) );
		}
		return "";
	}
	
	public static string GetUserDataHomeFolder(){
		if ( GameObject.Find ("Main") && GameObject.Find("Main").GetComponent<Main>().steamMode ){
			// in steam mode, use the install directory for user data.
			return GetHomeFolder();
		} else {
			if ( Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ){
				string result = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/Axis Game Factory";
				return result.Replace("\\", "/");
			} else if ( Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer ) {
				return "/Users/Shared/Axis Game Factory";
			} else if ( Application.platform == RuntimePlatform.LinuxPlayer ){
				return System.Environment.GetEnvironmentVariable("HOME") + "/Documents/Axis Game Factory";
			}
		}
		return "";
	}
	
	private void OnApplicationQuit(){
		
	}
	public static Bounds GetBoundsRecursively( Transform t ){
		Bounds result;
		
		// if this transform has a bounds, use that.
		if ( t.GetComponent<Renderer>() ){
			result = t.GetComponent<Renderer>().bounds;
		} else {
			// otherwise, we'll need the bounds of the first child.
			List<Transform> renderers = new List<Transform>();
			GetTransformsWithComponentRecursively(t, "Renderer", ref renderers );	
			
			if(renderers.Count > 0)
				result = renderers[0].GetComponent<Renderer>().bounds;
			else{
				t.gameObject.AddComponent<MeshRenderer>();
				result = t.GetComponent<Renderer>().bounds;
			}
		}
		
		_GetBoundsRecursively( t, ref result );
		
		return result;
	}
	
	private static void _GetBoundsRecursively( Transform t, ref Bounds bounds ){
		foreach (Transform child in t){
			if ( t.transform == child ) {
				continue;
			}
			_GetBoundsRecursively( child, ref bounds );
		}
		if ( t.GetComponent<Renderer>() ){
			if ( bounds == null ){
				bounds = t.GetComponent<Renderer>().bounds;	
			}
			bounds.Encapsulate(t.GetComponent<Renderer>().bounds);
		}
	}

}
