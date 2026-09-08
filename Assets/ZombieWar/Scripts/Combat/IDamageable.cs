public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(DamageInfo damageInfo);
}
