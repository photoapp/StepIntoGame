using UnityEngine;
using System.Collections;

public class AGF_Decryptor : MonoBehaviour {
	// we cannot decrypt in this version of the tool.
	public virtual bool CanDecrypt(){
		return false;	
	}
	
	public virtual byte[] Decrypt( byte[] encryptedBytes ){
		return encryptedBytes;
	}
}
