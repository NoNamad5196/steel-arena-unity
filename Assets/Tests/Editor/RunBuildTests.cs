using System;
using NUnit.Framework;
using ProtocolArena.Run;

namespace ProtocolArena.Tests
{
    public sealed class RunBuildTests
    {
        [Test]
        public void DraftThree_ReturnsDistinctUnownedRewards()
        {
            var build = new RunBuild();
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.PowerCore, 1f));

            var offers = UpgradeCatalog.DraftThree(build, 1f, new Random(10));

            Assert.That(offers, Has.Count.EqualTo(3));
            Assert.That(offers[0].Id, Is.Not.EqualTo(offers[1].Id));
            Assert.That(offers[0].Id, Is.Not.EqualTo(offers[2].Id));
            Assert.That(offers[1].Id, Is.Not.EqualTo(offers[2].Id));
            Assert.That(offers.Exists(offer => offer.Id == UpgradeId.PowerCore), Is.False);
        }

        [Test]
        public void ApplyUpgrade_ImmediatelyChangesBuildStat()
        {
            var build = new RunBuild();
            float before = build.AttackDamage;
            UpgradeOffer offer = UpgradeCatalog.CreateOffer(UpgradeId.PowerCore, 1.2f);

            build.Apply(offer);

            Assert.That(build.AttackDamage, Is.GreaterThan(before));
            Assert.That(build.Upgrades, Has.Count.EqualTo(1));
            Assert.That(offer.Name, Is.EqualTo("강화 무기"));
            Assert.That(offer.Description, Does.Contain("공격력"));
        }

        [Test]
        public void ThreeDraftRounds_StillOfferThreeChoices()
        {
            var build = new RunBuild();
            var random = new Random(12);
            for (int round = 0; round < 3; round++)
            {
                var offers = UpgradeCatalog.DraftThree(build, 1f, random);
                Assert.That(offers, Has.Count.EqualTo(3));
                build.Apply(offers[0]);
            }

            Assert.That(build.Upgrades, Has.Count.EqualTo(3));
        }

        [Test]
        public void DraftThree_WhenOneHalfOfSynergyIsOwned_OffersItsPartner()
        {
            var build = new RunBuild();
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.OverclockServo, 1f));

            var offers = UpgradeCatalog.DraftThree(build, 1f, new Random(22));

            Assert.That(offers.Exists(offer => offer.Id == UpgradeId.BarrageAmplifier), Is.True);
        }

        [Test]
        public void MatchingUpgradePairs_ActivateSynergies()
        {
            var build = new RunBuild();
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.ReinforcedFrame, 1f));
            build.Apply(UpgradeCatalog.CreateOffer(UpgradeId.CounterArmor, 1f));

            Assert.That(build.HasCounterSynergy, Is.True);
            Assert.That(build.HasBarrageSynergy, Is.False);
            Assert.That(build.HasChargeSynergy, Is.False);
        }
    }
}
