using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour {

	//TODO: 
	// Fix stability


	// the mass of the particle
	public float mass;

	// the velocity of the particle
	public Vector3 vel;

	// the acceleration of the particle
	private Vector3 acc;

	// the old position
	public Vector3 lastPos;

	// the current position
	public Vector3 nextPos;

	// pressure 

	private float pressure;
	// force due to pressure
	[SerializeField]
	private Vector3 fpr;

	// force due to viscosity
	[SerializeField]
	private Vector3 fvis;

	// viscosity coefficient
	[SerializeField]
	private float visc_coeff;

	// density
	protected float density;

	// array of all particles
	public GameObject[] particles;
	// array of all neighbours
	public List<GameObject> neighbours;

	[Range(1,4)]
	[SerializeField]
	private int gridL;
	[Range(1,4)]
	[SerializeField]
	private int gridB;
	[Range(1,4)]
	[SerializeField]
	private int gridH;

	/*public int gridx;
	public int gridy;
	public int gridz;*/

	public Vector3 gridPos;

	//to keep track of collision time
	private int collTime;

	// God script (to access gridLoc)
	public Creator god;

	// Gas constant
	[SerializeField]
	private float k_gas;

	const float KRAD = 2f;
	const float KRAD3 = KRAD * KRAD * KRAD;
	const float KRAD6 = KRAD3 * KRAD3;
	const float KRAD9 = KRAD3 * KRAD6;

	private Vector3 GRAVITY = new Vector3(0f,-9.8f,0f);

	const float POLY6 = 315.0f / (64.0f * Mathf.PI * KRAD9);
	const float SPIKY = -45.0f / (Mathf.PI * KRAD6);

	// if particles array has been set
	private bool setParts;

	// the timestep
	[SerializeField]
	private float timestep;

	[SerializeField]
	private float restDensity;

	// number of chunks the current frame is divided into
	private int numchunks;
	private float elapsedTime;
	// leftover time
	private float extraTime;
	// timestep squared
	private float tssq;

	// minimum & maximum distance to be maintained in between the particles
	[SerializeField]
	private float minDist;
	[SerializeField]
	private float maxDist;

	// repulsion offset
	Vector3 repulsiveOffset = Vector3.zero;

	// collision offset
	public Vector3 collisionOffset = Vector3.zero;

	void Start ()
	{
		god = GameObject.Find ("God").GetComponent<Creator> ();
		nextPos = lastPos = transform.position;
		vel = Vector3.zero;
		elapsedTime = 0;
		extraTime = 0;
		pressure = 0;
		if (mass == 0)
			mass = 1;
		tssq = timestep * timestep;
		acc = Vector3.zero;
		pressure = 0;
		density = 0;
	}

	void FixedUpdate () 
	{
		elapsedTime = Time.deltaTime;
		elapsedTime += extraTime;
		particles = god.particles;
		numchunks = (int) Mathf.Floor(elapsedTime / timestep);
		extraTime = elapsedTime - timestep * numchunks;
		List<Particle> lParts = new List<Particle>();

		for (int i = 0; i < numchunks; i++) 
		{
			density = 0;
			fvis = fpr = Vector3.zero;
			// calc grid co-ords
			spatialHash ();

			lParts = NeighbourParticles (gridPos);

			// calc density and repulsion/attraction
			foreach (var part in lParts) 
			{
				if (part) 
				{
					density += part.mass * wpoly6 (part.gameObject);
				}
			}

			// calc pressure
			pressure = k_gas * (density - restDensity);

			// calc force due to pressure and viscosity
			foreach (var nei in lParts) 
			{
				if(nei)
				{
					fpr -= nei.mass * gradwspiky (nei.gameObject) * (nei.pressure + pressure) / (2 * nei.density);
					fvis += nei.mass * wvis (nei.gameObject) * (nei.vel - vel) / nei.density;
					fvis *= visc_coeff;
				}
			}

			acc = GRAVITY;
			// sometimes, particles might not have spawned on time for fpr or fvis to be added
			if(!float.IsNaN(fpr.x) && !float.IsNaN(fpr.y) && !float.IsNaN(fpr.z))
				acc += fpr / density;
			if(!float.IsNaN(fvis.x) && !float.IsNaN(fvis.y) && !float.IsNaN(fvis.z))
				acc += fvis / density;


			vel = transform.position - lastPos;
			//Damping the Velocity
			vel *= 0.99f;
			nextPos = transform.position + vel + 0.5f * acc * tssq;// - repulsiveOffset * 0.9f;
			lastPos = transform.position;
			transform.position = nextPos;
			acc = Vector3.zero;


			if (transform.position.y < 0) 
			{
				transform.position = new Vector3 (transform.position.x, 0, transform.position.z);
			}

		}

		repulsiveOffset = Vector3.zero;

		// push away particles that are too close
		foreach (var part in lParts) 
		{
				Vector3 diff = part.transform.position - transform.position;
				float md2 = minDist * minDist;
				float mD2 = maxDist * maxDist;
				float dot = diff.sqrMagnitude;
				if (dot < md2) 
				{	//transform.position -= diff * 4 * (md2 - dot);
					repulsiveOffset += 0.5f * diff.normalized * (md2 - dot);
					
				}
				/*else if (dot < mD2)
				{
					repulsiveOffset -= diff.normalized/256;
				}*/

		}
			
		transform.position -= repulsiveOffset ;
				
	}


	// to handle collisions (runs after onCollisionXXX)
	/*void LateUpdate()
	{
		//Debug.DrawRay (transform.position, collisionOffset * 10f, Color.red, 2f);
		transform.position += collisionOffset;
		//lastPos = transform.position;
		collisionOffset = Vector3.zero;
	}*/

	// to calculate grid co-ords based on actual position - done once every frame
	void spatialHash()
	{
		if (god.gridLoc.ContainsKey (gridPos)) 
		{
			god.gridLoc [gridPos].Remove (this);
			if (god.gridLoc [gridPos].Count == 0)
				god.gridLoc.Remove (gridPos);
		}
		int tx = (transform.position.x < 0) ? (int)transform.position.x - 2 : (int)transform.position.x;
		int ty = (transform.position.y < 0) ? (int)transform.position.y - 2 : (int)transform.position.y;
		int tz = (transform.position.z < 0) ? (int)transform.position.z - 2 : (int)transform.position.z;
		gridPos.x = tx / gridL;
		gridPos.y = ty / gridH;
		gridPos.z = tz / gridB;

		if (!god.gridLoc.ContainsKey (gridPos))
			god.gridLoc.Add (gridPos, new List<Particle>());
		god.gridLoc [gridPos].Add (this);

	}


	List<Particle> NeighbourParticles(Vector3 pos)
	{
		List<Particle> lp = new List<Particle> ();
		addParticles (pos, ref lp);
		addParticles (Right (pos), ref lp);
		addParticles (Left (pos), ref lp);
		addParticles (Up (pos), ref lp);
		addParticles (Down (pos), ref lp);
		addParticles (Front (pos), ref lp);
		addParticles (Back (pos), ref lp);
		return lp;
	}

	void addParticles(Vector3 pos, ref List<Particle> lp)
	{
		
		if (god.gridLoc.ContainsKey (pos)) 
		{
			foreach (var part in god.gridLoc[pos]) 
			{
				lp.Add (part);
			}
		}
	}
		

	float wpoly6(GameObject part)
	{
		Vector3 temp = transform.position - part.transform.position;
		float r2 = Vector3.Dot (temp, temp);
		float h2r2 = ((KRAD * KRAD) - (r2));
		return (h2r2>0) ? POLY6 * h2r2 * h2r2 * h2r2 : 0;
	}

	Vector3 gradwspiky(GameObject part)
	{
		float r = (transform.position - part.transform.position).magnitude;
		if (r > KRAD)
			return Vector3.zero;
		float hr = KRAD - r;
		return SPIKY * hr * hr * (transform.position - part.transform.position).normalized;
	}

	float wvis(GameObject part)
	{
		float r = (transform.position - part.transform.position).magnitude;
		if (r > KRAD)
			return 0;
		float hr = KRAD - r;
		return -SPIKY * hr;
	}

	void OnCollisionEnter(Collision coll)
	{
		if (!coll.gameObject.GetComponent<Particle> ()) 
		{
			transform.position += 0.15f * coll.contacts [0].normal.normalized;
			//Debug.DrawRay (coll.contacts[0].point, coll.contacts [0].normal * 10, Color.green, 2f);
			/*vel = 0.0000001f * (coll.contacts[0].point - lastPos);
			vel -= 2 * (Vector3.Dot (vel, coll.contacts [0].normal)) * coll.contacts [0].normal;
			collisionOffset += 0.0000001f * vel;*/
		}
	}

	/*void OnCollisionStay(Collision coll)
	{
		
		if (!coll.gameObject.GetComponent<Particle> ()) 
		{
			transform.position += 0.01f * coll.contacts [0].normal.normalized;
			//Debug.DrawRay (coll.contacts[0].point, coll.contacts [0].normal * 10, Color.green, 2f);
			vel = transform.position - lastPos;
			vel = vel - 2 * (Vector3.Dot (vel, coll.contacts [0].normal)) * coll.contacts [0].normal.normalized;
			collisionOffset += vel;
		}
	}*/

	public void AddForce(Vector3 force)
	{
		acc += force / mass;
		//transform.position += acc * tssq;
		//lastPos = transform.position;
	}

	Vector3 Right(Vector3 pos)
	{
		pos.x += gridL;
		return pos;
	}

	Vector3 Left(Vector3 pos)
	{
		pos.x -= gridL;
		return pos;
	}

	Vector3 Front(Vector3 pos)
	{
		pos.z += gridB;
		return pos;
	}

	Vector3 Back(Vector3 pos)
	{
		pos.z -= gridB;
		return pos;
	}

	Vector3 Up(Vector3 pos)
	{
		pos.y += gridH;
		return pos;
	}

	Vector3 Down(Vector3 pos)
	{
		pos.y -= gridH;
		return pos;
	}

}
