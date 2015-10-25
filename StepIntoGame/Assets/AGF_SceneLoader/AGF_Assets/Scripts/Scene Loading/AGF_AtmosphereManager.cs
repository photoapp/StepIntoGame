using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AGF_AtmosphereManager : MonoBehaviour {
	
	// Sub-states, dictating the current appearance of the atmosphere.
	public enum SubState{
		Init, On, Off, Underwater, Build, Design,
	}
	private SubState m_CurrentSubState;
	
	// inspector values.
	public Flare[] lightFlares;
	public Transform lightKit;
	public Color offClearColor = new Color(76.0f/255.0f, 83.0f/255.0f, 81.0f/255.0f, 255.0f/255.0f); 
	
	private Dictionary<string,Dictionary<string,Texture2D>> loadedSkyboxes;
	
	// Properties Classes
	public abstract class AtmosphereProperties{
		public bool hasChanged;
		protected bool _active;
		protected bool _underwaterActive;
		protected AGF_AtmosphereManager m_AtmosphereManager;
		
		public bool active{
			get { return _active; }
			set { if ( value != _active ) { hasChanged = true; _active = value; } }
		}
		
		public bool underwaterActive{
			get { return _underwaterActive; }
			set { if ( value != _underwaterActive ) { hasChanged = true; _underwaterActive = value; } }
		}
		
		public AtmosphereProperties(){
			m_AtmosphereManager = GameObject.Find ("AGF_AtmosphereManager").GetComponent<AGF_AtmosphereManager>();	
		}
		
		public abstract void UpdateOnMode();
		public abstract void UpdateOffMode();
		public abstract void UpdateUnderwaterMode();
	}
	
	public class SkyboxProperties : AtmosphereProperties{
		private Color _tint;
		private bool _autoRotate;
		private float _autoRotationSpeed;
		private float _rotation;
		private string _skyboxID;
		private Color _clearColor;
		private Color _offClearColor;
		private Color _underwaterClearColor;
		
		public Color tint{
			get { return _tint; }
			set { if ( value != _tint ) { _tint = value; } }
		}
		public float rotation{
			get { return _rotation; }
			set { if ( value != _rotation ) { _rotation = value; } }
		}
		public bool autoRotate{
			get { return _autoRotate; }
			set { if ( value != _autoRotate ) { _autoRotate = value; } }
		}
		public float autoRotationSpeed{
			get { return _autoRotationSpeed; }
			set { if ( value != _autoRotationSpeed ) { _autoRotationSpeed = value; } }
		}
		public string skyboxID{
			get { return _skyboxID; }
			set { if ( value != _skyboxID ) { hasChanged = true; _skyboxID = value; } }
		}
		public Color clearColor{
			get { return _clearColor; }
			set { if ( value != _clearColor ) { _clearColor = value; } }
		}
		public Color offClearColor{
			get { return _offClearColor; }
			set { if ( value != _offClearColor ) { _offClearColor = value; } }
		}
		public Color underwaterClearColor{
			get { return _underwaterClearColor; }
			set { if ( value != _underwaterClearColor ) { _underwaterClearColor = value; } }
		}
		
		private AGF_TerrainManager m_TerrainManager;
		private AGF_LevelLoader m_SaveDataManager;
		private AGF_CameraManager m_CameraManager;
		
		public SkyboxProperties() : base() {
			// store references
			m_TerrainManager = GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>();
			m_SaveDataManager = GameObject.Find ("AGF_LevelLoader").GetComponent<AGF_LevelLoader>();
			m_CameraManager = GameObject.Find ("AGF_CameraManager").GetComponent<AGF_CameraManager>();
		}
		
		public override void UpdateOnMode(){
			// For skyboxes, the hasChanged flag only watches the skyboxID and active flag.
			if ( hasChanged ){
				if ( _active ){
					string[] split = _skyboxID.Split( new char[]{'/'} );
					m_CameraManager.GetMainCamera().GetComponent<Skybox>().material = (Material)m_SaveDataManager.LoadAssetFromBundle( split[0], split[1] + ".mat" );
					print( m_CameraManager.GetMainCamera().GetComponent<Skybox>().material );
					m_CameraManager.GetMainCamera().GetComponent<Skybox>().material.shader = Shader.Find ("AGF/Skybox Cubed Rotate");
					print( m_CameraManager.GetMainCamera().GetComponent<Skybox>().material.shader );
				} else {
					m_CameraManager.GetMainCamera().GetComponent<Skybox>().material = null;
				}
				Resources.UnloadUnusedAssets();
			}
			
			// everything else updates each frame.
			if ( _active ){
				if ( _autoRotate ) _rotation += _autoRotationSpeed;
				Quaternion rot = Quaternion.Euler (0f, _rotation, 0f);
		        Matrix4x4 m = Matrix4x4.TRS (Vector3.zero, rot, new Vector3(1,1,1) );
				m_CameraManager.GetMainCamera().GetComponent<Skybox>().material.SetMatrix("_Rotation", m);
				m_CameraManager.GetMainCamera().GetComponent<Skybox>().material.SetColor("_Tint", _tint );
			} else {
				m_CameraManager.GetMainCamera().backgroundColor = _clearColor;
				m_TerrainManager.SetWaterClearColor( _clearColor );
			}
			
			hasChanged = false;
		}
		
		public override void UpdateOffMode(){
			// when the atmosphere is off, the skybox will only display a default clear color.
			if ( hasChanged ){
				m_CameraManager.GetMainCamera().GetComponent<Skybox>().material = null;
				Resources.UnloadUnusedAssets();
			}
			
			m_CameraManager.GetMainCamera().backgroundColor = _offClearColor;
			m_TerrainManager.SetWaterClearColor( _offClearColor );
			
			hasChanged = false;
		}
		
		public override void UpdateUnderwaterMode(){
			// while underwater, only display the underwater clear color.
			if ( hasChanged ){
				m_CameraManager.GetMainCamera().GetComponent<Skybox>().material = null;
				Resources.UnloadUnusedAssets();
			}
		}
	}
	
	public class HazeProperties : AtmosphereProperties{
		private bool _distanceMode;
		private float _density;
		private float _startDist;
		private float _height;
		private float _falloff;
		private Color _tint;
		
		private bool _underwaterDistanceMode;
		private float _underwaterDensity;
		private float _underwaterStartDist;
		private float _underwaterHeight;
		private float _underwaterFalloff;
		private Color _underwaterTint;
		
		public bool distanceMode{
			get { return _distanceMode; }
			set { if ( value != _distanceMode ) { hasChanged = true; _distanceMode = value; } }
		}
		public float density{
			get { return _density; }
			set { if ( value != _density ) { hasChanged = true; _density = value; } }
		}
		public float startDist{
			get { return _startDist; }
			set { if ( value != _startDist ) { hasChanged = true; _startDist = value; } }
		}
		public float height{
			get { return _height; }
			set { if ( value != _height ) { hasChanged = true; _height = value; } }
		}
		public float falloff{
			get { return _falloff; }
			set { if ( value != _falloff ) { hasChanged = true; _falloff = value; } }
		}
		public Color tint{
			get { return _tint; }
			set { if ( value != _tint ) { hasChanged = true; _tint = value; } }
		}
		
		public bool underwaterDistanceMode{
			get { return _underwaterDistanceMode; }
			set { if ( value != _underwaterDistanceMode ) { hasChanged = true; _underwaterDistanceMode = value; } }
		}
		public float underwaterDensity{
			get { return _underwaterDensity; }
			set { if ( value != _underwaterDensity ) { hasChanged = true; _underwaterDensity = value; } }
		}
		public float underwaterStartDist{
			get { return _underwaterStartDist; }
			set { if ( value != _underwaterStartDist ) { hasChanged = true; _underwaterStartDist = value; } }
		}
		public float underwaterHeight{
			get { return _underwaterHeight; }
			set { if ( value != _underwaterHeight ) { hasChanged = true; _underwaterHeight = value; } }
		}
		public float underwaterFalloff{
			get { return _underwaterFalloff; }
			set { if ( value != _underwaterFalloff ) { hasChanged = true; _underwaterFalloff = value; } }
		}
		public Color underwaterTint{
			get { return _underwaterTint; }
			set { if ( value != _underwaterTint ) { hasChanged = true; _underwaterTint = value; } }
		}
		
		private GlobalFog m_GlobalFog;
		
		public HazeProperties() : base(){
			m_GlobalFog = GameObject.Find("AGF_CameraManager").GetComponent<AGF_CameraManager>().GetMainCamera().GetComponent<GlobalFog>();
		}
		
		public override void UpdateOnMode(){
			if ( hasChanged ){
				m_GlobalFog.enabled = _active;
				m_GlobalFog.fogMode = _distanceMode ? GlobalFog.FogMode.AbsoluteYAndDistance : GlobalFog.FogMode.AbsoluteY;
				m_GlobalFog.startDistance = _startDist;
				m_GlobalFog.globalDensity = _density;
				m_GlobalFog.heightScale = _falloff;
				m_GlobalFog.height = _height;
				m_GlobalFog.globalFogColor = _tint;
			}
			
			if ( m_AtmosphereManager.IsFogVisible() == false ){
				m_GlobalFog.enabled = false;
			}
			
			hasChanged = false;
		}
		
		public override void UpdateOffMode(){
			m_GlobalFog.enabled = false;
		}
		
		public override void UpdateUnderwaterMode(){
			if ( hasChanged ){
				m_GlobalFog.enabled = _underwaterActive;
				m_GlobalFog.fogMode = _underwaterDistanceMode ? GlobalFog.FogMode.AbsoluteYAndDistance : GlobalFog.FogMode.AbsoluteY;
				m_GlobalFog.startDistance = _underwaterStartDist;
				m_GlobalFog.globalDensity = _underwaterDensity;
				m_GlobalFog.heightScale = _underwaterFalloff;
				m_GlobalFog.height = _underwaterHeight;
				m_GlobalFog.globalFogColor = _underwaterTint;
			}
			
			if ( m_AtmosphereManager.IsFogVisible() == false ){
				m_GlobalFog.enabled = false;	
			}
			
			hasChanged = false;
		}
	}
	
	public class BasicFogProperties : AtmosphereProperties{
		private bool _linearMode;
		private float _startDist;
		private float _endDist;
		private float _density;
		private Color _tint;
		
		private bool _underwaterLinearMode;
		private float _underwaterStartDist;
		private float _underwaterEndDist;
		private float _underwaterDensity;
		private Color _underwaterTint;
		
		public bool linearMode{
			get { return _linearMode; }
			set { if ( value != _linearMode ) { hasChanged = true; _linearMode = value; } }
		}
		public float startDist{
			get { return _startDist; }
			set { if ( value != _startDist ) { hasChanged = true; _startDist = value; } }
		}
		public float endDist{
			get { return _endDist; }
			set { if ( value != _endDist ) { hasChanged = true; _endDist = value; } }
		}
		public float density{
			get { return _density; }
			set { if ( value != _density ) { hasChanged = true; _density = value; } }
		}
		public Color tint{
			get { return _tint; }
			set { if ( value != _tint ) { hasChanged = true; _tint = value; } }
		}
		
		public bool underwaterLinearMode{
			get { return _underwaterLinearMode; }
			set { if ( value != _underwaterLinearMode ) { hasChanged = true; _underwaterLinearMode = value; } }
		}
		public float underwaterStartDist{
			get { return _underwaterStartDist; }
			set { if ( value != _underwaterStartDist ) { hasChanged = true; _underwaterStartDist = value; } }
		}
		public float underwaterEndDist{
			get { return _underwaterEndDist; }
			set { if ( value != _underwaterEndDist ) { hasChanged = true; _underwaterEndDist = value; } }
		}
		public float underwaterDensity{
			get { return _underwaterDensity; }
			set { if ( value != _underwaterDensity ) { hasChanged = true; _underwaterDensity = value; } }
		}
		public Color underwaterTint{
			get { return _underwaterTint; }
			set { if ( value != _underwaterTint ) { hasChanged = true; _underwaterTint = value; } }
		}
		
		public override void UpdateOnMode(){
			if ( hasChanged ){
				RenderSettings.fog = _active;
				RenderSettings.fogColor = _tint;
				RenderSettings.fogDensity = _density;
				RenderSettings.fogStartDistance = _startDist;
				RenderSettings.fogEndDistance = _endDist;
				RenderSettings.fogMode = _linearMode ? FogMode.Linear : FogMode.Exponential;
			}
			
			if ( m_AtmosphereManager.IsFogVisible() == false ){
				RenderSettings.fog = false;
			}
			
			hasChanged = false;
		}
		
		public override void UpdateOffMode(){
			RenderSettings.fog = false;
		}
		
		public override void UpdateUnderwaterMode(){
			if ( hasChanged ){
				RenderSettings.fog = _underwaterActive;
				RenderSettings.fogColor = _underwaterTint;
				RenderSettings.fogDensity = _underwaterDensity;
				RenderSettings.fogStartDistance = _underwaterStartDist;
				RenderSettings.fogEndDistance = _underwaterEndDist;
				RenderSettings.fogMode = _underwaterLinearMode ? FogMode.Linear : FogMode.Exponential;
			}
			
			if ( m_AtmosphereManager.IsFogVisible() == false ){
				RenderSettings.fog = false;
			}
			
			hasChanged = false;
		}
	}
	
	public class AmbientLightProperties : AtmosphereProperties{
		private Color _tint;
		private Color _underwaterTint;
		
		public Color tint{
			get { return _tint; }
			set { if ( value != _tint ) { hasChanged = true; _tint = value; } }
		}
		
		public Color underwaterTint{
			get { return _underwaterTint; }
			set { if ( value != _underwaterTint ) { hasChanged = true; _underwaterTint = value; } }
		}
		
		public override void UpdateOnMode(){
			if ( m_AtmosphereManager.IsLightingVisible() && _active ){
				RenderSettings.ambientLight = _tint;
			} else {
				RenderSettings.ambientLight = Color.black;
			}
		}
		
		public override void UpdateOffMode(){
			RenderSettings.ambientLight = Color.black;
		}
		
		public override void UpdateUnderwaterMode(){
			if ( m_AtmosphereManager.IsLightingVisible() && _active ){
				RenderSettings.ambientLight = _underwaterTint;
			} else {
				RenderSettings.ambientLight = Color.black;
			}
		}
	}
	
	public class BasicLightProperties : AtmosphereProperties{
		public Transform lightTransform;
		protected Light lightObject;
		
		protected Color _tint;
		private float _intensity;
		private float _yawAngle;
		private float _pitchAngle;
		
		private Color _underwaterTint;
		private float _underwaterIntensity;
		private float _underwaterYawAngle;
		private float _underwaterPitchAngle;
		
		private bool _offActive;
		private Color _offTint;
		private float _offIntensity;
		protected float _offYawAngle;
		protected float _offPitchAngle;
		
		public Color tint{
			get { return _tint; }
			set { if ( value != _tint ) { hasChanged = true; _tint = value; } }
		}
		public float intensity{
			get { return _intensity; }
			set { if ( value != _intensity ) { hasChanged = true; _intensity = value; } }
		}
		public float yawAngle{
			get { return _yawAngle; }
			set { if ( value != _yawAngle ) { hasChanged = true; _yawAngle = value; } }
		}
		public float pitchAngle{
			get { return _pitchAngle; }
			set { if ( value != _pitchAngle ) { hasChanged = true; _pitchAngle = value; } }
		}
		
		public Color underwaterTint{
			get { return _underwaterTint; }
			set { if ( value != _underwaterTint ) { hasChanged = true; _underwaterTint = value; } }
		}
		public float underwaterIntensity{
			get { return _underwaterIntensity; }
			set { if ( value != _underwaterIntensity ) { hasChanged = true; _underwaterIntensity = value; } }
		}
		public float underwaterYawAngle{
			get { return _underwaterYawAngle; }
			set { if ( value != _underwaterYawAngle ) { hasChanged = true; _underwaterYawAngle = value; } }
		}
		public float underwaterPitchAngle{
			get { return _underwaterPitchAngle; }
			set { if ( value != _underwaterPitchAngle ) { hasChanged = true; _underwaterPitchAngle = value; } }
		}
		
		public bool offActive{
			get { return _offActive; }
			set { if ( value != _offActive ) { hasChanged = true; _offActive = value; } }
		}
		public Color offTint{
			get { return _offTint; }
			set { if ( value != _offTint ) { hasChanged = true; _offTint = value; } }
		}
		public float offIntensity{
			get { return _offIntensity; }
			set { if ( value != _offIntensity ) { hasChanged = true; _offIntensity = value; } }
		}
		public float offYawAngle{
			get { return _offYawAngle; }
			set { if ( value != _offYawAngle ) { hasChanged = true; _offYawAngle = value; } }
		}
		public float offPitchAngle{
			get { return _offPitchAngle; }
			set { if ( value != _offPitchAngle ) { hasChanged = true; _offPitchAngle = value; } }
		}
		
		public BasicLightProperties( Transform newTransform ) : base(){
			lightTransform = newTransform;
			lightObject = lightTransform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Light>();
		}
		
		public override void UpdateOnMode(){
			if ( hasChanged ){
				lightObject.enabled = _active;
				lightObject.intensity = _intensity;
				lightObject.color = _tint;
				
				// reset global rotation.
				lightTransform.localRotation = Quaternion.identity;
				
				// yaw
				lightTransform.GetChild(0).localRotation = Quaternion.Euler( 0, _yawAngle, 0 );
				
				// pitch
				lightTransform.GetChild(0).GetChild(0).localRotation = Quaternion.Euler( _pitchAngle, 0, 0 );
			}
			
			if ( m_AtmosphereManager.IsLightingVisible() == false ){
				lightObject.enabled = false;
			}
			
			hasChanged = false;
		}
		
		public override void UpdateOffMode(){
			if ( hasChanged ){
				lightObject.enabled = _offActive;
				lightObject.intensity = _offIntensity;
				lightObject.color = _offTint;
				
				// reset global rotation.
				lightTransform.localRotation = Quaternion.identity;
				
				// yaw
				lightTransform.GetChild(0).localRotation = Quaternion.Euler( 0, _offYawAngle, 0 );
				
				// pitch
				lightTransform.GetChild(0).GetChild(0).localRotation = Quaternion.Euler( _offPitchAngle, 0, 0 );
			}
			
			hasChanged = false;
		}
		
		public override void UpdateUnderwaterMode(){
			if ( hasChanged ){
				lightObject.enabled = _underwaterActive;
				lightObject.intensity = _underwaterIntensity;
				lightObject.color = _underwaterTint;
				
				// reset global rotation.
				lightTransform.localRotation = Quaternion.identity;
				
				// yaw
				lightTransform.GetChild(0).localRotation = Quaternion.Euler( 0, _underwaterYawAngle, 0 );
				
				// pitch
				lightTransform.GetChild(0).GetChild(0).localRotation = Quaternion.Euler( _underwaterPitchAngle, 0, 0 );
			}
			
			if ( m_AtmosphereManager.IsLightingVisible() == false ){
				lightObject.enabled = false;
			}
			
			hasChanged = false;
		}
	}
	
	public class KeyLightProperties : BasicLightProperties{
		private bool _shadowsActive;
		private float _shadowStrength;
		private bool _lightFlareActive;
		private int _flareID;
		
		private bool _underwaterShadowsActive;
		private float _underwaterShadowStrength;
		private bool _underwaterLightFlareActive;
		private int _underwaterFlareID;
		
		public bool shadowsActive{
			get { return _shadowsActive; }
			set { if ( value != _shadowsActive ) { hasChanged = true; _shadowsActive = value; } }
		}
		public float shadowStrength{
			get { return _shadowStrength; }
			set { if ( value != _shadowStrength ) { hasChanged = true; _shadowStrength = value; } }
		}
		public bool lightFlareActive{
			get { return _lightFlareActive; }
			set { if ( value != _lightFlareActive ) { hasChanged = true; _lightFlareActive = value; } }
		}
		public int flareID{
			get { return _flareID; }
			set { if ( value != _flareID ) { hasChanged = true; _flareID = value; } }
		}
		
		public bool underwaterShadowsActive{
			get { return _underwaterShadowsActive; }
			set { if ( value != _underwaterShadowsActive ) { hasChanged = true; _underwaterShadowsActive = value; } }
		}
		public float underwaterShadowStrength{
			get { return _underwaterShadowStrength; }
			set { if ( value != _underwaterShadowStrength ) { hasChanged = true; _underwaterShadowStrength = value; } }
		}
		public bool underwaterLightFlareActive{
			get { return _underwaterLightFlareActive; }
			set { if ( value != _underwaterLightFlareActive ) { hasChanged = true; _underwaterLightFlareActive = value; } }
		}
		public int underwaterFlareID{
			get { return _underwaterFlareID; }
			set { if ( value != _underwaterFlareID ) { hasChanged = true; _underwaterFlareID = value; } }
		}
		
		private AGF_TerrainManager m_TerrainManager;
		private AGF_CameraManager m_CameraManager;
		
		public KeyLightProperties( Transform newTransform ) : base( newTransform ){
			m_TerrainManager = GameObject.Find ("AGF_TerrainManager").GetComponent<AGF_TerrainManager>();
			m_CameraManager = GameObject.Find("AGF_CameraManager").GetComponent<AGF_CameraManager>();
		}
		
		public override void UpdateOnMode(){
			bool hadChanged = hasChanged;
			
			base.UpdateOnMode();
			
			if ( hadChanged ){
				lightObject.shadows = _shadowsActive ? LightShadows.Soft : LightShadows.None;
				lightObject.shadowStrength = _shadowStrength;
				lightObject.flare = _lightFlareActive ? m_AtmosphereManager.lightFlares[_flareID] : null;
				
				// update the water in the terrain manager.
				if ( _active && m_AtmosphereManager.IsLightingVisible() ){
					m_TerrainManager.SetWaterSpecularDirection( lightObject.transform.TransformDirection(Vector3.forward) );
				} else {
					m_TerrainManager.SetWaterSpecularDirection( new Vector3(80,0,80) );
				}
				
				m_TerrainManager.SetWaterSpecularColor( _tint );
			}
			
		}
		
		public override void UpdateOffMode(){
			_offYawAngle = 0;
			_offPitchAngle = 0;
			
			base.UpdateOffMode();
			
			lightObject.shadows = LightShadows.None;
			lightObject.flare = null;
			
			lightTransform.LookAt( lightTransform.position + m_CameraManager.GetMainCamera().transform.forward );
			
			m_TerrainManager.SetWaterSpecularDirection( new Vector3(80,0,80) );
		}
		
		public override void UpdateUnderwaterMode(){
			base.UpdateUnderwaterMode();
			
			if ( hasChanged ){
				lightObject.shadows = _underwaterShadowsActive ? LightShadows.Soft : LightShadows.None;
				lightObject.shadowStrength = _underwaterShadowStrength;
				lightObject.flare = _underwaterLightFlareActive ? m_AtmosphereManager.lightFlares[_flareID] : null;
				
				// update the water in the terrain manager.
				if ( _active ){
					m_TerrainManager.SetWaterSpecularDirection( lightObject.transform.TransformDirection(Vector3.forward) );
				} else {
					m_TerrainManager.SetWaterSpecularDirection( new Vector3(80,0,80) );
				}
				
				m_TerrainManager.SetWaterSpecularColor( _tint );
			}
		}
	}
	
	// Property class objects
	private SkyboxProperties m_CurrentSkybox;
	private HazeProperties m_CurrentHaze;
	private BasicFogProperties m_CurrentBasicFog;
	private BasicLightProperties m_CurrentFillLight;
	private BasicLightProperties m_CurrentBackLight;
	private KeyLightProperties m_CurrentKeyLight;
	private AmbientLightProperties m_CurrentAmbientLight;
	private AtmosphereProperties[] m_AtmosphereProperties;
	
	// scene object references
	private Transform m_CurrentLightKit;
	
	// individual visibility flags
	private bool m_LightingVisible = false;
	private bool m_FogVisible = false;
	private bool m_AtmosphereVisible = true;
	
	// global light rotation
	private float m_GlobalLightRotation;
	
	private void Start(){
		// initialize all of the property trackers, and add them to the array.
		m_CurrentSkybox = new SkyboxProperties();
		m_CurrentHaze = new HazeProperties();
		m_CurrentBasicFog = new BasicFogProperties();
		m_CurrentFillLight = new BasicLightProperties( lightKit.Find("Fill light") );
		m_CurrentBackLight = new BasicLightProperties( lightKit.Find("Bounce light") );
		m_CurrentKeyLight = new KeyLightProperties( lightKit.Find("Directional light") );
		m_CurrentAmbientLight = new AmbientLightProperties();
		m_CurrentLightKit = GameObject.Find ("MainLightKit").transform;
		
		m_AtmosphereProperties = new AtmosphereProperties[]{ m_CurrentSkybox, m_CurrentHaze, m_CurrentBasicFog, m_CurrentFillLight, m_CurrentBackLight, m_CurrentKeyLight, m_CurrentAmbientLight };
		
		// initialize the texture list.
		loadedSkyboxes = new Dictionary<string, Dictionary<string, Texture2D>>();
		
		// setup the default atmosphere configuration.
		SetupDefaultConfiguration();
		
		// register for events.
//		EventHandler.instance.addEventListener( ComplexEventObj.ChangeGridArea, this.gameObject, "OnChangeGridArea" );
//		EventHandler.instance.addEventListener( BasicEventObj.ToggleThemeRenderSettings, this.gameObject, "OnToggleThemeRenderSettings" );
		
		// set the default state to 'on'.
		m_CurrentSubState = SubState.Init;
		this.ChangeState( SubState.On ); // this process will trigger all "hasChanged" flags to be set to true.
	}
	
	public void EditorInit(){
		// Called while in the editor to initialize this object.
		Start();
	}
	
	private void SetupDefaultConfiguration(){
		// When the atmosphere is off, these values will be applied to the lighting, etc.
		m_CurrentSkybox.offClearColor = offClearColor;
		m_CurrentSkybox.clearColor = offClearColor;
		
		m_CurrentKeyLight.offActive = true;
		m_CurrentKeyLight.offIntensity = 0.8f;
		m_CurrentKeyLight.offTint = Color.white;
	}
	
	private void Update(){
		// update the global light rotation seperately.
		m_CurrentLightKit.rotation = Quaternion.Euler( 0.0f, m_GlobalLightRotation, 0.0f );
		
		if ( m_AtmosphereVisible == false || m_CurrentSubState == SubState.Off ){
			for ( int i = 0; i < m_AtmosphereProperties.Length; i++ ){
				m_AtmosphereProperties[i].UpdateOffMode();	
			}
		} else if ( m_CurrentSubState == SubState.On ){
			for ( int i = 0; i < m_AtmosphereProperties.Length; i++ ){
				m_AtmosphereProperties[i].UpdateOnMode();	
			}
		} else if ( m_CurrentSubState == SubState.Underwater ){
			for ( int i = 0; i < m_AtmosphereProperties.Length; i++ ){
				m_AtmosphereProperties[i].UpdateUnderwaterMode();	
			}
		}
	}
	
	public void EditorUpdate(){
		// Called while in the editor once to prepare all atmos settings.
		Update();
	}
	
	// skybox
	public SkyboxProperties GetCurrentSkybox(){
		return m_CurrentSkybox;	
	}
	
	// fog
	public HazeProperties GetCurrentHaze(){
		return m_CurrentHaze;
	}
	
	public BasicFogProperties GetCurrentBasicFog(){
		return m_CurrentBasicFog;	
	}
	
	// light
	public BasicLightProperties GetCurrentFillLight(){
		return m_CurrentFillLight;	
	}
	
	public BasicLightProperties GetCurrentBackLight(){
		return m_CurrentBackLight;	
	}
	
	public KeyLightProperties GetCurrentKeyLight(){
		return m_CurrentKeyLight;	
	}
	
	public AmbientLightProperties GetCurrentAmbientLight(){
		return m_CurrentAmbientLight;	
	}
	
	public float GetGlobalLightingRotation(){
		return m_GlobalLightRotation;
	}
	
	public void SetGlobalLightingRotation( float newRotation ){
		if ( newRotation != m_GlobalLightRotation ){
			m_CurrentKeyLight.hasChanged = true;
			m_CurrentFillLight.hasChanged = true;
			m_CurrentBackLight.hasChanged = true;
			m_GlobalLightRotation = newRotation;
		}
	}
	
	// visibility checks
	public bool IsLightingVisible(){
		return m_LightingVisible;
	}
	
	public void SetLightingVisible( bool newBool ){
		if ( m_LightingVisible != newBool ){
			m_CurrentKeyLight.hasChanged = true;
			m_CurrentFillLight.hasChanged = true;
			m_CurrentBackLight.hasChanged = true;
		}
		
		m_LightingVisible = newBool;	
	}
	
	public bool IsFogVisible(){
		return m_FogVisible;
	}
	
	public void SetFogVisible( bool newBool ){
		if ( m_FogVisible != newBool ){
			m_CurrentHaze.hasChanged = true;
			m_CurrentBasicFog.hasChanged = true;
		}
		m_FogVisible = newBool;	
	}
	
	public bool IsAtmosphereActive(){
		return m_AtmosphereVisible;
	}
	
	public void SetAtmosphereActive( bool newActive ){
		if ( m_AtmosphereVisible != newActive ){
			for ( int i = 0; i < m_AtmosphereProperties.Length; i++ ){
				m_AtmosphereProperties[i].hasChanged = true;	
			}
		}
		m_AtmosphereVisible = newActive;
	}
	
	public void ChangeState( SubState newState ){
		if ( m_CurrentSubState != newState ){
			for ( int i = 0; i < m_AtmosphereProperties.Length; i++ ){
				m_AtmosphereProperties[i].hasChanged = true;	
			}
		}
		m_CurrentSubState = newState;
	}
	
	public void Reset(){
		for ( int i = 0; i < m_AtmosphereProperties.Length; i++ ){
			m_AtmosphereProperties[i].active = false;
			m_AtmosphereProperties[i].hasChanged = true;
		}
		
		m_CurrentSkybox.clearColor = offClearColor;
		m_AtmosphereVisible = true;	
	}
	
//	public void UpdateCurrentCharacterCubemap(){
//		if ( CharacterManager.GetCurrentCharacter() != null ){
//			if ( m_CurrentSkybox.active ){
////				Main.AttachSkyboxCubemapRecursively( CharacterManager.GetCurrentCharacter(),  );
//			} else {
//				Main.AttachSkyboxCubemapRecursively( CharacterManager.GetCurrentCharacter(), null );
//			}
//		}
//	}
	
	public void AddTextureToList( Texture2D newTex, string bundleName ){
		// determine if this texture is a colormap or normalmap.
		if ( loadedSkyboxes.ContainsKey( bundleName ) == false ){
			loadedSkyboxes.Add ( bundleName, new Dictionary<string,Texture2D>() );	
		}
		string name = newTex.name;
		
		loadedSkyboxes[bundleName].Add ( name, newTex );	
	}
	
	public void UnloadMaterialsFromBundle( string bundleName ){
		if ( loadedSkyboxes.ContainsKey( bundleName ) ){
			loadedSkyboxes.Remove ( bundleName );
		}
		
		Resources.UnloadUnusedAssets();
	}
	
	public void ClearSkyboxMaterialList(){
		loadedSkyboxes.Clear();
		
		Resources.UnloadUnusedAssets();
	}
	
	public Dictionary<string,Texture2D> GetLoadedSkyboxTextures(){
		Dictionary<string,Texture2D> result = new Dictionary<string,Texture2D>();
		foreach ( KeyValuePair<string,Dictionary<string,Texture2D>> bundle in loadedSkyboxes ){
			foreach( KeyValuePair<string,Texture2D> texture in bundle.Value ){
				result.Add ( bundle.Key + "/" + texture.Key, texture.Value );	
			}
		} 
		return result;	
	}
	
	// event callbacks //
//	private void OnChangeGridArea( ComplexEventObj eventObj ){
//		GridManager.GridArea gridArea = (GridManager.GridArea)eventObj.arguments["gridArea"];
//		if ( gridArea == GridManager.GridArea.Build ){
//			this.ChangeState( SubState.On );
//		} else if ( gridArea == GridManager.GridArea.Factory ){
//			this.ChangeState( SubState.Off );
//		}
//	}
	
//	private void OnToggleThemeRenderSettings( BasicEventObj eventObj ){
//		SetAtmosphereActive( !m_AtmosphereVisible );
//	}
	
	// Serialization //
//	public void SetAtmosphereFromString( string serializationString ){
//		string[] split = serializationString.Split (new char[]{'*'});
//		
//		int i = 0;
//		
//		int outInt;
//		int.TryParse( split[i], out outInt );
//		m_AtmosphereVisible = (outInt == 1);
//	}
	
	public void SetLightFromString( string serializationString ){
		string[] split = serializationString.Split (new char[]{'*'});
		
		int i = 0;
		
		// global light rotation
		float globalRotation;
		float.TryParse( split[i], out globalRotation );
		m_GlobalLightRotation = globalRotation;
		
		// directional light
		int outInt;
		int.TryParse( split[++i], out outInt );
		m_CurrentKeyLight.active = (outInt == 1);
		
		// color
		float r,g,b,a;
		float.TryParse( split[++i], out r ); 
		float.TryParse( split[++i], out g ); 
		float.TryParse( split[++i], out b ); 
		float.TryParse( split[++i], out a );
		m_CurrentKeyLight.tint = new Color(r,g,b,a);
		
		// shadows
		int.TryParse( split[++i], out outInt );
		m_CurrentKeyLight.shadowsActive = (outInt == 1);
		float shadowStrength;
		float.TryParse( split[++i], out shadowStrength );
		m_CurrentKeyLight.shadowStrength = shadowStrength;
		
		// flares
		int.TryParse( split[++i], out outInt );
		m_CurrentKeyLight.lightFlareActive = (outInt == 1);
		int flareID;
		int.TryParse( split[++i], out flareID );
		m_CurrentKeyLight.flareID = flareID;
		
		// rotation
		float yawAngle, pitchAngle;
		float.TryParse( split[++i], out yawAngle );
		float.TryParse( split[++i], out pitchAngle );
		m_CurrentKeyLight.yawAngle = yawAngle;
		m_CurrentKeyLight.pitchAngle = pitchAngle;
		
		// intensity
		float intensity;
		float.TryParse( split[++i], out intensity );
		m_CurrentKeyLight.intensity = intensity;
		
		// fill light
		int.TryParse( split[++i], out outInt );
		m_CurrentFillLight.active = (outInt == 1);
		
		// color
		float.TryParse( split[++i], out r ); 
		float.TryParse( split[++i], out g ); 
		float.TryParse( split[++i], out b ); 
		float.TryParse( split[++i], out a );
		m_CurrentFillLight.tint = new Color(r,g,b,a);
		
		// rotation
		float.TryParse( split[++i], out yawAngle );
		float.TryParse( split[++i], out pitchAngle );
		m_CurrentFillLight.yawAngle = yawAngle;
		m_CurrentFillLight.pitchAngle = pitchAngle;
		
		// intensity
		float.TryParse( split[++i], out intensity );
		m_CurrentFillLight.intensity = intensity;
		
		// bounce light
		int.TryParse( split[++i], out outInt );
		m_CurrentBackLight.active = (outInt == 1);
		
		// color
		float.TryParse( split[++i], out r ); 
		float.TryParse( split[++i], out g ); 
		float.TryParse( split[++i], out b ); 
		float.TryParse( split[++i], out a );
		m_CurrentBackLight.tint = new Color(r,g,b,a);
		
		// rotation
		float.TryParse( split[++i], out yawAngle );
		float.TryParse( split[++i], out pitchAngle );
		m_CurrentBackLight.yawAngle = yawAngle;
		m_CurrentBackLight.pitchAngle = pitchAngle;
		
		// intensity
		float.TryParse( split[++i], out intensity );
		m_CurrentBackLight.intensity = intensity;
		
		// ambient light
		int.TryParse( split[++i], out outInt );
		m_CurrentAmbientLight.active = (outInt == 1);
		
		// color
		float.TryParse( split[++i], out r ); 
		float.TryParse( split[++i], out g ); 
		float.TryParse( split[++i], out b ); 
		float.TryParse( split[++i], out a );
		m_CurrentAmbientLight.tint = new Color(r,g,b,a);
		
		// light visibility
		int.TryParse( split[++i], out outInt );
		m_LightingVisible = (outInt == 1);
		
		// set all light-related hasChanged flags.
		m_CurrentBackLight.hasChanged = true;
		m_CurrentFillLight.hasChanged = true;
		m_CurrentKeyLight.hasChanged = true;
	}
	
	public void SetSkyboxFromString( string serializationString ){
		string[] split = serializationString.Split (new char[]{'*'});
		
		int i = 0;
		
		m_CurrentSkybox.skyboxID = split[i];
		
		float rotation;
		float.TryParse( split[++i], out rotation );
		m_CurrentSkybox.rotation = rotation;
		
		int autoRotate;
		int.TryParse( split[++i], out autoRotate );
		m_CurrentSkybox.autoRotate = (autoRotate == 1 );
		
		float autoRotationSpeed;
		float.TryParse( split[++i], out autoRotationSpeed );
		m_CurrentSkybox.autoRotationSpeed = autoRotationSpeed;
		
		int skyboxVisible;
		int.TryParse( split[++i], out skyboxVisible );
		m_CurrentSkybox.active = ( skyboxVisible == 1 );
		
		float r,g,b,a;
		float.TryParse( split[++i], out r );
		float.TryParse( split[++i], out g );
		float.TryParse( split[++i], out b );
		float.TryParse( split[++i], out a );
		m_CurrentSkybox.tint = new Color(r,g,b,a);
		
		float.TryParse( split[++i], out r );
		float.TryParse( split[++i], out g );
		float.TryParse( split[++i], out b );
		float.TryParse( split[++i], out a );
		m_CurrentSkybox.clearColor = new Color(r,g,b,a);
		
		// set all skybox-related hasChanged flags.
		m_CurrentSkybox.hasChanged = true;
	}
	
	public void SetFogFromString( string serializationString ){
		
		string[] split = serializationString.Split (new char[]{'*'});
		
		int i = 0;
		
		m_CurrentHaze.active = ( split[i] == "1" );
		m_CurrentBasicFog.active = ( split[++i] == "1" );
		m_FogVisible = ( split[++i] == "1" );
		
		// get all fog values.
		float fogDensity, startDistance, heightScale, height;
		int fogModeInt;
		int.TryParse( split[++i], out fogModeInt );
		float.TryParse( split[++i], out fogDensity );
		float.TryParse( split[++i], out startDistance );
		float.TryParse( split[++i], out heightScale );
		float.TryParse( split[++i], out height );
		
		float r,g,b,a;
		float.TryParse( split[++i], out r );
		float.TryParse( split[++i], out g );
		float.TryParse( split[++i], out b );
		float.TryParse( split[++i], out a );
		Color fogColor = new Color(r,g,b,a);
		
		float basicFogDensity, basicFogStart, basicFogEnd;
		int basicFogModeInt;
		int.TryParse( split[++i], out basicFogModeInt );
		float.TryParse( split[++i], out basicFogDensity );
		float.TryParse( split[++i], out basicFogStart );
		float.TryParse( split[++i], out basicFogEnd );
		
		float.TryParse( split[++i], out r );
		float.TryParse( split[++i], out g );
		float.TryParse( split[++i], out b );
		float.TryParse( split[++i], out a );
		Color basicFogColor = new Color(r,g,b,a);
		
		// set all fog values.
		m_CurrentHaze.distanceMode = ( fogModeInt == 1 );
		m_CurrentHaze.tint = fogColor;
		m_CurrentHaze.density = fogDensity;
		m_CurrentHaze.startDist = startDistance;
		m_CurrentHaze.falloff = heightScale;
		m_CurrentHaze.height = height;
		
		m_CurrentBasicFog.linearMode = ( basicFogModeInt == 1 );
		m_CurrentBasicFog.tint = basicFogColor;
		m_CurrentBasicFog.density = basicFogDensity;
		m_CurrentBasicFog.startDist = basicFogStart;
		m_CurrentBasicFog.endDist = basicFogEnd;
		
		// set all fog related hasChanged flags.
		m_CurrentBasicFog.hasChanged = true;
		m_CurrentHaze.hasChanged = true;
	}
}
