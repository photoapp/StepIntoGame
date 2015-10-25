using UnityEngine;
using System.Collections;

public enum OperationID{
	Add, Delete, Modify, None
}

public struct AGF_TileDataStruct {
	public OperationID operation;
	public string tileID;
	public int instanceID;
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;
	public string customString;
	public int operationGroup;
	
	public AGF_TileDataStruct( OperationID op, string id, int instance, Vector3 pos, Vector3 size, Quaternion rot, int opGroup, string customStr ){
		operation = op;
		tileID = id;
		instanceID = instance; 
		position = pos;
		rotation = rot;
		scale = size;
		customString = customStr;
		operationGroup = opGroup;
	}
	
	public AGF_TileDataStruct( string str ){
		string[] dataString = str.Split(new char[]{ '~'});
		
		int i = 0;
		
		int parsedInt = 0;
		int.TryParse( dataString[i], out parsedInt );
		operation = (OperationID)parsedInt;
		
		int.TryParse( dataString[++i], out parsedInt );
		operationGroup = parsedInt;
		
		tileID = dataString[++i];
		
		int.TryParse( dataString[++i], out parsedInt );
		instanceID = parsedInt;
		
		float parsedFloatx = 0.0f, parsedFloaty = 0.0f, parsedFloatz = 0.0f;
		
		float.TryParse( dataString[++i], out parsedFloatx );
		float.TryParse( dataString[++i], out parsedFloaty );
		float.TryParse( dataString[++i], out parsedFloatz );
		
		position = new Vector3( parsedFloatx, parsedFloaty, parsedFloatz );
		
		float parsedFloatw = 0.0f;
		
		float.TryParse( dataString[++i], out parsedFloatx );
		float.TryParse ( dataString[++i], out parsedFloaty );
		float.TryParse (dataString[++i], out parsedFloatz );
		float.TryParse (dataString[++i], out parsedFloatw );
		
		rotation = new Quaternion( parsedFloatx, parsedFloaty, parsedFloatz, parsedFloatw );
		
		float.TryParse( dataString[++i], out parsedFloatx );
		float.TryParse( dataString[++i], out parsedFloaty );
		float.TryParse( dataString[++i], out parsedFloatz );
		
		scale = new Vector3( parsedFloatx, parsedFloaty, parsedFloatz );
		
		customString = dataString[++i];
	}
	
	public override string ToString(){
		string outString = "";
		
		outString += (int)operation + "~";
		outString += operationGroup.ToString() + "~";
		outString += tileID + "~";
		outString += instanceID.ToString() + "~";
		
		outString += position.x.ToString() + "~";
		outString += position.y.ToString() + "~";
		outString += position.z.ToString() + "~";
		
		outString += rotation.x.ToString() + "~";
		outString += rotation.y.ToString() + "~";
		outString += rotation.z.ToString() + "~";
		outString += rotation.w.ToString() + "~";
		
		outString += scale.x.ToString() + "~";
		outString += scale.y.ToString() + "~";
		outString += scale.z.ToString() + "~";
		
		outString += customString;
		
		return outString;
	}
}
