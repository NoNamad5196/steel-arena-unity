using System;

namespace ProtocolArena.Minigame
{
    public sealed class CalibrationSession
    {
        public const int RequiredAttempts = 3;
        public const float TargetHalfWidth = 0.075f;

        private readonly Random random;
        private float direction = 1f;
        private float totalScore;

        public CalibrationSession(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            CursorPosition = 0.08f;
            PickTarget();
        }

        public float CursorPosition { get; private set; }
        public float TargetCenter { get; private set; }
        public int AttemptsCompleted { get; private set; }
        public float LastAttemptScore { get; private set; }
        public float FinalScore => AttemptsCompleted == 0 ? 0f : totalScore / AttemptsCompleted;
        public float RewardQuality => 0.8f + FinalScore / 100f * 0.6f;
        public bool IsComplete => AttemptsCompleted >= RequiredAttempts;

        public void Tick(float deltaTime)
        {
            if (IsComplete || deltaTime <= 0f)
            {
                return;
            }

            float speed = 0.72f + AttemptsCompleted * 0.16f;
            CursorPosition += direction * speed * deltaTime;
            if (CursorPosition >= 0.96f)
            {
                CursorPosition = 0.96f;
                direction = -1f;
            }
            else if (CursorPosition <= 0.04f)
            {
                CursorPosition = 0.04f;
                direction = 1f;
            }
        }

        public float LockIn()
        {
            if (IsComplete)
            {
                return LastAttemptScore;
            }

            float distance = Math.Abs(CursorPosition - TargetCenter);
            LastAttemptScore = Math.Max(0f, 100f * (1f - distance / 0.36f));
            totalScore += LastAttemptScore;
            AttemptsCompleted++;
            if (!IsComplete)
            {
                PickTarget();
            }

            return LastAttemptScore;
        }

        public string GetGrade()
        {
            float score = FinalScore;
            if (score >= 90f) return "S";
            if (score >= 75f) return "A";
            if (score >= 55f) return "B";
            return "C";
        }

        private void PickTarget()
        {
            TargetCenter = 0.18f + (float)random.NextDouble() * 0.64f;
        }
    }
}
