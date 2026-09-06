using System;
using System.Collections.Generic;

namespace ProtocolArena.Run
{
    public static class UpgradeCatalog
    {
        private static readonly UpgradeId[] AllUpgrades =
        {
            UpgradeId.ReinforcedFrame,
            UpgradeId.PowerCore,
            UpgradeId.OverclockServo,
            UpgradeId.ThrusterKit,
            UpgradeId.RepairProtocol,
            UpgradeId.BurstProtocol,
            UpgradeId.BarrageAmplifier,
            UpgradeId.CounterArmor,
            UpgradeId.ChargeCannon
        };

        public static List<UpgradeOffer> DraftThree(RunBuild build, float calibrationQuality, Random random)
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var available = new List<UpgradeId>();
            for (int index = 0; index < AllUpgrades.Length; index++)
            {
                if (!build.HasUpgrade(AllUpgrades[index]))
                {
                    available.Add(AllUpgrades[index]);
                }
            }

            int offerCount = Math.Min(3, available.Count);
            var result = new List<UpgradeOffer>(offerCount);
            var synergyCandidates = new List<UpgradeId>();
            AddMissingPartner(build, available, synergyCandidates, UpgradeId.OverclockServo, UpgradeId.BarrageAmplifier);
            AddMissingPartner(build, available, synergyCandidates, UpgradeId.ReinforcedFrame, UpgradeId.CounterArmor);
            AddMissingPartner(build, available, synergyCandidates, UpgradeId.ThrusterKit, UpgradeId.ChargeCannon);
            if (synergyCandidates.Count > 0)
            {
                UpgradeId partner = synergyCandidates[random.Next(synergyCandidates.Count)];
                result.Add(CreateOffer(partner, calibrationQuality));
                available.Remove(partner);
            }

            while (result.Count < offerCount)
            {
                int pickedIndex = random.Next(available.Count);
                result.Add(CreateOffer(available[pickedIndex], calibrationQuality));
                available.RemoveAt(pickedIndex);
            }

            return result;
        }

        public static UpgradeOffer CreateOffer(UpgradeId id, float calibrationQuality)
        {
            float quality = Math.Max(0.8f, Math.Min(1.4f, calibrationQuality));
            switch (id)
            {
                case UpgradeId.ReinforcedFrame:
                {
                    float value = Round(24f * quality);
                    return new UpgradeOffer(id, "튼튼한 장갑", $"최대 체력 +{value:0}", value);
                }
                case UpgradeId.PowerCore:
                {
                    float value = Round(3.5f * quality);
                    return new UpgradeOffer(id, "강화 무기", $"공격력 +{value:0.0}", value);
                }
                case UpgradeId.OverclockServo:
                {
                    float value = Round(0.13f * quality);
                    return new UpgradeOffer(id, "고속 장전기", $"공격 속도 +{value * 100f:0}%", value);
                }
                case UpgradeId.ThrusterKit:
                {
                    float value = Round(0.22f * quality);
                    return new UpgradeOffer(id, "가속 장치", $"이동 속도 +{value * 100f:0}%", value);
                }
                case UpgradeId.RepairProtocol:
                {
                    float value = Round(22f * quality);
                    return new UpgradeOffer(id, "비상 수리", $"체력이 35% 이하일 때 한 번 {value:0} 회복", value);
                }
                case UpgradeId.BurstProtocol:
                {
                    float value = Round(0.65f * quality);
                    return new UpgradeOffer(id, "연속 공격", $"세 번째 공격마다 피해량 +{value * 100f:0}%", value);
                }
                case UpgradeId.BarrageAmplifier:
                {
                    float value = Round(0.42f * quality);
                    return new UpgradeOffer(id, "탄막 증폭기", $"네 번째 공격마다 피해량 +{value * 100f:0}%", value);
                }
                case UpgradeId.CounterArmor:
                {
                    float value = Round(0.2f * quality);
                    return new UpgradeOffer(id, "반격 장갑", $"받은 피해의 {value * 100f:0}%를 적에게 되돌림", value);
                }
                case UpgradeId.ChargeCannon:
                {
                    float value = Round(0.7f * quality);
                    return new UpgradeOffer(id, "돌진포", $"전투 첫 공격의 피해량 +{value * 100f:0}%", value);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        private static float Round(float value)
        {
            return (float)Math.Round(value, 2);
        }

        private static void AddMissingPartner(RunBuild build, List<UpgradeId> available, List<UpgradeId> candidates, UpgradeId first, UpgradeId second)
        {
            if (build.HasUpgrade(first) && available.Contains(second)) candidates.Add(second);
            else if (build.HasUpgrade(second) && available.Contains(first)) candidates.Add(first);
        }
    }
}
