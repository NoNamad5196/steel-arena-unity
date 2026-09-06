namespace ProtocolArena.Battle
{
    public enum EnemyKind
    {
        RushUnit,
        CrusherBoss
    }

    public readonly struct EnemyProfile
    {
        public EnemyProfile(
            EnemyKind kind,
            string name,
            string description,
            float maxHealth,
            float attackDamage,
            float moveSpeed,
            float attackRange,
            float attackCooldown,
            int heavyAttackInterval,
            float heavyAttackMultiplier)
        {
            Kind = kind;
            Name = name;
            Description = description;
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            HeavyAttackInterval = heavyAttackInterval;
            HeavyAttackMultiplier = heavyAttackMultiplier;
        }

        public EnemyKind Kind { get; }
        public string Name { get; }
        public string Description { get; }
        public float MaxHealth { get; }
        public float AttackDamage { get; }
        public float MoveSpeed { get; }
        public float AttackRange { get; }
        public float AttackCooldown { get; }
        public int HeavyAttackInterval { get; }
        public float HeavyAttackMultiplier { get; }
    }

    public static class EnemyCatalog
    {
        public static EnemyProfile Get(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.RushUnit:
                    return new EnemyProfile(kind, "고철 돌격병", "빠르게 달라붙어 자주 공격합니다.", 82f, 8f, 2.15f, 1.05f, 0.82f, 5, 1.45f);
                case EnemyKind.CrusherBoss:
                    return new EnemyProfile(kind, "파쇄 대장", "느리지만 세 번째 공격마다 강타합니다.", 155f, 10f, 1.15f, 1.2f, 1.12f, 3, 1.8f);
                default:
                    return Get(EnemyKind.RushUnit);
            }
        }
    }
}
