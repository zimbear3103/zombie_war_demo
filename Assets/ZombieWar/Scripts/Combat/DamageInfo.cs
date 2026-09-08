using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float amount;
    public readonly Vector3 hitPoint;
    public readonly Vector3 hitDirection;
    public readonly float knockbackForce;
    public readonly GameObject source;

    public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitDirection, float knockbackForce, GameObject source)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;
        this.knockbackForce = knockbackForce;
        this.source = source;
    }
}
