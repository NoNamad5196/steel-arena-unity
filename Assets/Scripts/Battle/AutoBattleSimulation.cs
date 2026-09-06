using System;
using ProtocolArena.Run;

namespace ProtocolArena.Battle
{
    public enum BattleOutcome
    {
        Victory,
        Defeat
    }

    public enum CombatEventType
    {
        PlayerAttack,
        PlayerBurst,
        PlayerBarrage,
        PlayerCharge,
        PlayerCounter,
        PlayerRepair,
        EnemyAttack,
        EnemyHeavy
    }

    public readonly struct CombatEvent
    {
        public CombatEvent(float time, CombatEventType type, string message, float value = 0f)
        {
            Time = time;
            Type = type;
            Message = message;
            Value = value;
        }

        public float Time { get; }
        public CombatEventType Type { get; }
        public string Message { get; }
        public float Value { get; }
    }

    public sealed class FighterState
    {
        public FighterState(float maxHealth, float attackDamage, float moveSpeed, float attackRange, float attackCooldown)
        {
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            CurrentHealth = maxHealth;
        }

        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public float AttackDamage { get; }
        public float MoveSpeed { get; }
        public float AttackRange { get; }
        public float AttackCooldown { get; }
        public float HealthRatio => CurrentHealth / MaxHealth;
        public bool IsAlive => CurrentHealth > 0f;

        public void Damage(float amount)
        {
            CurrentHealth = Math.Max(0f, CurrentHealth - Math.Max(0f, amount));
        }

        public float Heal(float amount)
        {
            float before = CurrentHealth;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + Math.Max(0f, amount));
            return CurrentHealth - before;
        }
    }

    public sealed class AutoBattleSimulation
    {
        private const float PlayerStartX = -3f;
        private const float EnemyStartX = 3f;

        private RunBuild build;
        private float playerAttackTimer;
        private float enemyAttackTimer;
        private int playerHitCount;
        private int enemyHitCount;
        private bool repairConsumed;

        public event Action<CombatEvent> EventRaised;
        public event Action StateChanged;
        public event Action<BattleOutcome> BattleEnded;

        public FighterState Player { get; private set; }
        public FighterState Enemy { get; private set; }
        public float PlayerX { get; private set; }
        public float EnemyX { get; private set; }
        public float ElapsedTime { get; private set; }
        public float TotalPlayerDamage { get; private set; }
        public float TotalHealing { get; private set; }
        public int BurstTriggers { get; private set; }
        public int SynergyTriggers { get; private set; }
        public EnemyProfile EnemyProfile { get; private set; }
        public bool IsRunning { get; private set; }

        public void Start(RunBuild runBuild)
        {
            Start(runBuild, EnemyCatalog.Get(EnemyKind.CrusherBoss));
        }

        public void Start(RunBuild runBuild, EnemyProfile enemyProfile)
        {
            build = runBuild ?? throw new ArgumentNullException(nameof(runBuild));
            EnemyProfile = enemyProfile;
            Player = new FighterState(build.MaxHealth, build.AttackDamage, build.MoveSpeed, 1.3f, build.AttackCooldown);
            Enemy = new FighterState(enemyProfile.MaxHealth, enemyProfile.AttackDamage, enemyProfile.MoveSpeed, enemyProfile.AttackRange, enemyProfile.AttackCooldown);
            PlayerX = PlayerStartX;
            EnemyX = EnemyStartX;
            ElapsedTime = 0f;
            TotalPlayerDamage = 0f;
            TotalHealing = 0f;
            BurstTriggers = 0;
            SynergyTriggers = 0;
            playerAttackTimer = 0f;
            enemyAttackTimer = 0f;
            playerHitCount = 0;
            enemyHitCount = 0;
            repairConsumed = false;
            IsRunning = true;
            StateChanged?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0f)
            {
                return;
            }

            ElapsedTime += deltaTime;
            playerAttackTimer -= deltaTime;
            enemyAttackTimer -= deltaTime;
            MoveFighters(deltaTime);
            TryPlayerAttack();
            if (!IsRunning) return;
            TryEnemyAttack();
            if (!IsRunning) return;
            TryRepair();
            StateChanged?.Invoke();
        }

        private void MoveFighters(float deltaTime)
        {
            if (Distance > Player.AttackRange)
            {
                PlayerX = Math.Min(EnemyX, PlayerX + Player.MoveSpeed * deltaTime);
            }

            if (Distance > Enemy.AttackRange)
            {
                EnemyX = Math.Max(PlayerX, EnemyX - Enemy.MoveSpeed * deltaTime);
            }
        }

        private void TryPlayerAttack()
        {
            if (Distance > Player.AttackRange || playerAttackTimer > 0f)
            {
                return;
            }

            playerHitCount++;
            bool isBurst = build.BurstBonus > 0f && playerHitCount % 3 == 0;
            int barrageInterval = build.HasUpgrade(UpgradeId.OverclockServo) ? 3 : 4;
            bool isBarrage = build.BarrageBonus > 0f && playerHitCount % barrageInterval == 0;
            bool isCharge = build.ChargeBonus > 0f && playerHitCount == 1;
            float chargeBonus = build.ChargeBonus * (build.HasUpgrade(UpgradeId.ThrusterKit) ? 1.5f : 1f);
            float damageMultiplier = 1f;
            if (isBurst) damageMultiplier += build.BurstBonus;
            if (isBarrage) damageMultiplier += build.BarrageBonus;
            if (isCharge) damageMultiplier += chargeBonus;
            float damage = Player.AttackDamage * damageMultiplier;
            float healthBefore = Enemy.CurrentHealth;
            Enemy.Damage(damage);
            float dealtDamage = healthBefore - Enemy.CurrentHealth;
            TotalPlayerDamage += dealtDamage;
            playerAttackTimer = Player.AttackCooldown;
            if (isBarrage)
            {
                SynergyTriggers++;
                string combo = build.HasUpgrade(UpgradeId.OverclockServo) ? "고속 장전 연계!" : "탄막 폭발!";
                Raise(CombatEventType.PlayerBarrage, $"{combo}  {dealtDamage:0} 피해", dealtDamage);
            }
            else if (isCharge)
            {
                SynergyTriggers++;
                string combo = build.HasUpgrade(UpgradeId.ThrusterKit) ? "가속 돌진포!" : "돌진포!";
                Raise(CombatEventType.PlayerCharge, $"{combo}  {dealtDamage:0} 피해", dealtDamage);
            }
            else if (isBurst)
            {
                BurstTriggers++;
                Raise(CombatEventType.PlayerBurst, $"연속 공격!  {dealtDamage:0} 피해", dealtDamage);
            }
            else
            {
                Raise(CombatEventType.PlayerAttack, $"내 공격  {dealtDamage:0} 피해", dealtDamage);
            }

            CheckEnd();
        }

        private void TryEnemyAttack()
        {
            if (Distance > Enemy.AttackRange || enemyAttackTimer > 0f)
            {
                return;
            }

            enemyHitCount++;
            bool isHeavy = enemyHitCount % EnemyProfile.HeavyAttackInterval == 0;
            float damage = Enemy.AttackDamage * (isHeavy ? EnemyProfile.HeavyAttackMultiplier : 1f);
            float healthBefore = Player.CurrentHealth;
            Player.Damage(damage);
            float dealtDamage = healthBefore - Player.CurrentHealth;
            enemyAttackTimer = Enemy.AttackCooldown * (isHeavy ? 1.25f : 1f);
            Raise(isHeavy ? CombatEventType.EnemyHeavy : CombatEventType.EnemyAttack,
                isHeavy ? $"적 강타!  {dealtDamage:0} 피해" : $"적 공격  {dealtDamage:0} 피해", dealtDamage);
            TryCounter(dealtDamage);
            CheckEnd();
        }

        private void TryCounter(float receivedDamage)
        {
            if (build.CounterRatio <= 0f || !Player.IsAlive || receivedDamage <= 0f) return;

            float ratio = build.CounterRatio * (build.HasUpgrade(UpgradeId.ReinforcedFrame) ? 1.5f : 1f);
            float enemyHealthBefore = Enemy.CurrentHealth;
            Enemy.Damage(receivedDamage * ratio);
            float reflected = enemyHealthBefore - Enemy.CurrentHealth;
            TotalPlayerDamage += reflected;
            SynergyTriggers++;
            string combo = build.HasUpgrade(UpgradeId.ReinforcedFrame) ? "튼튼한 장갑 연계 반격!" : "장갑 반격!";
            Raise(CombatEventType.PlayerCounter, $"{combo}  {reflected:0} 피해", reflected);
        }

        private void TryRepair()
        {
            if (repairConsumed || build.RepairAmount <= 0f || Player.HealthRatio > 0.35f)
            {
                return;
            }

            repairConsumed = true;
            float healed = Player.Heal(build.RepairAmount);
            TotalHealing += healed;
            Raise(CombatEventType.PlayerRepair, $"비상 수리!  체력 +{healed:0}", healed);
        }

        private void CheckEnd()
        {
            if (!Enemy.IsAlive)
            {
                Finish(BattleOutcome.Victory);
            }
            else if (!Player.IsAlive)
            {
                Finish(BattleOutcome.Defeat);
            }
        }

        private void Finish(BattleOutcome outcome)
        {
            if (!IsRunning) return;
            IsRunning = false;
            StateChanged?.Invoke();
            BattleEnded?.Invoke(outcome);
        }

        private void Raise(CombatEventType type, string message, float value)
        {
            EventRaised?.Invoke(new CombatEvent(ElapsedTime, type, message, value));
        }

        private float Distance => Math.Max(0f, EnemyX - PlayerX);
    }
}
