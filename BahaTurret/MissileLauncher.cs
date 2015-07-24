using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{

	
	public class MissileLauncher : PartModule, IBDWeapon
	{
		public enum MissileStates{Idle, Drop, Boost, Cruise, PostThrust}


		public enum GuidanceModes{AAMLead,AAMPure,AGM,Cruise,STS,Bomb,RCS}
		public GuidanceModes guidanceMode;
		[KSPField(isPersistant = false)]
		public string homingType = "AAM";

		[KSPField(isPersistant = false)]
		public string targetingType = "radar";
		public enum TargetingModes{None,Radar,Heat,Laser,GPS,AntiRad}
		public TargetingModes targetingMode;
		public bool team;
		
		public float timeFired = -1;
		public float timeIndex = 0;

		//aero
		[KSPField(isPersistant = false)]
		public bool aero = false;
		[KSPField(isPersistant = false)]
		public float liftArea = 0.015f;
		[KSPField(isPersistant = false)]
		public float steerMult = 0.5f;
		Vector3 aeroTorque = Vector3.zero;
		float controlAuthority = 0;
		float finalMaxTorque = 0;

		[KSPField(isPersistant = false)]
		public float maxTorque = 90;
		//

		[KSPField(isPersistant = false)]
		public float thrust = 30;
		[KSPField(isPersistant = false)]
		public float cruiseThrust = 3;
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Drop Time"),
        	UI_FloatRange(minValue = 0f, maxValue = 2f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
		public float dropTime = 0.4f;
		[KSPField(isPersistant = false)]
		public float boostTime = 2.2f;
		[KSPField(isPersistant = false)]
		public float cruiseTime = 45;
		[KSPField(isPersistant = false)]
		public bool guidanceActive = true;
		[KSPField(isPersistant = false)]
		public float maxOffBoresight = 45;
		
		[KSPField(isPersistant = false)]
		public float maxAoA = 35;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Decouple Speed"),
        	UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float decoupleSpeed = 0;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Direction: "), 
			UI_Toggle(disabledText = "Lateral", enabledText = "Forward")]
		public bool decoupleForward = false;
		


		[KSPField(isPersistant = false)]
		public float optimumAirspeed = 220;
		
		[KSPField(isPersistant = false)]
		public float blastRadius = 150;
		[KSPField(isPersistant = false)]
		public float blastPower = 25;
		[KSPField(isPersistant = false)]
		public float maxTurnRateDPS = 20;
		
		[KSPField(isPersistant = false)]
		public string audioClipPath = string.Empty;

		AudioClip thrustAudio;

		[KSPField(isPersistant = false)]
		public string boostClipPath = string.Empty;

		AudioClip boostAudio;
		
		[KSPField(isPersistant = false)]
		public bool isSeismicCharge = false;
		
		[KSPField(isPersistant = false)]
		public float rndAngVel = 0;
		
		[KSPField(isPersistant = false)]
		public bool isTimed = false;
		
		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Detonation Time"),
        	UI_FloatRange(minValue = 2f, maxValue = 30f, stepIncrement = 0.5f, scene = UI_Scene.Editor)]
		public float detonationTime = 2;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Cruise Altitude"),
		 UI_FloatRange(minValue = 50f, maxValue = 1500f, stepIncrement = 10f, scene = UI_Scene.All)]
		public float cruiseAltitude = 300;

		[KSPField(isPersistant = false)]
		public string rotationTransformName = string.Empty;
		Transform rotationTransform;

		[KSPField(isPersistant = false)]
		public bool terminalManeuvering = false;
		
		
		
		[KSPField(isPersistant = false)]
		public string explModelPath = "BDArmory/Models/explosion/explosion";
		
		public string explSoundPath = "BDArmory/Sounds/explode1";
			
		
		[KSPField(isPersistant = false)]
		public bool spoolEngine = false;
		
		[KSPField(isPersistant = false)]
		public bool hasRCS = false;
		[KSPField(isPersistant = false)]
		public float rcsThrust = 1;
		float rcsRVelThreshold = 0.13f;
		KSPParticleEmitter upRCS;
		KSPParticleEmitter downRCS;
		KSPParticleEmitter leftRCS;
		KSPParticleEmitter rightRCS;
		KSPParticleEmitter forwardRCS;
		float rcsAudioMinInterval = 0.2f;

		public Vessel sourceVessel = null;
		bool checkMiss = false;
		bool hasExploded = false;
		bool targetInView = false;
		private AudioSource audioSource;
		public AudioSource sfAudioSource;
		List<KSPParticleEmitter> pEmitters;
		List<BDAGaplessParticleEmitter> gaplessEmitters;
		

		public Vessel targetVessel = null;
		public MissileFire targetMf = null;
		public bool hasFired = false;

		bool startedEngine = false;
		
		LineRenderer LR;
		float cmTimer;
		
		//deploy animation
		[KSPField(isPersistant = false)]
		public string deployAnimationName = "";
		
		[KSPField(isPersistant = false)]
		public float deployedDrag = 0.02f;
		
		[KSPField(isPersistant = false)]
		public float deployTime = 0.2f;

		[KSPField(isPersistant = false)]
		public bool useSimpleDrag = false;

		[KSPField(isPersistant = false)]
		public Vector3 simpleCoD = new Vector3(0,0,-1);

		[KSPField(isPersistant = false)]
		public float agmDescentRatio = 1.45f;
		
		float currentThrust = 0;
		
		public bool deployed;
		public float deployedTime;
		
		AnimationState[] deployStates;
		
		bool hasPlayedFlyby = false;
		
		Quaternion previousRotation;
		
		float debugTurnRate = 0;
		string debugString = "";
		
		Vector3 randomOffset = Vector3.zero;


		bool targetAcquired = false;
		Vector3 targetPosition = Vector3.zero;
		Vector3 targetVelocity = Vector3.zero;
		Vector3 targetAcceleration = Vector3.zero;

		Vessel legacyTargetVessel;



		[KSPField(isPersistant = false)]
		public string boostTransformName = string.Empty;
		List<KSPParticleEmitter> boostEmitters;

		public MissileStates MissileState = MissileStates.Idle;

		//laser stuff
		public ModuleTargetingCamera lockedCamera = null;
		Vector3 lastLaserPoint;
		Vector3 laserStartPosition;
		Vector3 laserStartDirection;


		//heat stuff
		public TargetSignatureData heatTarget;
		[KSPField]
		public float heatThreshold = 200;
		float heatFailTimer = -1;


		//radar stuff
		public ModuleRadar radar;
		public TargetSignatureData radarTarget;
		[KSPField]
		public float activeRadarRange = 6000;
		public bool activeRadar = false;


		//weapon interface
		[KSPField]
		public string missileType = "missile";
		private WeaponClasses weaponClass;
		public WeaponClasses GetWeaponClass()
		{
			return weaponClass;
		}
		void ParseWeaponClass()
		{
			missileType = missileType.ToLower();
			if(missileType == "bomb")
			{
				weaponClass = WeaponClasses.Bomb;
			}
			else
			{
				weaponClass = WeaponClasses.Missile;
			}
		}
		[KSPField]
		public string shortName = string.Empty;
		public string GetShortName()
		{
			return shortName;
		}
		public Part GetPart()
		{
			return part;
		}



		public override void OnStart (PartModule.StartState state)
		{
			ParseWeaponClass();

			if(shortName == string.Empty)
			{
				shortName = part.partInfo.title;
			}

			gaplessEmitters = new List<BDAGaplessParticleEmitter>();
			pEmitters = new List<KSPParticleEmitter>();
			boostEmitters = new List<KSPParticleEmitter>();

			if(isTimed)
			{
				Fields["detonationTime"].guiActive = true;
				Fields["detonationTime"].guiActiveEditor = true;
			}
			else
			{
				Fields["detonationTime"].guiActive = false;
				Fields["detonationTime"].guiActiveEditor = false;
			}
			
			if(HighLogic.LoadedSceneIsFlight)
			{
				ParseModes();

				foreach(var emitter in part.FindModelComponents<KSPParticleEmitter>())
				{
					if(emitter.useWorldSpace)
					{
						BDAGaplessParticleEmitter gaplessEmitter = emitter.gameObject.AddComponent<BDAGaplessParticleEmitter>();	
						gaplessEmitter.part = part;
						gaplessEmitters.Add (gaplessEmitter);
					}
					else
					{
						if(emitter.transform.name != boostTransformName)
						{
							pEmitters.Add(emitter);	
						}
						else
						{
							boostEmitters.Add(emitter);
						}
					}
				}
				//pEmitters = part.FindModelComponents<KSPParticleEmitter>();
				
				audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME)+0.1f;
				audioSource.minDistance = 1;
				audioSource.maxDistance = 1000;
				audioSource.loop = true;
				audioSource.pitch = 1f;
				audioSource.priority = 255;
				
				previousRotation = transform.rotation;
			
				if(audioClipPath!=string.Empty)
				{
					audioSource.clip = GameDatabase.Instance.GetAudioClip(audioClipPath);
				}
				
				sfAudioSource = gameObject.AddComponent<AudioSource>();
				sfAudioSource.volume = Mathf.Sqrt(GameSettings.SHIP_VOLUME);
				sfAudioSource.minDistance = 1;
				sfAudioSource.maxDistance = 2000;
				sfAudioSource.dopplerLevel = 0;
				sfAudioSource.priority = 230;
				
				
				cmTimer = Time.time;
				
				part.force_activate();
				part.OnJustAboutToBeDestroyed += new Callback(Detonate);
				
			
				
				foreach(var pe in pEmitters)	
				{
					if(hasRCS)
					{
						if(pe.gameObject.name == "rcsUp") upRCS = pe;
						else if(pe.gameObject.name == "rcsDown") downRCS = pe;
						else if(pe.gameObject.name == "rcsLeft") leftRCS = pe;
						else if(pe.gameObject.name == "rcsRight") rightRCS = pe;
						else if(pe.gameObject.name == "rcsForward") forwardRCS = pe;
					}
					
					if(!pe.gameObject.name.Contains("rcs") && !pe.useWorldSpace)
					{
						pe.sizeGrow = 99999;
					}
				}
				
				if(hasRCS)
				{
					SetupRCS();
					KillRCS();
				}

				if(rotationTransformName!=string.Empty)
				{
					rotationTransform = part.FindModelTransform(rotationTransformName);
				}

				if(audioClipPath != string.Empty)
				{
					thrustAudio = GameDatabase.Instance.GetAudioClip(audioClipPath);
				}

				if(boostClipPath != string.Empty)
				{
					boostAudio = GameDatabase.Instance.GetAudioClip(boostClipPath);
				}

			}

			if(guidanceMode != GuidanceModes.Cruise)
			{
				Fields["cruiseAltitude"].guiActive = false;
				Fields["cruiseAltitude"].guiActiveEditor = false;
			}
			
			if(part.partInfo.title.Contains("Bomb"))
			{
				Fields["dropTime"].guiActive = false;
				Fields["dropTime"].guiActiveEditor = false;
			}
			
			if(deployAnimationName != "")
			{
				deployStates = Misc.SetUpAnimation(deployAnimationName, part);
			}
			else
			{
				deployedDrag = part.maximum_drag;	
			}
		}
		
		[KSPAction("Fire Missile")]
		public void AGFire(KSPActionParam param)
		{
			FireMissile();	
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}
		
		[KSPEvent(guiActive = true, guiName = "Fire Missile", active = true)]
		public void GuiFire()
		{
			FireMissile();	
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}

		[KSPEvent(guiActive = true, guiActiveEditor = false, active = true, guiName = "Jettison")]
		public void Jettison()
		{
			part.decouple(0);
			if(BDArmorySettings.Instance.ActiveWeaponManager!=null) BDArmorySettings.Instance.ActiveWeaponManager.UpdateList();
		}
		
		
		public void FireMissile()
		{
			if(!hasFired)
			{
				if(GetComponentInChildren<KSPParticleEmitter>())
				{
					BDArmorySettings.numberOfParticleEmitters++;
				}
				
				foreach(var wpm in vessel.FindPartModulesImplementing<MissileFire>())
				{
					team = wpm.team;	
					break;
				}
				
				sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/deployClick"));
				
				sourceVessel = vessel;



				//TARGETING
				if(BDArmorySettings.ALLOW_LEGACY_TARGETING)
				{
					if(vessel.targetObject!=null && vessel.targetObject.GetVessel()!=null)
					{
						legacyTargetVessel = vessel.targetObject.GetVessel();

						if(targetingMode == TargetingModes.Heat)
						{
							heatTarget = new TargetSignatureData(legacyTargetVessel, 9999);
						}
					}
				}
				if(targetingMode == TargetingModes.Laser)
				{
					laserStartPosition = transform.position;
					laserStartDirection = transform.forward;
					if(lockedCamera)
					{
						targetAcquired = true;
						targetPosition = lastLaserPoint = lockedCamera.groundTargetPosition;
					}
				}

				part.decouple(0);
				part.force_activate();
				part.Unpack();
				vessel.situation = Vessel.Situations.FLYING;
				rigidbody.isKinematic = false;
				BDArmorySettings.Instance.ApplyNewVesselRanges(vessel);


				//add target info to vessel
				if(targetVessel!=null && !vessel.gameObject.GetComponent<TargetInfo>())
				{
					TargetInfo info = vessel.gameObject.AddComponent<TargetInfo>();
					info.isMissile = true;
					info.missileModule = this;

					foreach(var mf in targetVessel.FindPartModulesImplementing<MissileFire>())
					{
						targetMf = mf;
						break;
					}
				}

				if(decoupleForward)
				{
					vessel.rigidbody.velocity += decoupleSpeed * part.transform.forward;
				}
				else
				{
					vessel.rigidbody.velocity += decoupleSpeed * -part.transform.up;
				}
				
				if(rndAngVel > 0)
				{
					vessel.rigidbody.angularVelocity += UnityEngine.Random.insideUnitSphere.normalized * rndAngVel;	
				}
				

				vessel.vesselName = part.partInfo.title + " (fired)";
				vessel.vesselType = VesselType.Probe;

				
				timeFired = Time.time;
				hasFired = true;
				
				previousRotation = transform.rotation;

				//setting ref transform for navball
				GameObject refObject = new GameObject();
				refObject.transform.rotation = Quaternion.LookRotation(-transform.up, transform.forward);
				refObject.transform.parent = transform;
				part.SetReferenceTransform(refObject.transform);
				vessel.SetReferenceTransform(part);

				MissileState = MissileStates.Drop;

				part.crashTolerance = 9999;
				
			}
		}

		/// <summary>
		/// Fires the missile on target vessel.  Used by AI currently.
		/// </summary>
		/// <param name="v">V.</param>
		public void FireMissileOnTarget(Vessel v)
		{
			if(!hasFired)
			{
				targetVessel = v;

				if(targetingMode == TargetingModes.Heat)
				{
					heatTarget = new TargetSignatureData(v, 9999);
				}
				if(targetingMode == TargetingModes.Radar)
				{
					radarTarget = new TargetSignatureData(v, 2000);
				}

				FireMissile();
			}
		}
		
	
		
		
		
		public override void OnFixedUpdate()
		{

			debugString = "";
			if(hasFired && !hasExploded && part!=null)
			{
				rigidbody.isKinematic = false;
				AntiSpin();

				//deploy stuff
				if(deployAnimationName != "" && timeIndex > deployTime && !deployed)
				{
					deployed = true;
					deployedTime = Time.time;
				}
				
				if(deployed)
				{
					foreach(var anim in deployStates)
					{
						anim.speed = 1;
						part.maximum_drag = deployedDrag;
						part.minimum_drag = deployedDrag;
					}	
				}

				//simpleDrag
				if(useSimpleDrag)
				{
					SimpleDrag();
				}

				//flybyaudio
				float mCamDistanceSqr = (FlightCamera.fetch.mainCamera.transform.position-transform.position).sqrMagnitude;
				float mCamRelVSqr = (float)(FlightGlobals.ActiveVessel.srf_velocity-vessel.srf_velocity).sqrMagnitude;
				if(!hasPlayedFlyby 
				   && FlightGlobals.ActiveVessel != vessel 
				   && FlightGlobals.ActiveVessel != sourceVessel 
				   && mCamDistanceSqr < 400*400 && mCamRelVSqr > 300*300  
				   && mCamRelVSqr < 800*800 
				   && Vector3.Angle(rigidbody.velocity, FlightGlobals.ActiveVessel.transform.position-transform.position)<60)
				{
					sfAudioSource.PlayOneShot (GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/missileFlyby"));	
					hasPlayedFlyby = true;
				}
				
				if(vessel.isActiveVessel)
				{
					audioSource.dopplerLevel = 0;
				}
				else
				{
					audioSource.dopplerLevel = 1f;
				}


				
				//Missile State
				timeIndex = Time.time - timeFired;
				if(timeIndex < dropTime)
				{
					MissileState = MissileStates.Drop;
				}
				else if(timeIndex < dropTime + boostTime)
				{
					MissileState = MissileStates.Boost;	
				}
				else if(timeIndex < dropTime+boostTime+cruiseTime)
				{
					MissileState = MissileStates.Cruise;	
				}
				else
				{
					MissileState = MissileStates.PostThrust;	
				}
				
				if(timeIndex > 0.5f)
				{
					part.crashTolerance = 1;
				}
				
				
				if(MissileState == MissileStates.Drop) //drop phase
				{
				}
				else if(MissileState == MissileStates.Boost) //boost phase
				{
					//light, sound & particle fx
					if(boostAudio||thrustAudio)	
					{
						if(!PauseMenu.isOpen)
						{
							if(!audioSource.isPlaying)
							{
								if(boostAudio)
								{
									audioSource.clip = boostAudio;
								}
								else if(thrustAudio)
								{
									audioSource.clip = thrustAudio;
								}
								audioSource.Play();	
							}
						}
						else if(audioSource.isPlaying)
						{
							audioSource.Stop();
						}
					}


					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 1.5f;	
					}
					if(spoolEngine) 
					{
						currentThrust = Mathf.MoveTowards(currentThrust, thrust, thrust/10);
					}
					else
					{
						currentThrust = thrust;	
					}

					if(boostTransformName != string.Empty)
					{
						foreach(var emitter in boostEmitters)
						{
							emitter.emit = true;
						}
					}

					rigidbody.AddRelativeForce(currentThrust * Vector3.forward);
					if(hasRCS) forwardRCS.emit = true;
					if(!startedEngine && thrust > 0)
					{
						startedEngine = true;
						sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/launch"));
					}
				}
				else if(MissileState == MissileStates.Cruise) //cruise phase
				{
					part.crashTolerance = 1;

					if(thrustAudio)	
					{
						if(!PauseMenu.isOpen)
						{
							if(!audioSource.isPlaying || audioSource.clip!=thrustAudio)
							{
								audioSource.clip = thrustAudio;
								audioSource.Play();	
							}
						}
						else if(audioSource.isPlaying)
						{
							audioSource.Stop();
						}
					}


					if(spoolEngine)
					{
						currentThrust = Mathf.MoveTowards(currentThrust, cruiseThrust, thrust/10);
					}
					else
					{
						currentThrust = cruiseThrust;	
					}
					
					if(!hasRCS)
					{
						rigidbody.AddRelativeForce(cruiseThrust * Vector3.forward);
					}
					else
					{
						forwardRCS.emit = false;
						audioSource.Stop();
					}

					if(boostTransformName != string.Empty)
					{
						foreach(var emitter in boostEmitters)
						{
							emitter.emit = false;
						}
					}
				}
				
				if(MissileState != MissileStates.Idle && MissileState != MissileStates.PostThrust && MissileState != MissileStates.Drop) //all thrust
				{
					if(!hasRCS)
					{
						foreach(KSPParticleEmitter pe in pEmitters)
						{
							pe.emit = true;
						}
						foreach(var gpe in gaplessEmitters)
						{
							if(vessel.atmDensity > 0)
							{
								gpe.emit = true;
								gpe.pEmitter.worldVelocity = 2*ParticleTurbulence.Turbulence;
							}
							else
							{
								gpe.emit = false;
							}	
						}
					}
					
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						if(!pe.gameObject.name.Contains("rcs") && !pe.useWorldSpace)
						{
							pe.sizeGrow = Mathf.Lerp(pe.sizeGrow, 1, 0.4f);
						}
					}
				}
				else
				{
					if(thrustAudio && audioSource.isPlaying)
					{
						audioSource.volume = Mathf.Lerp(audioSource.volume, 0, 0.1f);
						audioSource.pitch = Mathf.Lerp(audioSource.pitch, 0, 0.1f);
					}
					foreach(Light light in gameObject.GetComponentsInChildren<Light>())
					{
						light.intensity = 0;	
					}
				}
				if(timeIndex > dropTime + boostTime + cruiseTime && !hasRCS)
				{
					foreach(KSPParticleEmitter pe in pEmitters)
					{
						pe.maxEmission = Mathf.FloorToInt(pe.maxEmission * 0.8f);
						pe.minEmission =  Mathf.FloorToInt(pe.minEmission * 0.8f);
					}
					foreach(var gpe in gaplessEmitters)
					{
						gpe.pEmitter.maxSize = Mathf.MoveTowards(gpe.pEmitter.maxSize, 0, 0.005f);
						gpe.pEmitter.minSize = Mathf.MoveTowards(gpe.pEmitter.minSize, 0, 0.008f);
						gpe.pEmitter.worldVelocity = 2*ParticleTurbulence.Turbulence;
					}
				}
				
				

				if(MissileState != MissileStates.Idle && MissileState != MissileStates.Drop) //guidance
				{
					//guidance and attitude stabilisation scales to atmospheric density. //use part.atmDensity
					float atmosMultiplier = Mathf.Clamp01 (2.5f*(float)FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(transform.position), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody)); 

					float optimumSpeedFactor = (float)vessel.srfSpeed/(2*optimumAirspeed);
					controlAuthority = Mathf.Clamp01(atmosMultiplier * (-Mathf.Abs(2*optimumSpeedFactor-1) + 1));
					debugString += "\ncontrolAuthority: "+controlAuthority;

					if(guidanceActive)// && timeIndex - dropTime > 0.5f)
					{
						WarnTarget();
						Vector3 targetPosition = Vector3.zero;

						if(targetVessel && targetVessel.loaded)
						{
							Vector3 targetCoMPos = targetVessel.findWorldCenterOfMass();
							targetPosition = targetCoMPos+targetVessel.rb_velocity*Time.fixedDeltaTime;
						}
					
						//increaseTurnRate after launch
						float turnRateDPS = Mathf.Clamp(((timeIndex-dropTime)/boostTime)*maxTurnRateDPS * 25f, 0, maxTurnRateDPS);
						float turnRatePointDPS = turnRateDPS;
						if(!hasRCS)
						{
							turnRateDPS *= controlAuthority;
						}
						
						//decrease turn rate after thrust cuts out
						if(timeIndex > dropTime+boostTime+cruiseTime)
						{
							turnRateDPS = atmosMultiplier * Mathf.Clamp(maxTurnRateDPS - ((timeIndex-dropTime-boostTime-cruiseTime)*0.45f), 1, maxTurnRateDPS);	
							if(hasRCS) 
							{
								turnRateDPS = 0;
							}
						}
						
						if(hasRCS)
						{
							if(turnRateDPS > 0)
							{
								DoRCS();
							}
							else
							{
								KillRCS();
							}
						}
						debugTurnRate = turnRateDPS;
						float radiansDelta = turnRateDPS*Mathf.Deg2Rad*Time.fixedDeltaTime;

						finalMaxTorque = Mathf.Clamp((timeIndex-dropTime)*30, 0, maxTorque); //ramp up torque

						if(guidanceMode == GuidanceModes.AAMLead)
						{
							AAMGuidance();
						}
						else if(guidanceMode == GuidanceModes.AGM)
						{
							AGMGuidance();
						}
						else if(guidanceMode == GuidanceModes.RCS)
						{
							if(targetVessel!=null)
							{
								transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetPosition-transform.position, transform.up), turnRateDPS*Time.fixedDeltaTime);
							}
						}
						else if(guidanceMode == GuidanceModes.Cruise)
						{
							Vector3 cruiseTarget = Vector3.zero;
							if(targetVessel!=null)
							{
								//if(terminalManeuvering && targetDistance < 4500)
								//{
								//	cruiseTarget = MissileGuidance.GetTerminalManeuveringTarget(targetPosition, vessel, targetVessel);
							//	}
							//	else
							//	{
									cruiseTarget = MissileGuidance.GetCruiseTarget(targetPosition, vessel, targetVessel, cruiseAltitude);
							//	}

								targetPosition = cruiseTarget;
								
								float clampedSpeed = Mathf.Clamp((float) vessel.srfSpeed, 1, 1000);
								float limitAoA = Mathf.Clamp(3500/clampedSpeed, 5, maxAoA);
								
								//debugString += "\n limitAoA: "+limitAoA.ToString("0.0");

								Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D());

								//axial rotation
								if(rotationTransform)
								{
									Quaternion originalRotation = transform.rotation;
									Quaternion originalRTrotation = rotationTransform.rotation;
									transform.rotation = Quaternion.LookRotation(transform.forward, upDirection);
									rotationTransform.rotation = originalRTrotation;
									Vector3 lookUpDirection = Misc.ProjectOnPlane(vessel.acceleration, Vector3.zero, transform.forward);
									lookUpDirection = transform.InverseTransformPoint(lookUpDirection + transform.position);

									lookUpDirection = new Vector3(lookUpDirection.x, 0, 0);
									lookUpDirection += 10*Vector3.up;
									//Debug.Log ("lookUpDirection: "+lookUpDirection);
								

									rotationTransform.localRotation = Quaternion.Lerp(rotationTransform.localRotation, Quaternion.LookRotation(Vector3.forward, lookUpDirection), 0.04f);
									Quaternion finalRotation = rotationTransform.rotation;
									transform.rotation = originalRotation;
									rotationTransform.rotation = finalRotation;
								}

								aeroTorque = MissileGuidance.DoAeroForces(this, cruiseTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, limitAoA); 
							}
						}
					

					}
					else
					{
						targetMf = null;
						if(!aero)
						{
							if(!hasRCS && !useSimpleDrag)	
							{
								transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vessel.srf_velocity, transform.up), atmosMultiplier * (0.5f*(timeIndex-dropTime)) * 50*Time.fixedDeltaTime);	
							}
						}
						else
						{
							aeroTorque = MissileGuidance.DoAeroForces(this, transform.position + 20*vessel.srf_velocity, liftArea, .25f, aeroTorque, maxTorque, maxAoA);
						}


					}
					
					if(hasRCS && !guidanceActive)
					{
						KillRCS();	
					}
				}
				
				//Timed detonation
				if(isTimed && timeIndex > detonationTime)
				{
					Detonate();
				}
				
				
			}
		}

		void AAMGuidance()
		{
		
			if(BDArmorySettings.ALLOW_LEGACY_TARGETING && legacyTargetVessel)
			{
				UpdateLegacyTarget();
			}
			else
			{
				if(targetingMode == TargetingModes.Heat)
				{
					UpdateHeatTarget();
				}
				else if(targetingMode == TargetingModes.Radar)
				{
					UpdateRadarTarget();
				}
			}

			Vector3 aamTarget;
			if(targetAcquired)
			{
				DrawDebugLine(transform.position+(part.rb.velocity*Time.fixedDeltaTime), targetPosition);

				aamTarget = MissileGuidance.GetAirToAirTarget(targetPosition, targetVelocity, targetAcceleration, vessel);
				if(Vector3.Angle(aamTarget-transform.position, transform.forward) > maxOffBoresight*0.85f)
				{
					aamTarget = targetPosition;
				}
				CheckMiss();
			}
			else
			{
				aamTarget = transform.position + (20*vessel.srf_velocity.normalized);
			}


			if(Time.time-timeFired > dropTime+0.25f)
			{
				aeroTorque = MissileGuidance.DoAeroForces(this, aamTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, maxAoA);
			}

			//proxy detonation
			if((targetPosition-transform.position).sqrMagnitude < Mathf.Pow(blastRadius * 0.75f,2))
			{
				Detonate();
			}
		}

		void AGMGuidance()
		{
			if(BDArmorySettings.ALLOW_LEGACY_TARGETING && legacyTargetVessel)
			{
				UpdateLegacyTarget();
				lastLaserPoint = targetPosition;
			}
			else
			{
				if(targetingMode == TargetingModes.Laser)
				{
					if(targetAcquired)
					{
						if(lockedCamera && lockedCamera.groundStabilized && !lockedCamera.gimbalLimitReached && lockedCamera.surfaceDetected) //active laser target
						{
							targetPosition = lockedCamera.groundTargetPosition;
							//do target velocity info here
							lastLaserPoint = targetPosition;
						}
						else //lost active laser target, home on last known position
						{
							if(CMSmoke.RaycastSmoke(new Ray(transform.position, lastLaserPoint-transform.position)))
							{
								Debug.Log ("Laser missile affected by smoke countermeasure");
								float angle = VectorUtils.FullRangePerlinNoise(0.75f*Time.time, 10) * BDArmorySettings.SMOKE_DEFLECTION_FACTOR;
								targetPosition = VectorUtils.RotatePointAround(lastLaserPoint, transform.position, VectorUtils.GetUpDirection(transform.position), angle);
								lastLaserPoint = targetPosition;
							}
							else
							{
								targetPosition = lastLaserPoint;
							}
						}
					}
					else
					{
						ModuleTargetingCamera foundCam = null;
						foundCam = BDATargetManager.GetLaserTarget(this);
						if(foundCam != null && foundCam.cameraEnabled && foundCam.groundStabilized && CanSeePosition(foundCam.groundTargetPosition))
						{
							Debug.Log ("Laser guided missile actively found laser point. Enabling guidance.");
							lockedCamera = foundCam;
							targetAcquired = true;
						}
					}
				}
			}


			if(targetAcquired)
			{
				//lose lock if seeker reaches gimbal limit
				float targetViewAngle = Vector3.Angle (transform.forward, targetPosition-transform.position);
				
				if(targetViewAngle > maxOffBoresight)
				{
					Debug.Log ("AGM Missile guidance failed - target out of view");
					guidanceActive = false;
				}



				CheckMiss();
			}
			else
			{
				//keep going straight until found laser point
				targetPosition = laserStartPosition + (20000 * laserStartDirection);
			}

			Vector3 agmTarget = MissileGuidance.GetAirToGroundTarget(targetPosition, vessel, agmDescentRatio);

			aeroTorque = MissileGuidance.DoAeroForces(this, agmTarget, liftArea, controlAuthority * steerMult, aeroTorque, finalMaxTorque, maxAoA);
		}

		void UpdateLegacyTarget()
		{
			if(legacyTargetVessel)
			{
				targetAcquired = true;
				targetPosition = legacyTargetVessel.CoM + (legacyTargetVessel.rb_velocity*Time.fixedDeltaTime);
				targetVelocity = legacyTargetVessel.srf_velocity;
				targetAcceleration = legacyTargetVessel.acceleration;
			}
			else
			{
				targetAcquired = false;
			}
		}

		TargetSignatureData[] scannedTargets;
		void UpdateRadarTarget()
		{
			targetAcquired = false;
			if(scannedTargets == null) scannedTargets = new TargetSignatureData[5];
			TargetSignatureData.ResetTSDArray(ref scannedTargets);
			float angleToTarget = Vector3.Angle(radarTarget.position-transform.position,transform.forward);
			if(radarTarget.exists)
			{
				if((radarTarget.predictedPosition-transform.position).sqrMagnitude > Mathf.Pow(activeRadarRange, 2) || angleToTarget > maxOffBoresight*0.9f)
				{
					if(radar && radar.lockedTarget.exists)
					{
						targetAcquired = true;
						radarTarget = radar.lockedTarget;
						targetPosition = radarTarget.predictedPosition;
						targetVelocity = radarTarget.velocity;
						targetAcceleration = radarTarget.acceleration;
						//radarTarget.signalStrength = 
						return;
					}
					else
					{
						Debug.Log ("Radar guidance failed. Out of range and no data feed.");
						radarTarget = TargetSignatureData.noTarget;
						return;
					}
				}
				else
				{
					radar = null;

					if(angleToTarget > maxOffBoresight)
					{
						Debug.Log ("Radar guidance failed.  Target is out of active seeker gimbal limits.");
						radarTarget = TargetSignatureData.noTarget;
						return;
					}
					else
					{
						Ray ray = new Ray(transform.position,radarTarget.predictedPosition-transform.position);
						RadarUtils.ScanInDirection(ray, 10, 100, ref scannedTargets, 0);
						for(int i = 0; i < scannedTargets.Length; i++)
						{
							if(scannedTargets[i].exists && (scannedTargets[i].predictedPosition-radarTarget.position).sqrMagnitude < Mathf.Pow(20,2))
							{
								radarTarget = scannedTargets[i];
								targetAcquired = true;
								targetPosition = radarTarget.predictedPosition + (radarTarget.velocity*Time.fixedDeltaTime);
								targetVelocity = radarTarget.velocity;
								targetAcceleration = radarTarget.acceleration;

								if(!activeRadar)
								{
									activeRadar = true;
									Debug.Log ("Pitbull! Radar missile has gone active.  Radar sig strength: "+radarTarget.signalStrength.ToString("0.0"));
								}
								return;
							}
						}

					}
				}
			}
		}

		void UpdateHeatTarget()
		{
			targetAcquired = false;
			
			if(heatTarget.exists && heatFailTimer < 0)
			{
				heatFailTimer = 0;
			}
			if(heatFailTimer >= 0 && heatFailTimer < 1)
			{
				Ray lookRay = new Ray(transform.position, heatTarget.position+(heatTarget.velocity*Time.fixedDeltaTime)-transform.position);
				heatTarget = BDATargetManager.GetHeatTarget(lookRay, 20, heatThreshold);
				
				if(heatTarget.exists)
				{
					targetAcquired = true;
					targetPosition = heatTarget.position+(heatTarget.velocity*Time.fixedDeltaTime);
					targetVelocity = heatTarget.velocity;
					targetAcceleration = heatTarget.acceleration;
				}
				else
				{
					if(FlightGlobals.ready)
					{
						heatFailTimer += Time.fixedDeltaTime;
					}
				}
			}
		}

		void CheckMiss()
		{
			float sqrDist = (targetPosition-(transform.position+(part.rb.velocity*Time.fixedDeltaTime))).sqrMagnitude;
			if(sqrDist < 800*800 || MissileState == MissileStates.PostThrust)
			{
				checkMiss = true;	
			}
			
			//kill guidance if missile has missed
			if(checkMiss && 
			   (Vector3.Angle(targetPosition-transform.position, vessel.srf_velocity-targetVelocity) > 45)) 
			{
				Debug.Log ("Missile CheckMiss showed miss");
				guidanceActive = false;
				targetMf = null;
				if(hasRCS) KillRCS();
				if(sqrDist < Mathf.Pow(blastRadius*2f, 2)) Detonate();
				return;
			}
		}

		void DrawDebugLine(Vector3 start, Vector3 end)
		{
			if(BDArmorySettings.DRAW_DEBUG_LINES)
			{
				if(!gameObject.GetComponent<LineRenderer>())
				{
					LR = gameObject.AddComponent<LineRenderer>();
					LR.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
					LR.material.SetColor("_EmissiveColor", Color.red);
				}else
				{
					LR = gameObject.GetComponent<LineRenderer>();
				}
				LR.SetVertexCount(2);
				LR.SetPosition(0, start);
				LR.SetPosition(1, end);
			}
		}
		
		
		
		public void Detonate()
		{
			if(isSeismicCharge)
			{
				DetonateSeismicCharge();
			
			}
			else if(!hasExploded && hasFired)
			{
				BDArmorySettings.numberOfParticleEmitters--;
				
				hasExploded = true;

				
				if(targetVessel!=null)
				{
					foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
					{
						wpm.missileIsIncoming = false;
					}
				}
				
				if(part!=null) part.temperature = part.maxTemp + 100;
				Vector3 position = transform.position;//+rigidbody.velocity*Time.fixedDeltaTime;
				if(sourceVessel==null) sourceVessel = vessel;
				ExplosionFX.CreateExplosion(position, blastRadius, blastPower, sourceVessel, transform.forward, explModelPath, explSoundPath);
			}
		}



		public bool CanSeePosition(Vector3 pos)
		{
			if((pos-transform.position).sqrMagnitude < Mathf.Pow(20,2))
			{
				return false;
			}

			float dist = 10000;
			Ray ray = new Ray(transform.position, pos-transform.position);
			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, dist, 557057))
			{
				if((rayHit.point-pos).sqrMagnitude < 200)
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
		
		
		
		
		void DetonateSeismicCharge()
		{
			if(!hasExploded && hasFired)
			{
				GameSettings.SHIP_VOLUME = 0;
				GameSettings.MUSIC_VOLUME = 0;
				GameSettings.AMBIENCE_VOLUME = 0;
				
				BDArmorySettings.numberOfParticleEmitters--;
				
				hasExploded = true;

				/*
				if(targetVessel == null)
				{
					if(target!=null && FlightGlobals.ActiveVessel.gameObject == target)
					{
						targetVessel = FlightGlobals.ActiveVessel;
					}
					else if(target!=null && !BDArmorySettings.Flares.Contains(t => t.gameObject == target))
					{
						targetVessel = Part.FromGO(target).vessel;
					}
					
				}
				*/
				if(targetVessel!=null)
				{
					foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
					{
						wpm.missileIsIncoming = false;
					}
				}
				
				if(part!=null)
				{
					
					part.temperature = part.maxTemp + 100;
				}
				Vector3 position = transform.position+rigidbody.velocity*Time.fixedDeltaTime;
				if(sourceVessel==null) sourceVessel = vessel;
				
				SeismicChargeFX.CreateSeismicExplosion(transform.position-(rigidbody.velocity.normalized*15), UnityEngine.Random.rotation);
				
			}	
		}
		
		
		
		public static bool CheckIfMissile(Part p)
		{
			if(p.GetComponent<MissileLauncher>())
			{
				return true;
			}
			else return false;
		}

		/*
		void LookForCountermeasure(ref Vector3 flarePosition)
		{
			foreach(var flare in BDArmorySettings.Flares)
			{
				if(flare!=null)
				{
					float flareAcquireMaxRange = 2500;
					float chanceFactor = BDArmorySettings.FLARE_CHANCE_FACTOR;
					float chance = Mathf.Clamp(chanceFactor-(Vector3.Distance(flare.transform.position, transform.position)/(flareAcquireMaxRange/chanceFactor)), 0, chanceFactor);
					chance -= UnityEngine.Random.Range(0f, chance);
					bool chancePass = (flare.GetComponent<CMFlare>().acquireDice < chance);
					float angle = Vector3.Angle(transform.forward, flare.transform.position-transform.position);
					if(angle < 45 && (flare.transform.position-transform.position).sqrMagnitude < Mathf.Pow(flareAcquireMaxRange, 2) && chancePass && targetInView)
					{
						//Debug.Log ("=Missile deflected via flare=");
						//target = flare;
						flarePosition = flare.transform.position;
						return;
					}
				}
			}
			
			
		}
		*/
		
		void WarnTarget()
		{
			if(targetVessel == null)
			{
				return;
				/*
				if(FlightGlobals.ActiveVessel.gameObject == target)
				{
					targetVessel = FlightGlobals.ActiveVessel;
				}
				else if(target!=null && !BDArmorySettings.Flares.Contains(target))
				{
					targetVessel = Part.FromGO(target).vessel;
				}
				*/
				
			}
			
			if(targetVessel!=null)
			{
				foreach(var wpm in targetVessel.FindPartModulesImplementing<MissileFire>())
				{
					wpm.MissileWarning(Vector3.Distance(transform.position, targetVessel.transform.position), this);
					break;
				}
			}
		}


		float[] rcsFiredTimes;
		KSPParticleEmitter[] rcsTransforms;
		void SetupRCS()
		{
			rcsFiredTimes = new float[]{0,0,0,0};
			rcsTransforms = new KSPParticleEmitter[]{upRCS, leftRCS, rightRCS, downRCS};
		}



		void DoRCS()
		{
			for(int i = 0; i < 4; i++)
			{
				Vector3 relV = targetVessel.obt_velocity-vessel.obt_velocity;
				Vector3 localRelV = rcsTransforms[i].transform.InverseTransformPoint(relV + transform.position);


				float giveThrust = Mathf.Clamp(-localRelV.z, 0, rcsThrust);
				rigidbody.AddForce(-giveThrust*rcsTransforms[i].transform.forward);

				if(localRelV.z < -rcsRVelThreshold)
				{
					rcsAudioMinInterval = UnityEngine.Random.Range(0.15f,0.25f);
					if(Time.time-rcsFiredTimes[i] > rcsAudioMinInterval)
					{
						sfAudioSource.PlayOneShot(GameDatabase.Instance.GetAudioClip("BDArmory/Sounds/popThrust"));
						rcsTransforms[i].emit = true;
						rcsFiredTimes[i] = Time.time;
					}
				}
				else
				{
					rcsTransforms[i].emit = false;
				}

				//turn off emit
				if(Time.time-rcsFiredTimes[i] > rcsAudioMinInterval*0.75f)
				{
					rcsTransforms[i].emit = false;
				}
			}


		}
		
		void KillRCS()
		{
			upRCS.emit = false;
			downRCS.emit = false;
			leftRCS.emit = false;
			rightRCS.emit = false;
		}


		void OnGUI()
		{
			if(hasFired && BDArmorySettings.DRAW_DEBUG_LABELS)	
			{
				GUI.Label(new Rect(200,200,200,200), debugString);	
			}
		}


		void AntiSpin()
		{
			Vector3 spin = Vector3.Project(rigidbody.angularVelocity, rigidbody.transform.forward);// * 8 * Time.fixedDeltaTime;
			rigidbody.angularVelocity -= spin;
			//rigidbody.maxAngularVelocity = 7;
			rigidbody.angularVelocity -= 0.5f * rigidbody.angularVelocity;
		}
		
		void SimpleDrag()
		{
			float simSpeedSquared = rigidbody.velocity.sqrMagnitude;
			Vector3 currPos = transform.position;
			Vector3 dragForce = (0.008f * rigidbody.mass) * part.minimum_drag * 0.5f * simSpeedSquared * (float) FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(currPos), FlightGlobals.getExternalTemperature(), FlightGlobals.currentMainBody) * rigidbody.velocity.normalized;
			rigidbody.AddForceAtPosition(-dragForce, transform.TransformPoint(simpleCoD));

		}

		void ParseModes()
		{
			homingType = homingType.ToLower();
			switch(homingType)
			{
			case "aam":
				guidanceMode = GuidanceModes.AAMLead;
				break;
			case "aamlead":
				guidanceMode = GuidanceModes.AAMLead;
				break;
			case "aampure":
				guidanceMode = GuidanceModes.AAMPure;
				break;
			case "agm":
				guidanceMode = GuidanceModes.AGM;
				break;
			case "cruise":
				guidanceMode = GuidanceModes.Cruise;
				break;
			case "sts":
				guidanceMode = GuidanceModes.STS;
				break;
			case "rcs":
				guidanceMode = GuidanceModes.RCS;
				break;
			}

			targetingType = targetingType.ToLower();
			switch(targetingType)
			{
			case "radar":
				targetingMode = TargetingModes.Radar;
				break;
			case "heat":
				targetingMode = TargetingModes.Heat;
				break;
			case "laser":
				targetingMode = TargetingModes.Laser;
				break;
			case "gps":
				targetingMode = TargetingModes.GPS;
				break;
			case "antirad":
				targetingMode = TargetingModes.AntiRad;
				break;
			}
		}
		
	}
}

