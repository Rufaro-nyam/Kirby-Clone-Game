using UnityEngine;

// A list of all possible abilities. We can add to this list later!
public enum CopyAbility
{
    None,
    Fly,
    Bow,
    Dash
}

public class InhalableEnemy : MonoBehaviour
{
    [Header("Enemy Properties")]
    public CopyAbility abilityType = CopyAbility.None; // Change this in the Inspector!
}