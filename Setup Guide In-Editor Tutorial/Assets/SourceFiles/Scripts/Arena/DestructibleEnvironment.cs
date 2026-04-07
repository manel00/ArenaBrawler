using UnityEngine;

namespace ArenaEnhanced
{
    /// <summary>
    /// Attach to arena objects (trees, rocks) to make them destructible.
    /// When a Boss (isDestructive=true) collides, Shatter() is called and the object is removed.
    /// </summary>
    public class DestructibleEnvironment : MonoBehaviour
    {
        [Header("Shatter Effect")]
        public int fragmentCount = 8;
        public float fragmentForce = 12f;
        public float fragmentLifetime = 2f;
        public Color fragmentColor = new Color(0.4f, 0.25f, 0.1f);

        private bool _shattered = false;

        public void Shatter()
        {
            if (_shattered) return;
            _shattered = true;

            SpawnFragments();
            Destroy(gameObject);
        }

        private void SpawnFragments()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            Color color = renderers.Length > 0 ? fragmentColor : fragmentColor;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null || mat.shader.name.Contains("Error"))
                mat = new Material(Shader.Find("Standard"));
            if (mat == null || mat.shader.name.Contains("Error"))
                mat = new Material(Shader.Find("Diffuse"));
            mat.color = color;

            for (int i = 0; i < fragmentCount; i++)
            {
                var frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frag.transform.position = transform.position + Vector3.up * Random.Range(0.5f, 2f)
                                          + Random.insideUnitSphere * 0.8f;
                float size = Random.Range(0.15f, 0.55f);
                frag.transform.localScale = new Vector3(size, size, size);
                frag.GetComponent<Renderer>().material = mat;

                var rb = frag.AddComponent<Rigidbody>();
                rb.linearVelocity = Random.insideUnitSphere * fragmentForce + Vector3.up * fragmentForce * 0.5f;
                rb.angularVelocity = Random.insideUnitSphere * 5f;

                Destroy(frag, fragmentLifetime);
            }
        }

        /// <summary>
        /// Convenience: call in editor to tag all Environment children as destructible.
        /// </summary>
        [ContextMenu("Add To All Children")]
        private void AddToAllChildren()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child == transform) continue;
                if (child.GetComponent<DestructibleEnvironment>() == null)
                    child.gameObject.AddComponent<DestructibleEnvironment>();
            }
        }
    }
}
