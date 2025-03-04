﻿using System;
using System.Collections.Generic;
using System.Numerics;
using OpenSage.Data.Ini;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public sealed class JetAIUpdate : AIUpdate
    {
        public GameObject Base;

        private readonly JetAIUpdateModuleData _moduleData;
        private Vector3 _currentTargetPoint;
        private bool _unparkingRequested;

        private Queue<string> _pathToStart;
        private Queue<string> _pathToParking;
        private string _currentTaxiingTarget;

        public JetAIState CurrentJetAIState;

        private LogicFrame _waitUntil;

        private bool _afterburnerEnabled;

        private Vector3 _positionSomething;
        private uint _unknownInt1;
        private uint _unknownInt2;
        private bool _unknownBool1;
        private uint _unknownInt3;
        private bool _unknownBool2;
        private uint _unknownInt4;
        private bool _unknownBool3;
        private bool _unknownBool4;
        private uint _unknownInt5;
        private uint _unknownInt6;
        private uint _unknownInt7;
        private uint _unknownInt8;
        private uint _unknownInt9;
        private uint _unknownInt10;
        private uint _unknownInt11;
        private bool _unknownBool5;

        public enum JetAIState
        {
            JustCreated, //TODO: better name
            MovingToParkingPlace,
            ReachedParkingPlace,
            Rotating, //TODO: better name
            Parked,
            MovingTowardsStart,
            PreparingStart,
            Starting,
            Started,
            MovingTowardsTarget,
            ReachedTargetPoint,
            ReturningToBase,
            MovingBackToHangar
        }

        internal JetAIUpdate(GameObject gameObject, JetAIUpdateModuleData moduleData)
            : base(gameObject, moduleData)
        {
            _moduleData = moduleData;
            CurrentJetAIState = JetAIState.Parked;
        }

        internal override void Load(StatePersister reader)
        {
            reader.PersistVersion(2);
            reader.BeginObject("Base");
            base.Load(reader);
            reader.EndObject();

            reader.PersistVector3(ref _positionSomething);
            reader.PersistUInt32(ref _unknownInt1); // 11
            reader.PersistUInt32(ref _unknownInt2); // 11

            reader.SkipUnknownBytes(12);

            reader.PersistBoolean(ref _unknownBool1);

            reader.SkipUnknownBytes(12);

            reader.PersistUInt32(ref _unknownInt3);
            if (_unknownInt3 != 0x7FFFFFFF)
            {
                throw new InvalidStateException();
            }

            reader.PersistBoolean(ref _unknownBool2);

            reader.PersistUInt32(ref _unknownInt4);
            if (_unknownInt4 != 0x7FFFFFFF)
            {
                throw new InvalidStateException();
            }

            reader.PersistBoolean(ref _unknownBool3);
            reader.PersistBoolean(ref _unknownBool4);

            reader.SkipUnknownBytes(18);

            reader.PersistUInt32(ref _unknownInt5); // 1

            reader.SkipUnknownBytes(8);

            reader.PersistUInt32(ref _unknownInt6); // 255, probably frameSomething
            reader.PersistUInt32(ref _unknownInt7); // 255, probably frameSomething
            reader.PersistUInt32(ref _unknownInt8); // 0
            reader.PersistUInt32(ref _unknownInt9); // 1
            reader.PersistUInt32(ref _unknownInt10); // 0
            reader.PersistUInt32(ref _unknownInt11); // 6

            reader.PersistBoolean(ref _unknownBool5);
        }

        internal override void SetTargetPoint(Vector3 targetPoint)
        {
            if (_moduleData.KeepsParkingSpaceWhenAirborne) // check if not a helicopter
            {
                switch (CurrentJetAIState)
                {
                    case JetAIState.JustCreated:
                    case JetAIState.MovingToParkingPlace:
                    case JetAIState.ReachedParkingPlace:
                    case JetAIState.Rotating:
                    case JetAIState.Parked:
                    case JetAIState.MovingTowardsStart:
                    case JetAIState.Starting:
                    case JetAIState.MovingBackToHangar:
                        _currentTargetPoint = targetPoint;
                        _unparkingRequested = true;
                        return;
                    case JetAIState.Started:
                    case JetAIState.MovingTowardsTarget:
                    case JetAIState.ReachedTargetPoint:
                    case JetAIState.ReturningToBase:
                        CurrentJetAIState = JetAIState.MovingTowardsTarget;
                        break;
                }
            }
            base.SetTargetPoint(targetPoint);
        }

        internal override void Update(BehaviorUpdateContext context)
        {
            base.Update(context);

            if (!_moduleData.KeepsParkingSpaceWhenAirborne)
            {
                return; // helicopters are way more simple (at least for now)
            }

            if (Base == null)
            {
                // fix for mapObject aircrafts etc (e.g. ZH shellmap)
                // TODO: handle scenario of a destroyed airfield
                return;
            }

            var parkingPlaceBehavior = Base.FindBehavior<ParkingPlaceBehaviour>();

            var isMoving = GameObject.ModelConditionFlags.Get(ModelConditionFlag.Moving);

            var trans = GameObject.Translation;

            var terrainHeight = context.GameContext.Terrain.HeightMap.GetHeight(trans.X, trans.Y);

            switch (CurrentJetAIState)
            {
                case JetAIState.JustCreated:
                    var parkingTransform = parkingPlaceBehavior.GetParkingTransform(GameObject);
                    var parkingOffset = Vector4.Transform(new Vector4(_moduleData.ParkingOffset, 0, 0, 1),
                        parkingTransform.Rotation).ToVector3();
                    base.SetTargetPoint(Base.ToWorldspace(parkingTransform.Translation + parkingOffset));
                    CurrentJetAIState = JetAIState.MovingToParkingPlace;
                    break;

                case JetAIState.MovingToParkingPlace:
                    if (isMoving)
                    {
                        break;
                    }

                    CurrentJetAIState = JetAIState.ReachedParkingPlace;
                    break;

                case JetAIState.ReachedParkingPlace:
                    var createTransform = Base.ToWorldspace(parkingPlaceBehavior.GetUnitCreateTransform(GameObject));
                    SetTargetDirection(createTransform.LookDirection);
                    CurrentJetAIState = JetAIState.Rotating;
                    break;

                case JetAIState.Rotating:
                    if (isMoving)
                    {
                        break;
                    }

                    //base.SetTargetPoint(GameObject.Transform.Translation + GameObject.Transform.LookDirection * _moduleData.ParkingOffset);
                    CurrentJetAIState = JetAIState.Parked;
                    break;

                case JetAIState.Parked:
                    if (_unparkingRequested)
                    {
                        _pathToStart = parkingPlaceBehavior.GetPathToStart(GameObject);
                        CurrentJetAIState = JetAIState.MovingTowardsStart;
                        _unparkingRequested = false;
                    }
                    break;

                case JetAIState.MovingTowardsStart:
                    if (isMoving || ProcessWaypointPath(context, parkingPlaceBehavior, _pathToStart))
                    {
                        break;
                    }

                    CurrentJetAIState = JetAIState.PreparingStart;
                    _waitUntil = context.LogicFrame + _moduleData.TakeoffPause;
                    break;

                case JetAIState.PreparingStart:
                    if (context.LogicFrame < _waitUntil)
                    {
                        break;
                    }

                    SetLocomotor(LocomotorSetType.Normal);
                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.JetExhaust, true);
                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.JetAfterburner, true);
                    _afterburnerEnabled = true;
                    var endPointPosition =
                        Base.ToWorldspace(parkingPlaceBehavior.GetRunwayEndPoint(GameObject));
                    base.SetTargetPoint(endPointPosition);
                    AddTargetPoint(_currentTargetPoint);
                    CurrentJetAIState = JetAIState.Starting;
                    CurrentLocomotor.LiftFactor = 0;
                    break;

                case JetAIState.Starting:
                    var speedPercentage = GameObject.Speed / CurrentLocomotor.GetSpeed();
                    CurrentLocomotor.LiftFactor = speedPercentage;

                    if (speedPercentage < _moduleData.TakeoffSpeedForMaxLift)
                    {
                        break;
                    }

                    CurrentJetAIState = JetAIState.MovingTowardsTarget;
                    break;

                case JetAIState.MovingTowardsTarget:
                    if (isMoving)
                    {
                        if (_afterburnerEnabled && trans.Z - terrainHeight >= CurrentLocomotor.GetLocomotorValue(_ => _.PreferredHeight))
                        {
                            GameObject.ModelConditionFlags.Set(ModelConditionFlag.JetAfterburner, false);
                            _afterburnerEnabled = false;
                        }
                        break;
                    }
                    CurrentJetAIState = JetAIState.ReachedTargetPoint;
                    _waitUntil = context.LogicFrame + _moduleData.ReturnToBaseIdleTime;
                    break;

                case JetAIState.ReachedTargetPoint:
                    if (context.LogicFrame < _waitUntil)
                    {
                        break;
                    }

                    var endPosition = Base.ToWorldspace(parkingPlaceBehavior.GetRunwayEndPoint(GameObject));

                    base.SetTargetPoint(endPosition);
                    CurrentJetAIState = JetAIState.ReturningToBase;
                    break;

                case JetAIState.ReturningToBase:
                    if (isMoving)
                    {
                        break;
                    }

                    GameObject.ModelConditionFlags.Set(ModelConditionFlag.JetExhaust, false);
                    CurrentJetAIState = JetAIState.MovingBackToHangar;
                    SetLocomotor(LocomotorSetType.Taxiing);
                    _pathToParking = parkingPlaceBehavior.GetPathToHangar(GameObject);
                    break;

                case JetAIState.MovingBackToHangar:
                    if (isMoving || ProcessWaypointPath(context, parkingPlaceBehavior, _pathToParking))
                    {
                        break;
                    }

                    CurrentJetAIState = JetAIState.JustCreated;
                    break;

            }

            if (trans.Z - terrainHeight < _moduleData.MinHeight)
            {
                trans.Z = terrainHeight + _moduleData.MinHeight;
                GameObject.SetTranslation(trans);
            }

            if (GameObject.ModelConditionFlags.Get(ModelConditionFlag.Dying))
            {
                parkingPlaceBehavior.ClearObjectFromSlot(GameObject);
                Base.ProductionUpdate?.CloseDoor(parkingPlaceBehavior.GetCorrespondingSlot(GameObject));
            }
        }

        private bool ProcessWaypointPath(BehaviorUpdateContext context, ParkingPlaceBehaviour parkingPlaceBehavior, Queue<string> path)
        {
            if (_currentTaxiingTarget != null)
            {
                parkingPlaceBehavior.SetTaxiingPointBlocked(_currentTaxiingTarget, false);
            }

            if (path.Count > 0)
            {
                var nextPoint = path.Peek();
                if (parkingPlaceBehavior.IsTaxiingPointBlocked(nextPoint))
                {
                    _waitUntil = context.LogicFrame + _moduleData.TakeoffPause;
                    return true;
                }
                if (context.LogicFrame < _waitUntil)
                {
                    return true;
                }

                _currentTaxiingTarget = nextPoint;
                parkingPlaceBehavior.SetTaxiingPointBlocked(nextPoint, true);
                base.SetTargetPoint(Base.ToWorldspace(parkingPlaceBehavior.GetBoneTranslation(path.Dequeue())));
                return true;
            }

            return false;
        }
    }


    /// <summary>
    /// Allows the use of VoiceLowFuel and Afterburner within UnitSpecificSounds section of the object.
    /// Requires Kindof = AIRCRAFT.
    /// Allows the use of JETEXHAUST JETAFTERBURNER model condition states; this is triggered when
    /// it's taking off from the runway.
    /// </summary>
    public sealed class JetAIUpdateModuleData : AIUpdateModuleData
    {
        internal new static JetAIUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private new static readonly IniParseTable<JetAIUpdateModuleData> FieldParseTable = AIUpdateModuleData.FieldParseTable
            .Concat(new IniParseTable<JetAIUpdateModuleData>
            {
                { "OutOfAmmoDamagePerSecond", (parser, x) => x.OutOfAmmoDamagePerSecond = parser.ParsePercentage() },
                { "TakeoffSpeedForMaxLift", (parser, x) => x.TakeoffSpeedForMaxLift = parser.ParsePercentage() },
                { "TakeoffDistForMaxLift", (parser, x) => x.TakeoffDistForMaxLift = parser.ParsePercentage() },
                { "TakeoffPause", (parser, x) => x.TakeoffPause = parser.ParseTimeMillisecondsToLogicFrames() },
                { "MinHeight", (parser, x) => x.MinHeight = parser.ParseInteger() },
                { "NeedsRunway", (parser, x) => x.NeedsRunway = parser.ParseBoolean() },
                { "KeepsParkingSpaceWhenAirborne", (parser, x) => x.KeepsParkingSpaceWhenAirborne = parser.ParseBoolean() },
                { "SneakyOffsetWhenAttacking", (parser, x) => x.SneakyOffsetWhenAttacking = parser.ParseFloat() },
                { "AttackLocomotorType", (parser, x) => x.AttackLocomotorType = parser.ParseEnum<LocomotorSetType>() },
                { "AttackLocomotorPersistTime", (parser, x) => x.AttackLocomotorPersistTime = parser.ParseInteger() },
                { "AttackersMissPersistTime", (parser, x) => x.AttackersMissPersistTime = parser.ParseInteger() },
                { "ReturnForAmmoLocomotorType", (parser, x) => x.ReturnForAmmoLocomotorType = parser.ParseEnum<LocomotorSetType>() },
                { "ParkingOffset", (parser, x) => x.ParkingOffset = parser.ParseInteger() },
                { "ReturnToBaseIdleTime", (parser, x) => x.ReturnToBaseIdleTime = parser.ParseTimeMillisecondsToLogicFrames() },
            });

        /// <summary>
        /// Amount of damage, as a percentage of max health, to take per second when out of ammo.
        /// </summary>
        public Percentage OutOfAmmoDamagePerSecond { get; private set; }
        /// <summary>
        /// smaller numbers give more lift sooner when taking off
        /// </summary>
        public Percentage TakeoffSpeedForMaxLift { get; private set; }
        public LogicFrameSpan TakeoffPause { get; private set; }
        public int MinHeight { get; private set; }

        /// <summary>
        /// comanche (helicopter) does not need a runway
        /// </summary>
        public bool NeedsRunway { get; private set; } = true;

        /// <summary>
        /// comanche (helicopter) does not keep its parking space
        /// </summary>
        public bool KeepsParkingSpaceWhenAirborne { get; private set; } = true;
        /// <summary>
        /// this is how far behind us people aim when we are in attack mode
        /// </summary>
        public float SneakyOffsetWhenAttacking { get; private set; }
        public LocomotorSetType AttackLocomotorType { get; private set; }
        /// <summary>
        /// we start slowing down almost immediately
        /// </summary>
        public int AttackLocomotorPersistTime { get; private set; }
        /// <summary>
        /// but remain untargetable fer a bit longer
        /// </summary>
        public int AttackersMissPersistTime { get; private set; }
        public LocomotorSetType ReturnForAmmoLocomotorType { get; private set; }
        /// <summary>
        /// scooch it a little forward so the tail doesn't hit the doors
        /// </summary>
        public int ParkingOffset { get; private set; }
        /// <summary>
        /// if idle for this long, return to base, even if not out of ammo
        /// </summary>
        public LogicFrameSpan ReturnToBaseIdleTime { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public Percentage TakeoffDistForMaxLift { get; private set; }

        internal override BehaviorModule CreateModule(GameObject gameObject, GameContext context)
        {
            return new JetAIUpdate(gameObject, this);
        }
    }
}
