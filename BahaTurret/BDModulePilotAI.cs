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
	public class BDModulePilotAI : PartModule
	{
		[KSPField(isPersistant = true)]
		public bool pilotEnabled = false;

		bool startedLanded = false;
		bool extending = false;

		Transform velocityTransform;

		Vessel targetVessel;

		Vector3 upDirection = Vector3.up;

		MissileFire wm;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Default Alt."),
		 UI_FloatRange(minValue = 500f, maxValue = 8500f, stepIncrement = 25f, scene = UI_Scene.All)]
		public float defaultAltitude = 1500;
		
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min Altitude"),
		 UI_FloatRange(minValue = 150f, maxValue = 8500, stepIncrement = 10f, scene = UI_Scene.All)]
		public float minAltitude = 900;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steer Factor"),
		 UI_FloatRange(minValue = 0.1f, maxValue = 20f, stepIncrement = .1f, scene = UI_Scene.All)]
		public float steerMult = 12;
		//make a combat steer mult and idle steer mult
		
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steer Limiter"),
		 UI_FloatRange(minValue = .1f, maxValue = 1f, stepIncrement = .05f, scene = UI_Scene.All)]
		public float maxSteer = 1;
		
		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Steer Damping"),
		 UI_FloatRange(minValue = 1f, maxValue = 8f, stepIncrement = 0.5f, scene = UI_Scene.All)]
		public float steerDamping = 5;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Speed"),
		 UI_FloatRange(minValue = 125f, maxValue = 800f, stepIncrement = 1.0f, scene = UI_Scene.All)]
		public float maxSpeed = 325;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Standby Mode"),
		 UI_Toggle(enabledText = "On", disabledText = "Off")]
		public bool standbyMode = false;

		float threatLevel = 1;
		float turningTimer = 0;
		Vector3 lastTargetPosition;

		string debugString = string.Empty;

		LineRenderer lr;
		Vector3 flyingToPosition;

		void Start()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				GameObject velocityObject = new GameObject("velObject");
				velocityObject.transform.position = transform.position;
				velocityObject.transform.parent = transform;
				velocityTransform = velocityObject.transform;

				part.OnJustAboutToBeDestroyed += DeactivatePilot;
				vessel.OnJustAboutToBeDestroyed += DeactivatePilot;

				if(pilotEnabled)
				{
					ActivatePilot();
				}
			}

			RefreshPartWindow();
		}

		[KSPAction("Activate Pilot")]
		public void AGActivatePilot(KSPActionParam param)
		{
			ActivatePilot();
		}

		[KSPAction("Deactivate Pilot")]
		public void AGDeactivatePilot(KSPActionParam param)
		{
			DeactivatePilot();
		}

		[KSPAction("Toggle Pilot")]
		public void AGTogglePilot(KSPActionParam param)
		{
			TogglePilot();
		}


		public void ActivatePilot()
		{
			pilotEnabled = true;
			vessel.OnFlyByWire += AutoPilot;
			startedLanded = vessel.Landed;

			RefreshPartWindow();
		}

		public void DeactivatePilot()
		{
			pilotEnabled = false;
			vessel.OnFlyByWire -= AutoPilot;
			RefreshPartWindow();
		}



		[KSPEvent(guiActive = true, guiName = "Toggle Pilot", active = true)]
		public void TogglePilot()
		{
			if(pilotEnabled)
			{
				DeactivatePilot();
			}
			else
			{
				ActivatePilot();
			}
		}

		void RefreshPartWindow()
		{
			Events["TogglePilot"].guiName = pilotEnabled ? "Deactivate Pilot" : "Activate Pilot";

			//Misc.RefreshAssociatedWindows(part);
		}

		void Update()
		{
			if(BDArmorySettings.DRAW_DEBUG_LINES && pilotEnabled)
			{
				if(lr)
				{
					lr.enabled = true;
					lr.SetPosition(0, vessel.ReferenceTransform.position);
					lr.SetPosition(1, flyingToPosition);
				}
				else
				{
					lr = gameObject.AddComponent<LineRenderer>();
					lr.SetVertexCount(2);
					lr.SetWidth(0.5f, 0.5f);
				}
			}
			else
			{
				if(lr)
				{
					lr.enabled = false;
				}
			}
		}


		void AutoPilot(FlightCtrlState s)
		{
			if(!vessel || !vessel.transform)
			{
				return;
			}

			//default brakes off full throttle
			s.mainThrottle = 1;
			vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
			vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);


			//brake and cut throttle if exceeding max speed
			if(vessel.srfSpeed > maxSpeed)
			{
				vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
				s.mainThrottle = 0;
			}


			GetGuardTarget();

			if(vessel.Landed && standbyMode && wm && BDATargetManager.TargetDatabase[BDATargetManager.BoolToTeam(wm.team)].Count == 0)
			{
				s.mainThrottle = 0;
				vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
				return;
			}

			//upDirection = -FlightGlobals.getGeeForceAtPosition(transform.position).normalized;
			upDirection = (transform.position-FlightGlobals.currentMainBody.transform.position).normalized;
			debugString = string.Empty;

			if(MissileGuidance.GetRadarAltitude(vessel) < minAltitude)
			{
				startedLanded = true;
			}

			if(startedLanded)
			{
				TakeOff(s);
				turningTimer = 0;
			}
			else
			{
				if(wm && wm.guardMode && !targetVessel)
				{
					TargetInfo potentialTarget = BDATargetManager.GetLeastEngagedTarget(wm);
					if(potentialTarget && potentialTarget.Vessel)
					{
						targetVessel = potentialTarget.Vessel;
					}
				}

				if(wm && wm.missileIsIncoming)
				{
					Evasive(s);
					turningTimer = 0;
				}
				else if(!extending && wm && targetVessel!=null && targetVessel.transform!=null)
				{
					if(!targetVessel.Landed)
					{
						if(vessel.altitude < defaultAltitude && Vector3.Angle(targetVessel.transform.position-transform.position, -upDirection) < 35)
						{
							//dangerous if low altitude and target is far below you - don't dive into ground!
							extending = true;
							lastTargetPosition = targetVessel.transform.position;
						}

						if(Vector3.Angle(targetVessel.transform.position-vessel.ReferenceTransform.position, vessel.ReferenceTransform.up) > 35)
						{
							turningTimer += Time.deltaTime;
						}
						else
						{
							turningTimer = 0;
						}

						debugString += "turningTimer: "+turningTimer;

						if(turningTimer > 10)
						{
							//extend if turning circles for too long
							extending = true;
							turningTimer = 0;
							lastTargetPosition = targetVessel.transform.position;
						}
					}
					else //extend if too close for agm attack
					{
						float extendDistance = Mathf.Clamp(wm.guardRange-1800, 2500, 4000);
						Vector3 surfaceVector = GetSurfacePosition(targetVessel.transform.position)-GetSurfacePosition(vessel.transform.position);
						if(surfaceVector.sqrMagnitude < Mathf.Pow(extendDistance, 2) && Vector3.Angle(vessel.ReferenceTransform.up, targetVessel.transform.position-transform.position) > 45)
						{
							extending = true;
							lastTargetPosition = targetVessel.transform.position;
						}
					}

					if(!extending)
					{
						debugString += "\nFlying to target";
						threatLevel = 1;
						FlyToTargetVessel(s, targetVessel);
					}
				}
				else
				{
					if(!extending)
					{
						FlyCircular(s);
					}
				}

				if(extending)
				{
					threatLevel = 1;
					debugString += "\nExtending";
					FlyExtend(s, lastTargetPosition);
				}
			}

			debugString += "\nthreatLevel: "+threatLevel;
		}

		void FlyToTargetVessel(FlightCtrlState s, Vessel v)
		{
			Vector3 target = v.transform.position;
			MissileLauncher missile = null;
			if(wm)
			{
				missile = wm.currentMissile;
				if(missile != null)
				{
					target = MissileGuidance.GetAirToAirFireSolution(missile, v);
            	}
				else
				{
					ModuleWeapon weapon = wm.currentGun;
					if(weapon!=null)
					{
						target -= 1.25f*weapon.GetLeadOffset();
					}
				}


			}

			FlyToPosition(s, target);

			//try airbrake if in front of enemy
			if(Vector3.Angle(vessel.ReferenceTransform.up, v.transform.position-vessel.transform.position) > 120 //angle to enemy is greater than 120
			   && (v.transform.position-vessel.transform.position).sqrMagnitude < Mathf.Pow(800, 2) //distance is less than 800m
			   && vessel.srfSpeed > 200) //airspeed is more than 200 
			{
				debugString += ("\nEnemy on tail. Braking");
				vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
			}
			if(missile!=null 
			   && Vector3.Angle(vessel.ReferenceTransform.up, v.transform.position-vessel.transform.position) <45
			   && (v.transform.position-vessel.transform.position).sqrMagnitude < Mathf.Pow(300, 2)
			   && vessel.srfSpeed > 130)
			{
				extending = true;
				lastTargetPosition = v.transform.position;
			}

		}

		void FlyToPosition(FlightCtrlState s, Vector3 targetPosition)
		{
			if(!startedLanded)
			{
				targetPosition = FlightPosition(targetPosition, minAltitude);
			}

			if(BDArmorySettings.DRAW_DEBUG_LINES)
			{
				flyingToPosition = targetPosition;
			}

			Transform vesselTransform = vessel.ReferenceTransform;
			velocityTransform.rotation = Quaternion.LookRotation(vessel.srf_velocity, -vesselTransform.forward);
			Vector3 localAngVel = vessel.angularVelocity;


			Vector3 targetDirection = velocityTransform.InverseTransformPoint(targetPosition).normalized;
			targetDirection = Vector3.RotateTowards(Vector3.forward, targetDirection, 15*Mathf.Deg2Rad, 0);

			Vector3 targetDirectionYaw = vessel.ReferenceTransform.InverseTransformPoint(vesselTransform.position+vessel.srf_velocity);
			float angleYaw = Misc.SignedAngle(Vector3.up, targetDirectionYaw, Vector3.right);
			
			float steerYaw = (.07f * angleYaw);
			if(Mathf.Sign (steerYaw) == Mathf.Sign (-localAngVel.z))
			{
				steerYaw -= (1.35f*steerDamping * -localAngVel.z);
			}

			//float anglePitch = Misc.SignedAngle(Vector3.up, targetDirection, Vector3.back);
			float steerPitch = (steerMult * targetDirection.y) - (steerDamping * -localAngVel.x);

			float finalMaxSteer = threatLevel * maxSteer;

			float yaw = Mathf.Clamp(steerYaw, -finalMaxSteer, finalMaxSteer);
			s.yaw = yaw;

			s.pitch = Mathf.Clamp(steerPitch, Mathf.Max(-finalMaxSteer, -0.25f), finalMaxSteer);


			//roll
			Vector3 rollTarget = Vector3.ProjectOnPlane(upDirection, vesselTransform.up);
			if(Vector3.Angle(vesselTransform.up, targetPosition-vesselTransform.position) > 3)
			{
				rollTarget = Vector3.ProjectOnPlane((targetPosition+(45f*upDirection))-vesselTransform.position, vesselTransform.up);
			}



			Vector3 currentRoll = -vesselTransform.forward;
			float rollOffset = Misc.SignedAngle(currentRoll, rollTarget, vesselTransform.right);
			debugString += "\nRoll offset: "+rollOffset;
			float steerRoll = (steerMult * 0.0015f * rollOffset);
			debugString += "\nSteerRoll: "+steerRoll;
			float rollDamping = (.10f * steerDamping * -localAngVel.y);
			steerRoll -= rollDamping;
			debugString += "\nRollDamping: "+rollDamping;

			float roll = Mathf.Clamp(steerRoll, -maxSteer/2, maxSteer/2);
			s.roll = roll;
			//
		}

		void FlyExtend(FlightCtrlState s, Vector3 tPosition)
		{
			if(wm)
			{
				float extendDistance = Mathf.Clamp(wm.guardRange-1800, 2500, 4000);
				if(targetVessel!=null && !targetVessel.Landed)
				{
					extendDistance = 800;
				}

				Vector3 surfaceVector = GetSurfacePosition(tPosition)-GetSurfacePosition(vessel.transform.position);
				if(surfaceVector.sqrMagnitude < Mathf.Pow(extendDistance, 2))
				{
					Vector3 target = FlightPosition(tPosition + ((-surfaceVector).normalized*extendDistance), defaultAltitude);
					FlyToPosition(s, target);
					flyingToPosition = target;
				}
				else
				{
					extending = false;
				}
			}
			else
			{
				extending = false;
			}
		}

		void FlyCircular(FlightCtrlState s)
		{
			debugString += "\nFlying circular";
			bool enemiesNearby = false;
			if(wm)
			{
				BDArmorySettings.BDATeams team = wm.team ? BDArmorySettings.BDATeams.B : BDArmorySettings.BDATeams.A;
				if(BDATargetManager.TargetDatabase[team].Count > 0)
				{
					threatLevel = 1;
					enemiesNearby = true;
				}
			}

			if(!enemiesNearby)
			{
				threatLevel = Mathf.MoveTowards(threatLevel, 0.5f, 0.05f*Time.deltaTime);
			}
			Vector3 axis = Vector3.Project(-vessel.ReferenceTransform.right, upDirection).normalized;
			Vector3 target = DefaultAltPosition() + Quaternion.AngleAxis(15, axis) * Vector3.ProjectOnPlane(vessel.ReferenceTransform.up * 1000, upDirection);
			FlyToPosition(s, target);
		}

		void Evasive(FlightCtrlState s)
		{
			debugString += "\nEvasive";
			threatLevel = 1f;
			Vector3 target = DefaultAltPosition() 
				+ (Quaternion.AngleAxis(Mathf.Sin (Time.time) * 80, upDirection) * Vector3.ProjectOnPlane(vessel.ReferenceTransform.up * 750, upDirection))
				+ (Mathf.Sin (Time.time/2) * upDirection * defaultAltitude/3);

			FlyToPosition(s, target);
		}

		void TakeOff(FlightCtrlState s)
		{
			threatLevel = 1;
			debugString += "\nTaking off/Gaining altitude";

			float radarAlt = MissileGuidance.GetRadarAltitude(vessel);

			if(radarAlt > 70)
			{
				FlyToPosition(s, transform.position + Vector3.ProjectOnPlane(vessel.ReferenceTransform.up * 100, upDirection) + (upDirection * 50));
				vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, false);
			}
			else
			{
				FlyToPosition(s, transform.position + Vector3.ProjectOnPlane(vessel.ReferenceTransform.up * 100, upDirection) + (upDirection * 20));
				vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, true);
			}

			if(radarAlt > minAltitude)
			{
				startedLanded = false;
			}
		}

		Vector3 DefaultAltPosition()
		{
			return (transform.position + (-(float)vessel.altitude*upDirection) + (defaultAltitude *upDirection));
		}

		Vector3 GetSurfacePosition(Vector3 position)
		{
			return position - ((float)FlightGlobals.getAltitudeAtPos(position) * upDirection);
		}


		Vector3 FlightPosition(Vector3 targetPosition, float minAlt)
		{
			Vector3 forwardDirection = vessel.ReferenceTransform.up;
			Vector3 targetDirection = (targetPosition - vessel.ReferenceTransform.position).normalized;
			if(Vector3.Angle(targetDirection, forwardDirection) > 90)
			{
				targetPosition = vessel.ReferenceTransform.position + Vector3.ProjectOnPlane(Vector3.RotateTowards(forwardDirection, targetDirection, 90*Mathf.Deg2Rad, 0), upDirection).normalized*200;
			}
			float pointRadarAlt = MissileGuidance.GetRaycastRadarAltitude(targetPosition);
			if(pointRadarAlt < minAlt)
			{
				float adjustment = (minAlt-pointRadarAlt);
				debugString += "\nTarget position is below minAlt. Adjusting by "+adjustment;
				return targetPosition + (adjustment * upDirection);
			}
			else
			{
				return targetPosition;
			}
		}

		public bool GetLaunchAuthorizion(Vessel targetV, MissileFire mf)
		{
			bool launchAuthorized = false;
			Vector3 target = targetV.transform.position;
			MissileLauncher missile = mf.currentMissile;
			if(missile != null)
			{
				target = MissileGuidance.GetAirToAirFireSolution(missile, targetV);
			}

			if(Vector3.Angle(vessel.ReferenceTransform.up, target-vessel.ReferenceTransform.position) < 20)
			   //|| (targetV.Landed && Vector3.Angle(vessel.ReferenceTransform.up, FlightPosition(target, (float)vessel.altitude)-vessel.ReferenceTransform.position) < 15))
			{
				launchAuthorized = true;
			}

			return launchAuthorized;
		}

		void GetGuardTarget()
		{
			if(wm!=null && wm.vessel == vessel)
			{
				if(wm.currentTarget!=null)
				{
					targetVessel = wm.currentTarget.Vessel;
				}
				else
				{
					targetVessel = null;
				}
				wm.pilotAI = this;
				return;
			}
			else
			{
				foreach(var mf in vessel.FindPartModulesImplementing<MissileFire>())
				{
					if(mf.currentTarget!=null)
					{
						targetVessel = mf.currentTarget.Vessel;
					}
					else
					{
						targetVessel = null;
					}

					wm = mf;
					mf.pilotAI = this;

					return;
				}
			}
		}

		void OnGUI()
		{
			if(pilotEnabled && BDArmorySettings.DRAW_DEBUG_LABELS && vessel.isActiveVessel)	
			{
				GUI.Label(new Rect(200,600,400,400), debugString);	
			}
		}

	}
}

