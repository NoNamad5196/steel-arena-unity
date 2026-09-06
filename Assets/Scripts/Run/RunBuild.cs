using System;
using System.Collections.Generic;

namespace ProtocolArena.Run
{
    public enum UpgradeId
    {
        ReinforcedFrame,
        PowerCore,
        OverclockServo,
        ThrusterKit,
        RepairProtocol,
        BurstProtocol,
        BarrageAmplifier,
        CounterArmor,
        ChargeCannon
    }

    public readonly struct UpgradeOffer
    {
        public UpgradeOffer(UpgradeId id, string name, string description, float magnitude)
        {
            Id = id;
            Name = name;
            Description = description;
            Magnitude = magnitude;
        }

        public UpgradeId Id { get; }
        public string Name { get; }
        public string Description { get; }
        public float Magnitude { get; }
    }

    public sealed class RunBuild
    {
        private readonly List<UpgradeOffer> upgrades = new List<UpgradeOffer>();

        public float MaxHealth { get; private set; } = 100f;
        public float AttackDamage { get; private set; } = 11f;
        public float AttackCooldown { get; private set; } = 1.05f;
        public float MoveSpeed { get; private set; } = 1.8f;
        public float RepairAmount { get; private set; }
        public float BurstBonus { get; private set; }
        public float BarrageBonus { get; private set; }
        public float CounterRatio { get; private set; }
        public float ChargeBonus { get; private set; }
        public IReadOnlyList<UpgradeOffer> Upgrades => upgrades;
        public bool HasBarrageSynergy => HasUpgrade(UpgradeId.BarrageAmplifier) && HasUpgrade(UpgradeId.OverclockServo);
        public bool HasCounterSynergy => HasUpgrade(UpgradeId.CounterArmor) && HasUpgrade(UpgradeId.ReinforcedFrame);
        public bool HasChargeSynergy => HasUpgrade(UpgradeId.ChargeCannon) && HasUpgrade(UpgradeId.ThrusterKit);

        public void Apply(UpgradeOffer offer)
        {
            if (HasUpgrade(offer.Id))
            {
                throw new InvalidOperationException($"Upgrade {offer.Id} has already been acquired.");
            }

            switch (offer.Id)
            {
                case UpgradeId.ReinforcedFrame:
                    MaxHealth += offer.Magnitude;
                    break;
                case UpgradeId.PowerCore:
                    AttackDamage += offer.Magnitude;
                    break;
                case UpgradeId.OverclockServo:
                    AttackCooldown = Math.Max(0.35f, AttackCooldown * (1f - offer.Magnitude));
                    break;
                case UpgradeId.ThrusterKit:
                    MoveSpeed *= 1f + offer.Magnitude;
                    break;
                case UpgradeId.RepairProtocol:
                    RepairAmount = offer.Magnitude;
                    break;
                case UpgradeId.BurstProtocol:
                    BurstBonus = offer.Magnitude;
                    break;
                case UpgradeId.BarrageAmplifier:
                    BarrageBonus = offer.Magnitude;
                    break;
                case UpgradeId.CounterArmor:
                    CounterRatio = offer.Magnitude;
                    break;
                case UpgradeId.ChargeCannon:
                    ChargeBonus = offer.Magnitude;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(offer));
            }

            upgrades.Add(offer);
        }

        public bool HasUpgrade(UpgradeId id)
        {
            for (int index = 0; index < upgrades.Count; index++)
            {
                if (upgrades[index].Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
