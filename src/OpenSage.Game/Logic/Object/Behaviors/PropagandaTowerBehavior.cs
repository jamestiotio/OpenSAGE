﻿using System.Collections.Generic;
using OpenSage.Data.Ini;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public sealed class PropagandaTowerBehavior : UpdateModule
    {
        private uint _unknownFrame;
        private readonly List<uint> _objectIds = new();

        internal override void Load(StatePersister reader)
        {
            reader.PersistVersion(1);

            base.Load(reader);

            reader.PersistFrame(ref _unknownFrame);

            reader.PersistList(
                _objectIds,
                static (StatePersister persister, ref uint item) =>
                {
                    persister.PersistObjectIDValue(ref item);
                });
        }
    }

    public sealed class PropagandaTowerBehaviorModuleData : BehaviorModuleData
    {
        internal static PropagandaTowerBehaviorModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<PropagandaTowerBehaviorModuleData> FieldParseTable = new IniParseTable<PropagandaTowerBehaviorModuleData>
        {
            { "Radius", (parser, x) => x.Radius = parser.ParseFloat() },
            { "DelayBetweenUpdates", (parser, x) => x.DelayBetweenUpdates = parser.ParseInteger() },
            { "HealPercentEachSecond", (parser, x) => x.HealPercentEachSecond = parser.ParsePercentage() },
            { "PulseFX", (parser, x) => x.PulseFX = parser.ParseAssetReference() },
            { "UpgradeRequired", (parser, x) => x.UpgradeRequired = parser.ParseAssetReference() },
            { "UpgradedHealPercentEachSecond", (parser, x) => x.UpgradedHealPercentEachSecond = parser.ParsePercentage() },
            { "UpgradedPulseFX", (parser, x) => x.UpgradedPulseFX = parser.ParseAssetReference() },
            { "AffectsSelf", (parser, x) => x.AffectsSelf = parser.ParseBoolean() },
        };

        public float Radius { get; private set; }
        public int DelayBetweenUpdates { get; private set; }
        public Percentage HealPercentEachSecond { get; private set; }
        public string PulseFX { get; private set; }
        public string UpgradeRequired { get; private set; }
        public Percentage UpgradedHealPercentEachSecond { get; private set; }
        public string UpgradedPulseFX { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool AffectsSelf { get; private set; }

        internal override BehaviorModule CreateModule(GameObject gameObject, GameContext context)
        {
            return new PropagandaTowerBehavior();
        }
    }
}
