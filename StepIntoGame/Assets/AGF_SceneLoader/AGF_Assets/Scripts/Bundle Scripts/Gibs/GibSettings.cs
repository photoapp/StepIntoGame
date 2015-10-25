using UnityEngine;
using System.Collections;

public class GibSettings : MonoBehaviour {
	public int maxNumber = 50;
	public bool persistOnDeath = false;
	public float deathTimer = 3.0f;
	public Transform deathEffect;
	public Transform remains;
	[HideInInspector]public string category;
	[HideInInspector]public string bundle;
}
