﻿using System.Numerics;
using OpenSage.Data.Ini;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public sealed class TunnelContain : OpenContainModule
    {
        public override int TotalSlots => GameObject.GameContext.Game.AssetStore.GameData.Current.MaxTunnelCapacity;

        private readonly TunnelContainModuleData _moduleData;
        private bool _unknown1;
        private bool _unknown2;

        // todo: containedObjectIds should be shared

        public TunnelContain(GameObject gameObject, TunnelContainModuleData moduleData) : base(gameObject, moduleData)
        {
            _moduleData = moduleData;
        }

        private protected override void UpdateModuleSpecific(BehaviorUpdateContext context)
        {
            HealUnits(_moduleData.TimeForFullHeal);
        }

        protected override bool TryAssignExitPath(GameObject unit)
        {
            if (!GameObject.RallyPoint.HasValue)
            {
                // todo: natural rally point?
                return false;
            }

            unit.UpdateTransform(GameObject.Transform.Translation, GameObject.Transform.Rotation);
            var startPoint = GameObject.Transform;
            unit.UpdateTransform(startPoint.Translation, startPoint.Rotation);
            var exitPoint = GameObject.RallyPoint.Value;
            unit.AIUpdate.AddTargetPoint(exitPoint);

            return true;
        }

        internal override void Load(StatePersister reader)
        {
            reader.PersistVersion(1);

            base.Load(reader);

            reader.PersistBoolean(ref _unknown1);
            reader.PersistBoolean(ref _unknown2);
        }
    }

    /// <summary>
    /// Tunnel contain limit is special case global logic defined by
    /// <see cref="GameData.MaxTunnelCapacity"/> in GameData.INI and allows the use of
    /// <see cref="ObjectDefinition.SoundEnter"/> and <see cref="ObjectDefinition.SoundExit"/>.
    /// </summary>
    public sealed class TunnelContainModuleData : GarrisonContainModuleData
    {
        internal static new TunnelContainModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static new readonly IniParseTable<TunnelContainModuleData> FieldParseTable = GarrisonContainModuleData.FieldParseTable
            .Concat(new IniParseTable<TunnelContainModuleData>
            {
                { "TimeForFullHeal", (parser, x) => x.TimeForFullHeal = parser.ParseInteger() },
                { "PassengerBonePrefix", (parser, x) => x.PassengerBonePrefix = PassengerBonePrefix.Parse(parser) },
                { "EntryPosition", (parser, x) => x.EntryPosition = parser.ParseVector3() },
                { "EntryOffset", (parser, x) => x.EntryOffset = parser.ParseVector3() },
                { "ExitOffset", (parser, x) => x.ExitOffset = parser.ParseVector3() },
                { "KillPassengersOnDeath", (parser, x) => x.KillPassengersOnDeath = parser.ParseBoolean() },
                { "ShowPips", (parser, x) => x.ShowPips = parser.ParseBoolean() },
                { "ExitDelay", (parser, x) => x.ExitDelay = parser.ParseInteger() },
                { "AllowOwnPlayerInsideOverride", (parser, x) => x.AllowOwnPlayerInsideOverride = parser.ParseBoolean() },
            });

        /// <summary>
        /// AllowInsideKindOf is never explicitly set for TunnelContain, but it seems to only be for infantry and vehicles?
        /// </summary>
        public override BitArray<ObjectKinds> AllowInsideKindOf { get; protected set; } = new(ObjectKinds.Infantry, ObjectKinds.Vehicle);

        public int TimeForFullHeal { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public PassengerBonePrefix PassengerBonePrefix { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public Vector3 EntryPosition { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public Vector3 EntryOffset { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public Vector3 ExitOffset { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool KillPassengersOnDeath { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool ShowPips { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public int ExitDelay { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool AllowOwnPlayerInsideOverride { get; private set; }

        internal override BehaviorModule CreateModule(GameObject gameObject, GameContext context)
        {
            return new TunnelContain(gameObject, this);
        }
    }
}
