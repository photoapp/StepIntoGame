using UnityEngine;
using System.Collections;

public class AGFEventObj : CustomEvent {
	// event types
	public static string SceneLoaded				= "agf_event_01";
	public static string ModelLoaded				= "agf_event_02";
	public static string AssetBundleLoaded			= "agf_event_03";
	public static string SceneLoadFailed			= "agf_event_04";
	
	public static string SetStartPosAndRot			= "agf_event_05";
	public static string PickupCollected			= "agf_event_06";
	
	public AGFEventObj(string eventType = "") {
       type = eventType;
	}
}
