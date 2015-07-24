using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class Misc : MonoBehaviour
	{
		
		public static Color ParseColor255(string color)
		{
			Color outputColor = new Color(0,0,0,1);
			
			var strings = color.Split(","[0]);
			for(int i = 0; i < 4; i++)
			{
				outputColor[i] = System.Single.Parse(strings[i])/255;	
			}
			
			return outputColor;
		}
		
		public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

		public static AnimationState SetUpSingleAnimation(string animationName, Part part)
		{
			var states = new List<AnimationState>();

			foreach (var animation in part.FindModelAnimators(animationName))
			{
				var animationState = animation[animationName];
				animationState.speed = 0;
				animationState.enabled = true;
				animationState.wrapMode = WrapMode.ClampForever;
				animation.Blend(animationName);
				return animationState;
			}

			return null;
		}
		
		public static bool CheckMouseIsOnGui()
		{
			Vector3 inverseMousePos = new Vector3(Input.mousePosition.x, Screen.height-Input.mousePosition.y, 0);
			Rect topGui = new Rect(0,0, Screen.width, 65);
			
			return 
			(
				BDArmorySettings.GAME_UI_ENABLED && 
				BDArmorySettings.FIRE_KEY.Contains("mouse") &&
				(
					(BDArmorySettings.toolbarGuiEnabled && BDArmorySettings.Instance.toolbarWindowRect.Contains(inverseMousePos)) 
					|| topGui.Contains(inverseMousePos)
					|| (ModuleTargetingCamera.windowIsOpen && ModuleTargetingCamera.camWindowRect.Contains(inverseMousePos))
					|| (BDArmorySettings.Instance.ActiveWeaponManager!=null && BDArmorySettings.Instance.ActiveWeaponManager.radar!=null && BDArmorySettings.Instance.ActiveWeaponManager.radar.radarEnabled && ModuleRadar.radarWindowRect.Contains(inverseMousePos))
				)
			);	
		}
		
		//Thanks FlowerChild
		//refreshes part action window
		public static void RefreshAssociatedWindows(Part part)
        {
			foreach ( UIPartActionWindow window in FindObjectsOfType( typeof( UIPartActionWindow ) ) ) 
            {
				if ( window.part == part )
                {
                    window.displayDirty = true;
                }
            }
        }

		public static Vector3 ProjectOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
		{
			planeNormal = planeNormal.normalized;
			
			Plane plane = new Plane(planeNormal, planePoint);
			float distance = plane.GetDistanceToPoint(point);
			
			return point - (distance*planeNormal);
		}

		public static float SignedAngle(Vector3 fromDirection, Vector3 toDirection, Vector3 referenceRight)
		{
			float angle = Vector3.Angle(fromDirection, toDirection);
			float sign = Mathf.Sign(Vector3.Dot(toDirection, referenceRight));
			float finalAngle = sign * angle;
			return finalAngle;
		}
		/// <summary>
		/// Parses the string to a curve.
		/// Format: "key:pair,key:pair"
		/// </summary>
		/// <returns>The curve.</returns>
		/// <param name="curveString">Curve string.</param>
		public static FloatCurve ParseCurve(string curveString)
		{
			string[] pairs = curveString.Split(new char[]{','});
			Keyframe[] keys = new Keyframe[pairs.Length]; 
			for(int p = 0; p < pairs.Length; p++)
			{
				string[] pair = pairs[p].Split(new char[]{':'});
				keys[p] = new Keyframe(float.Parse(pair[0]),float.Parse(pair[1]));
			}

			FloatCurve curve = new FloatCurve(keys);

			return curve;
		}

		public static bool CheckSightLine(Vector3 a, Vector3 b, float maxDistance, float threshold)
		{
			float dist = maxDistance;
			Ray ray = new Ray(a, b-a);
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, dist, 557057))
			{
				if((rayHit.point-b).sqrMagnitude < Mathf.Pow(threshold, 2))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			
			return true;
		}

		public static float[] ParseToFloatArray(string floatString)
		{
			string[] floatStrings = floatString.Split(new char[]{','});
			float[] floatArray = new float[floatStrings.Length];
			for(int i = 0; i < floatStrings.Length; i++)
			{
				floatArray[i] = float.Parse(floatStrings[i]);
			}

			return floatArray;
		}


		
	
	}
}

