using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class colliderUpdate : MonoBehaviour {
	
	void LateUpdate () {
		GetComponent<MeshCollider> ().sharedMesh = null;
		print (GetComponent<MeshCollider> ().sharedMesh);
		Mesh mf = GetComponent<MeshFilter> ().mesh;
		GetComponent<MeshCollider> ().sharedMesh = mf;
		print (GetComponent<MeshCollider> ().sharedMesh);

	}
}
