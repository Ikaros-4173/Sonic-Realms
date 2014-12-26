﻿using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
	private bool grounded;
	private float vx, vy, vg;

	// If grounded, the angle of the incline the player is standing on
	private float surfaceAngle;

	private WallMode wallMode;

	/// <summary>
	/// The amount in degrees past the threshold of changing wall mode that the player
	/// can go.
	/// </summary>
	private const float WallModeAngleThreshold = 10.0f;

	/// <summary>
	/// The speed at which the player must be moving to be able to switch wall modes.
	/// </summary>
	private const float WallModeSpeedThreshold = 0.5f;

	/// <summary>
	/// The maximum change in angle between two surfaces that the player can walk in.
	/// </summary>
	private const float SurfaceAngleThreshold = 70.0f;

	// The layer mask which represents all terrain to check for collision with
	private int terrainMask;

	private bool leftKeyDown, rightKeyDown, jumpKeyDown;
	

	[SerializeField]
	private float gravity;

	[SerializeField]
	private float acceleration;

	// Sensors
	[SerializeField]
	private Transform sensorGroundLeft;
	[SerializeField]
	private Transform sensorGroundRight;
	[SerializeField]
	private Transform sensorSideLeft;
	[SerializeField]
	private Transform sensorSideRight;
	[SerializeField]
	private Transform sensorCeilLeft;
	[SerializeField]
	private Transform sensorCeilRight;
	[SerializeField]
	private Transform sensorTileLeft;
	[SerializeField]
	private Transform sensorTileRight;


	// Use this for initialization
	void Start () {
		grounded = false;
		vx = vy = vg = 0.0f;
		leftKeyDown = rightKeyDown = jumpKeyDown = false;
		wallMode = WallMode.Floor;
		surfaceAngle = 0.0f;
		terrainMask = 1 << LayerMask.NameToLayer("Terrain");

		collider2D.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		HandleInput ();
	}


	private void HandleInput()
	{
		leftKeyDown = Input.GetKey (Settings.LeftKey);
		rightKeyDown = Input.GetKey (Settings.RightKey);
		if(!jumpKeyDown) jumpKeyDown = Input.GetKeyDown (Settings.JumpKey);
	}
	

	void FixedUpdate()
	{
		transform.position = new Vector3 (transform.position.x + (vx * Time.fixedDeltaTime), transform.position.y + (vy * Time.fixedDeltaTime));

		bool justLanded = false;

		if(!grounded)
		{
			// See if the player landed
			RaycastHit2D groundCheck = Physics2D.Linecast(sensorGroundLeft.position, sensorGroundRight.position, terrainMask);
			grounded = groundCheck;
			
			if(grounded)
			{
				// If so, set vertical velocity to zero
				vy = 0;
				justLanded = true;
				Debug.DrawLine (sensorGroundLeft.position, sensorGroundRight.position, Color.red);
			} else {
				// Otherwise, apply gravity and keep the player pointing straight down
				transform.eulerAngles = new Vector3();
				vy -= gravity;
				surfaceAngle = 0.0f;

				Debug.DrawLine (sensorGroundLeft.position, sensorGroundRight.position, Color.green);
			}
		}

		if(grounded)
		{
			SurfaceInfo s = GetSurface(terrainMask);
			
			if(s.hit)
			{
				float prevSurfaceAngle = surfaceAngle;
				surfaceAngle = AMath.Modp((s.raycast.normal.Angle() * Mathf.Rad2Deg) - 90.0f, 360.0f);

				// Can only stay on the surface if angle difference is low enough
				if(justLanded || Mathf.Abs(AMath.AngleDiff(prevSurfaceAngle * Mathf.Deg2Rad, surfaceAngle * Mathf.Deg2Rad)) * Mathf.Rad2Deg < SurfaceAngleThreshold)
				{
					justLanded = false;

					// Prevent fluctuating wall mode when standing still due to different sensor angles
					if(Mathf.Abs(vg) > WallModeSpeedThreshold)
					{
						if(wallMode == WallMode.Floor)
						{
							if(surfaceAngle > 45.0f + WallModeAngleThreshold && surfaceAngle < 180.0f) wallMode = WallMode.Right;
							else if(surfaceAngle < 315.0f - WallModeAngleThreshold && surfaceAngle > 180.0f) wallMode = WallMode.Left;
						} else if(wallMode == WallMode.Right)
						{
							if(surfaceAngle > 135.0f + WallModeAngleThreshold) wallMode = WallMode.Ceiling;
							else if(surfaceAngle < 45.0f - WallModeAngleThreshold) wallMode = WallMode.Floor;
						} else if(wallMode == WallMode.Ceiling)
						{
							if(surfaceAngle > 225.0f + WallModeAngleThreshold) wallMode = WallMode.Left;
							else if(surfaceAngle < 135.0f - WallModeAngleThreshold) wallMode = WallMode.Right;
						} else if(wallMode == WallMode.Left)
						{
							if(surfaceAngle > 315.0f + WallModeAngleThreshold || surfaceAngle < 180.0f) wallMode = WallMode.Floor;
							else if(surfaceAngle < 225.0f - WallModeAngleThreshold) wallMode = WallMode.Ceiling;
						}
					}

					// Input
					if(leftKeyDown) vg -= acceleration;
					else if(rightKeyDown) vg += acceleration;
					
					if(s.footing == Footing.Left)
					{
						// Rotate the player to the surface on its left foot
						transform.eulerAngles = new Vector3(0.0f, 0.0f, (s.raycast.normal.Angle() * Mathf.Rad2Deg) - 90.0f);
						
						// Overlap routine - if the player's right foot is submerged, correct the player's rotation
						RaycastHit2D overlapCheck = Physics2D.Linecast(sensorSideRight.position, sensorTileRight.position, terrainMask);
						
						Debug.DrawLine(sensorSideRight.position, sensorTileRight.position, Color.gray);
						
						if(overlapCheck)
						{
							// Correct rotation of the two sensors have similarly oriented surfaces
							if(Mathf.Abs(AMath.AngleDiff(s.raycast.normal, overlapCheck.normal)) * Mathf.Rad2Deg < SurfaceAngleThreshold)
								transform.eulerAngles = new Vector3(0.0f, 0.0f, 
								                                    (overlapCheck.point - s.raycast.point).Angle() * Mathf.Rad2Deg);
							
							Debug.DrawLine(sensorTileRight.position, overlapCheck.point, Color.red);
						}
						
						// Keep the player on the surface
						transform.position += (Vector3)s.raycast.point - sensorGroundLeft.position;
						
						Debug.DrawLine(sensorSideLeft.position, s.raycast.point, Color.green);
						Debug.DrawLine(s.raycast.point, sensorTileLeft.position, Color.red);
						
					} else if(s.footing == Footing.Right)
					{
						// Rotate the player to the surface on its right foot
						transform.eulerAngles = new Vector3(0.0f, 0.0f, (s.raycast.normal.Angle() * Mathf.Rad2Deg) - 90.0f);
						
						// Overlap routine - if the player's left foot is submerged, correct the player's rotation
						RaycastHit2D overlapCheck = Physics2D.Linecast(sensorSideLeft.position, sensorTileLeft.position, terrainMask);
						
						Debug.DrawLine(sensorSideLeft.position, sensorTileLeft.position, Color.gray);
						
						if(overlapCheck) 
						{
							// Correct rotation of the two sensors have similarly oriented surfaces
							if(Mathf.Abs(AMath.AngleDiff(s.raycast.normal, overlapCheck.normal)) * Mathf.Rad2Deg < SurfaceAngleThreshold)
								transform.eulerAngles = new Vector3(0.0f, 0.0f, 
								                                    (s.raycast.point - overlapCheck.point).Angle() * Mathf.Rad2Deg);
							
							Debug.DrawLine(sensorTileLeft.position, overlapCheck.point, Color.red);
						}
						
						// Keep the player on the surface
						transform.position += (Vector3)s.raycast.point - sensorGroundRight.position;
						
						Debug.DrawLine(sensorSideRight.position, s.raycast.point, Color.green);
						Debug.DrawLine(s.raycast.point, sensorTileRight.position, Color.red);
					}

					vx = vg * Mathf.Cos(surfaceAngle * Mathf.Deg2Rad);
					vy = vg * Mathf.Sin(surfaceAngle * Mathf.Deg2Rad);
				} else {
					vg = 0;
					surfaceAngle = 0.0f;
					grounded = false;
				}
            } else {
				vg = 0;
				surfaceAngle = 0.0f;
				grounded = false;
			}
		}

		Camera.main.transform.position = new Vector3 (transform.position.x, transform.position.y, Camera.main.transform.position.z);
	}

	/// <summary>
	/// Gets data about the surface closest to the player's feet, including its footing and raycast info.
	/// </summary>
	/// <returns>The surface.</returns>
	/// <param name="layerMask">A mask indicating what layers are surfaces.</param>
	private SurfaceInfo GetSurface(int layerMask)
	{
		RaycastHit2D checkLeft = Physics2D.Linecast (sensorSideLeft.position, sensorTileLeft.position, layerMask);
		RaycastHit2D checkRight = Physics2D.Linecast (sensorSideRight.position, sensorTileRight.position, layerMask);

		if(checkLeft && checkRight)
		{
			if(wallMode == WallMode.Floor && checkLeft.point.y > checkRight.point.y || 
			   wallMode == WallMode.Ceiling && checkLeft.point.y < checkRight.point.y || 
			   wallMode == WallMode.Right && checkLeft.point.x < checkRight.point.x || 
			   wallMode == WallMode.Left && checkLeft.point.x > checkRight.point.x)
				return new SurfaceInfo(true, checkLeft, Footing.Left);
			else return new SurfaceInfo(true, checkRight, Footing.Right);
		} else if(checkLeft)
		{
			return new SurfaceInfo(true, checkLeft, Footing.Left);
		} else if(checkRight)
		{
			return new SurfaceInfo(true, checkRight, Footing.Right);
		}

		return default(SurfaceInfo);
	}

	/// <summary>
	/// A collection of data about the surface at the player's feet.
	/// </summary>
	private struct SurfaceInfo
	{
		/// <summary>
		/// Whether or not there is a surface.
		/// </summary>
		public bool hit;

		/// <summary>
		/// If there is a surface, which foot of the player it is  beneath. Otherwise, Footing.none.
		/// </summary>
		public Footing footing;

		/// <summary>
		/// The result of the raycast onto the surface at the player's closest foot. This includes the normal
		/// of the surface and its location.
		/// </summary>
		public RaycastHit2D raycast;
		public SurfaceInfo(bool hit, RaycastHit2D raycast, Footing footing)
			{ this.hit = hit; this.raycast = raycast; this.footing = footing; }
	}

	private enum Footing
	{
		None, Left, Right,
	}
}
