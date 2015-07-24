//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18449
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using UnityEngine;
namespace BahaTurret
{
	public struct TargetSignatureData
	{
		public Vector3 velocity;
		public Vector3 geoPos;
		public Vector3 acceleration;
		public bool exists;
		public float timeAcquired;

		public float signalStrength;

		public TargetSignatureData(Vessel v, float _signalStrength)
		{
			velocity = v.srf_velocity;
			geoPos =  VectorUtils.WorldPositionToGeoCoords(v.CoM, v.mainBody);
			acceleration = v.acceleration;
			exists = true;
			timeAcquired = Time.time;
			signalStrength = _signalStrength;
		}

		public TargetSignatureData(CMFlare flare, float _signalStrength)
		{
			velocity = flare.velocity;
			geoPos =  VectorUtils.WorldPositionToGeoCoords(flare.transform.position, FlightGlobals.currentMainBody);
			exists = true;
			acceleration = Vector3.zero;
			timeAcquired = Time.time;
			signalStrength = _signalStrength;
		}

		public TargetSignatureData(Vector3 _velocity, Vector3 _position, Vector3 _acceleration, bool _exists, float _signalStrength)
		{
			velocity = _velocity;
			geoPos =  VectorUtils.WorldPositionToGeoCoords(_position, FlightGlobals.currentMainBody);
			acceleration = _acceleration;
			exists = _exists;
			timeAcquired = Time.time;
			signalStrength = _signalStrength;
		}

		public Vector3 position
		{
			get
			{
				return FlightGlobals.currentMainBody.GetWorldSurfacePosition(geoPos.x, geoPos.y, geoPos.z);
			}
		}

		public Vector3 predictedPosition
		{
			get
			{
				return position + (velocity * (Time.time-timeAcquired));
			}
		}

		public float altitude
		{
			get
			{
				return geoPos.z;
			}
		}

		public static TargetSignatureData noTarget
		{
			get
			{
				return new TargetSignatureData(Vector3.zero, Vector3.zero, Vector3.zero, false, 0);
			}
		}

		public static void ResetTSDArray(ref TargetSignatureData[] tsdArray)
		{
			for(int i = 0; i < tsdArray.Length; i++)
			{
				tsdArray[i] = TargetSignatureData.noTarget;
			}
		}
	}
}

