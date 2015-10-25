using UnityEngine;
using System.Collections;

public class ShaderManager : MonoBehaviour {
	
	
	public Material defaultOpaqueMaterial;
	public Material defaultTransparentMaterial;
	
	[System.Serializable]
	public class Shaders{
		public Material opaque;
		public Material transparent;
	}

	public Shaders[] shaderList;
}
