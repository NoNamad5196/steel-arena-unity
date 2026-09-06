using System;
using NUnit.Framework;
using ProtocolArena.Minigame;

namespace ProtocolArena.Tests
{
    public sealed class CalibrationSessionTests
    {
        [Test]
        public void ThreeLocks_CompleteSessionAndProduceBoundedQuality()
        {
            var session = new CalibrationSession(new Random(4));

            for (int attempt = 0; attempt < CalibrationSession.RequiredAttempts; attempt++)
            {
                session.Tick(0.2f);
                session.LockIn();
            }

            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.FinalScore, Is.InRange(0f, 100f));
            Assert.That(session.RewardQuality, Is.InRange(0.8f, 1.4f));
            CollectionAssert.Contains(new[] { "S", "A", "B", "C" }, session.GetGrade());
        }
    }
}
