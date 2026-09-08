using UnityEngine;

public static class PlayerControlRules
{
    public static Vector3 Move(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);
        return new Vector3(input.x, 0f, input.y);
    }

    public static bool IsAiming(Vector2 aim, float deadZone)
    {
        return aim.sqrMagnitude > deadZone * deadZone;
    }

    public static Vector3 Facing(Vector2 move, Vector2 aim, Vector3 previous, float deadZone)
    {
        if (IsAiming(aim, deadZone)) return Move(aim).normalized;
        Vector3 motion = Move(move);
        return motion.sqrMagnitude > 0.0001f ? motion.normalized : previous;
    }
}
