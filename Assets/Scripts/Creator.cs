using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creator : MonoBehaviour {


	// create a cube of particles
	// cube length
	[Range(0f,10f)]
	[SerializeField]
	private int cubeL;

	// cube breadth
	[Range(0f,10f)]
	[SerializeField]
	private int cubeB;

	// cube height
	[Range(0f,10f)]
	[SerializeField]
	private int cubeH;

	// Particle prefab
	[SerializeField]
	private GameObject particlePrefab;

	// array to store all particles
	public GameObject[] particles;

	// the parent to store all particles
	[SerializeField]
	private GameObject partParent;

	[System.Serializable]
	public struct GL
	{
		public Vector3 pos;
		public List<Particle> parts;
	}

	public GL[] glStruct;

	public Dictionary <Vector3, List<Particle>> gridLoc; 

	[SerializeField]
	private Vector3 startPos;


	// Use this for initialization
	void Start () {

		gridLoc = new Dictionary< Vector3, List<Particle>>();
		particles = new GameObject[cubeL*cubeB*cubeH];
		for (int c = 0, k = (int) startPos.y; k < (int) startPos.y + cubeH; k++, c++) 
		{
			for (int b = 0, j = (int) startPos.z; j < (int) startPos.z + cubeB; j++, b++) 
			{
				for (int a = 0, i = (int) startPos.x; i < (int) startPos.x + cubeL; i++, a++) 
				{
					GameObject go = Instantiate (particlePrefab, new Vector3 (i, j, k), Quaternion.identity, partParent.transform);
					particles [index(a,b,c)] = go;
					go.GetComponent<Particle> ().particles = particles;

				}
			}
		}
			

	}
	
	// Update is called once per frame
	void Update () {

		/*foreach (var gloc in gridLoc) 
		{
			if (gloc.Value.Count == 0)
				gridLoc.Remove (gloc.Key);
		}

		glStruct = new GL[gridLoc.Count]; 
		int i = 0;


		foreach (var gloc in gridLoc) 
		{
			glStruct [i].pos = gloc.Key;
			glStruct [i++].parts = gloc.Value;
		}*/
	}

	int index(int i, int j, int k)
	{
		return i + cubeB * j + cubeB * cubeH * k;
	}
		
}
