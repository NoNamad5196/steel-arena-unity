using NUnit.Framework;
using ProtocolArena.Battle;
using ProtocolArena.Run;

namespace ProtocolArena.Tests
{
    public sealed class AutoBattleSimulationTests
    {
        [Test]
        public void UpgradedBuild_AutoBattleAlwaysReachesResult()
        {
            var build = new RunBuild();
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.PowerCore, 1f));
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.RepairProtocol, 1f));
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.BurstProtocol, 1f));
            var simulation = new AutoBattleSimulation();
            BattleOutcome? result = null;
            int eventCount = 0;
            simulation.EventRaised += _ => eventCount++;
            simulation.BattleEnded += outcome => result = outcome;

            simulation.Start(build);
            for (int step = 0; step < 2000 && simulation.IsRunning; step++)
            {
                simulation.Tick(0.05f);
            }

            Assert.That(simulation.IsRunning, Is.False);
            Assert.That(result, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(eventCount, Is.GreaterThan(0));
            Assert.That(simulation.TotalPlayerDamage, Is.GreaterThan(0f));
        }

        [Test]
        public void BaseBuild_LosesToArenaUnit()
        {
            var simulation = new AutoBattleSimulation();
            BattleOutcome? result = null;
            simulation.BattleEnded += outcome => result = outcome;

            simulation.Start(new RunBuild());
            for (int step = 0; step < 2000 && simulation.IsRunning; step++)
            {
                simulation.Tick(0.05f);
            }

            Assert.That(result, Is.EqualTo(BattleOutcome.Defeat));
        }

        [Test]
        public void BarrageAndOverclock_TriggerCombinedAttack()
        {
            var build = new RunBuild();
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.OverclockServo, 1f));
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.BarrageAmplifier, 1f));
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.PowerCore, 1f));
            var simulation = new AutoBattleSimulation();
            bool barrageRaised = false;
            simulation.EventRaised += combatEvent => barrageRaised |= combatEvent.Type == CombatEventType.PlayerBarrage;

            simulation.Start(build, EnemyCatalog.Get(EnemyKind.RushUnit));
            for (int step = 0; step < 2000 && simulation.IsRunning; step++) simulation.Tick(0.05f);

            Assert.That(barrageRaised, Is.True);
            Assert.That(simulation.SynergyTriggers, Is.GreaterThan(0));
        }

        [Test]
        public void EnemyProfiles_HaveClearlyDifferentCombatStats()
        {
            EnemyProfile rush = EnemyCatalog.Get(EnemyKind.RushUnit);
            EnemyProfile boss = EnemyCatalog.Get(EnemyKind.CrusherBoss);

            Assert.That(rush.MoveSpeed, Is.GreaterThan(boss.MoveSpeed));
            Assert.That(boss.MaxHealth, Is.GreaterThan(rush.MaxHealth));
            Assert.That(boss.HeavyAttackInterval, Is.LessThan(rush.HeavyAttackInterval));
        }
    }
}
