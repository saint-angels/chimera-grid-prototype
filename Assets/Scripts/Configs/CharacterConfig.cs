using UnityEngine;

[CreateAssetMenu(fileName = "CharacterConfig", menuName = "Settings/CharacterConfig")]
public class CharacterConfig : ScriptableObject
{
    public int maxHealth = 1;
    public int attackDamage = 1;
    public int moveDistance = 6;
    public int attackRange = 1;
}
