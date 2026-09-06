using System.Text;
using ProtocolArena.Run;
using UnityEngine;

namespace ProtocolArena.UI
{
    internal static class UpgradePresentation
    {
        public static string BuildStats(RunBuild build)
        {
            return $"최대 체력          {build.MaxHealth:0}\n\n공격력             {build.AttackDamage:0.0}\n\n초당 공격          {1f / build.AttackCooldown:0.00}회\n\n이동 속도          {build.MoveSpeed:0.00}";
        }

        public static string BuildModules(RunBuild build, string emptyMessage)
        {
            if (build.Upgrades.Count == 0) return emptyMessage;
            var builder = new StringBuilder();
            for (int index = 0; index < build.Upgrades.Count; index++)
            {
                UpgradeOffer offer = build.Upgrades[index];
                builder.AppendLine($"{index + 1}.  {offer.Name}");
                builder.AppendLine($"    {offer.Description}");
            }

            if (build.HasBarrageSynergy) builder.AppendLine("<color=#D783FF>조합 발동 · 고속 탄막: 탄막이 세 번째 공격마다 발동</color>");
            if (build.HasCounterSynergy) builder.AppendLine("<color=#5DEBFF>조합 발동 · 중장 반격: 되돌리는 피해 50% 증가</color>");
            if (build.HasChargeSynergy) builder.AppendLine("<color=#FFB34D>조합 발동 · 가속 돌진포: 첫 공격 효과 50% 증가</color>");
            return builder.ToString();
        }

        public static string BuildBeforeAfter(RunBuild build, UpgradeOffer offer)
        {
            switch (offer.Id)
            {
                case UpgradeId.ReinforcedFrame:
                    return build.HasUpgrade(UpgradeId.CounterArmor)
                        ? $"체력  {build.MaxHealth:0}  →  {build.MaxHealth + offer.Magnitude:0}\n조합 완성 · 반사 피해 50% 증가"
                        : $"체력  {build.MaxHealth:0}  →  {build.MaxHealth + offer.Magnitude:0}";
                case UpgradeId.PowerCore:
                    return $"공격력  {build.AttackDamage:0.0}  →  {build.AttackDamage + offer.Magnitude:0.0}";
                case UpgradeId.OverclockServo:
                {
                    float afterCooldown = Mathf.Max(0.35f, build.AttackCooldown * (1f - offer.Magnitude));
                    string synergy = build.HasUpgrade(UpgradeId.BarrageAmplifier) ? "\n조합 완성 · 탄막 발동 주기 단축" : string.Empty;
                    return $"초당 공격  {1f / build.AttackCooldown:0.00}회  →  {1f / afterCooldown:0.00}회{synergy}";
                }
                case UpgradeId.ThrusterKit:
                {
                    string synergy = build.HasUpgrade(UpgradeId.ChargeCannon) ? "\n조합 완성 · 돌진포 피해 50% 증가" : string.Empty;
                    return $"이동 속도  {build.MoveSpeed:0.00}  →  {build.MoveSpeed * (1f + offer.Magnitude):0.00}{synergy}";
                }
                case UpgradeId.RepairProtocol:
                    return $"위기 시 체력 {offer.Magnitude:0} 자동 회복";
                case UpgradeId.BurstProtocol:
                    return $"세 번째 공격 피해 +{offer.Magnitude * 100f:0}%";
                case UpgradeId.BarrageAmplifier:
                    return build.HasUpgrade(UpgradeId.OverclockServo)
                        ? $"조합 완성 · 세 번째 공격마다 +{offer.Magnitude * 100f:0}%"
                        : $"네 번째 공격마다 피해 +{offer.Magnitude * 100f:0}%\n고속 장전기와 조합 가능";
                case UpgradeId.CounterArmor:
                    return build.HasUpgrade(UpgradeId.ReinforcedFrame)
                        ? $"조합 완성 · 받은 피해의 {offer.Magnitude * 150f:0}% 반사"
                        : $"받은 피해의 {offer.Magnitude * 100f:0}% 반사\n튼튼한 장갑과 조합 가능";
                case UpgradeId.ChargeCannon:
                    return build.HasUpgrade(UpgradeId.ThrusterKit)
                        ? $"조합 완성 · 첫 공격 피해 +{offer.Magnitude * 150f:0}%"
                        : $"첫 공격 피해 +{offer.Magnitude * 100f:0}%\n가속 장치와 조합 가능";
                default:
                    return string.Empty;
            }
        }

        public static string GetCategory(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.ReinforcedFrame:
                case UpgradeId.RepairProtocol:
                case UpgradeId.CounterArmor:
                    return "[ 생존 강화 ]";
                case UpgradeId.PowerCore:
                case UpgradeId.BurstProtocol:
                case UpgradeId.BarrageAmplifier:
                case UpgradeId.ChargeCannon:
                    return "[ 공격 강화 ]";
                case UpgradeId.OverclockServo:
                case UpgradeId.ThrusterKit:
                    return "[ 속도 강화 ]";
                default:
                    return "[ 강화 ]";
            }
        }

        public static Color GetColor(UpgradeId id)
        {
            switch (id)
            {
                case UpgradeId.ReinforcedFrame:
                case UpgradeId.RepairProtocol:
                case UpgradeId.CounterArmor:
                    return new Color(0.13f, 0.68f, 0.8f, 1f);
                case UpgradeId.PowerCore:
                case UpgradeId.BurstProtocol:
                case UpgradeId.BarrageAmplifier:
                case UpgradeId.ChargeCannon:
                    return new Color(0.92f, 0.37f, 0.18f, 1f);
                default:
                    return new Color(0.55f, 0.35f, 0.9f, 1f);
            }
        }
    }
}
