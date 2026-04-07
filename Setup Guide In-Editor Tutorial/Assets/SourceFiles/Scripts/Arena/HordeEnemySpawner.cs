using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ArenaEnhanced
{
    /// <summary>
    /// Static utility that instantiates horde enemies from glb models.
    /// Normal enemies (Mono, Piranha, Sabrewulf): ground melee, NavMeshAgent.
    /// Abeja: flying melee, no NavMeshAgent.
    /// Bosses (T-Rex, TyrannosaurusRex): ground melee, NavMeshAgent, 5x scaled, destructive.
    /// </summary>
    public static class HordeEnemySpawner
    {
        private const float PlayerHeight = 1.8f;

        public static ArenaCombatant SpawnNormalEnemy(string modelPath, Vector3 position, bool isFlying)
        {
            string enemyName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            var go = new GameObject("Enemy_" + enemyName);
            go.transform.position = position;
            go.tag = "Enemy";

#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab != null)
            {
                var model = Object.Instantiate(prefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                ApplyAutoScale(model, PlayerHeight);
                foreach (var c in model.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(c);
            }
            else
            {
                BuildCapsuleFallback(go, Color.red);
            }
#else
            BuildCapsuleFallback(go, Color.red);
#endif

            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.45f;
            col.height = 1.6f;
            col.center = new Vector3(0f, 0.8f, 0f);

            // All enemies use pure Rigidbody movement (arena is procedurally built, no NavMesh)
            var rb = go.AddComponent<Rigidbody>();
            if (isFlying)
            {
                rb.mass = 20f;
                rb.useGravity = false;
            }
            else
            {
                rb.mass = 60f;
                rb.useGravity = true;
            }
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = enemyName;
            combatant.teamId = 99;
            combatant.isPlayer = false;
            combatant.maxHp = isFlying ? 80f : 120f;
            combatant.hp = combatant.maxHp;
            combatant.countsForVictory = true;

            var hpBar = go.AddComponent<WorldHPBar>();
            hpBar.combatant = combatant;
            hpBar.label = enemyName;

            var ai = go.AddComponent<HordeEnemyAI>();
            ai.isFlying = isFlying;
            ai.isBoss = false;
            ai.isDestructive = false;
            ai.combatant = combatant;
            ai.attackRange = isFlying ? 2.5f : 1.8f;
            ai.attackDamage = isFlying ? 12f : 18f;
            ai.attackCooldown = 1.2f;
            ai.moveSpeed = Random.Range(7.35f, 11.55f);
            ai.flyingHoverHeight = 2.5f;

            return combatant;
        }

        public static ArenaCombatant SpawnBossEnemy(string modelPath, Vector3 position, float scaleMultiplier)
        {
            string bossName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            var go = new GameObject("Boss_" + bossName);
            go.transform.position = position;
            go.tag = "Boss";

            float bossH = PlayerHeight * scaleMultiplier; // ~9m

#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab != null)
            {
                var model = Object.Instantiate(prefab, go.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                ApplyAutoScale(model, bossH);
                foreach (var c in model.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(c);
            }
            else
            {
                BuildBoxFallback(go, new Color(0.6f, 0.1f, 0.1f), bossH);
            }
#else
            BuildBoxFallback(go, new Color(0.6f, 0.1f, 0.1f), bossH);
#endif

            // Large capsule collider for boss
            var col = go.AddComponent<CapsuleCollider>();
            col.radius = bossH * 0.22f;
            col.height = bossH;
            col.center = new Vector3(0f, bossH * 0.5f, 0f);

            // Pure Rigidbody movement (no NavMesh in procedural arena)
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1500f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var combatant = go.AddComponent<ArenaCombatant>();
            combatant.displayName = bossName + " [BOSS]";
            combatant.teamId = 99;
            combatant.isPlayer = false;
            combatant.maxHp = 1000f;
            combatant.hp = 1000f;
            combatant.countsForVictory = true;

            var hpBar = go.AddComponent<WorldHPBar>();
            hpBar.combatant = combatant;
            hpBar.label = bossName + " [BOSS]";

            var ai = go.AddComponent<HordeEnemyAI>();
            ai.isFlying = false;
            ai.isBoss = true;
            ai.isDestructive = true;
            ai.combatant = combatant;
            ai.attackRange = bossH * 0.5f;
            ai.attackDamage = 52.5f;
            ai.attackCooldown = 2f;
            ai.moveSpeed = 9f;
            ai.flyingHoverHeight = 0f;

            return combatant;
        }

        private static void ApplyAutoScale(GameObject model, float targetHeight)
        {
            model.transform.localScale = Vector3.one;
            var renderers = model.GetComponentsInChildren<Renderer>();
            float nativeH = 0f;
            foreach (var r in renderers)
                if (r.bounds.size.y > nativeH) nativeH = r.bounds.size.y;

            if (nativeH > 0.001f)
            {
                float factor = targetHeight / nativeH;
                model.transform.localScale = Vector3.one * factor;

                float lowestY = float.MaxValue;
                foreach (var r in renderers)
                    if (r.bounds.min.y < lowestY) lowestY = r.bounds.min.y;

                float offsetY = lowestY - model.transform.position.y;
                model.transform.localPosition = new Vector3(0f, -offsetY, 0f);
            }
        }

        private static void BuildCapsuleFallback(GameObject go, Color color)
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.transform.SetParent(go.transform);
            cap.transform.localPosition = new Vector3(0f, 1f, 0f);
            cap.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            cap.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(cap.GetComponent<Collider>());
        }

        private static void BuildBoxFallback(GameObject go, Color color, float height)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(go.transform);
            box.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            box.transform.localScale = new Vector3(height * 0.35f, height, height * 0.35f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            box.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }
    }
}
