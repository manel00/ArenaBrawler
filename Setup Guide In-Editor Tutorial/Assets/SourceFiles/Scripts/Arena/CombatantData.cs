using UnityEngine;

namespace ArenaEnhanced
{
    [CreateAssetMenu(fileName = "NewCombatantData", menuName = "Arena/Combatant Data")]
    public class CombatantData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Fighter";
        
        [Header("Stats")]
        public float maxHp = 100f;
        public float moveSpeed = 5f;
        
        [Header("Combat")]
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;

        [Header("VFX & Audio")]
        public Color themeColor = Color.white;
    }
}
