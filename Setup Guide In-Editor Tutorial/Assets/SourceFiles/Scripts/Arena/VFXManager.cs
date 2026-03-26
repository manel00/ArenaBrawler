using UnityEngine;

namespace ArenaEnhanced
{
    public static class VFXManager
    {
        public static void SpawnImpactEffect(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.5f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.8f, 0.2f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 3f);
                r.material = mat;
            }
            Object.Destroy(go.GetComponent<Collider>());
            Object.Destroy(go, 0.3f);
        }

        public static void SpawnDeathEffect(Vector3 pos)
        {
            for (int i = 0; i < 8; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pos + Random.insideUnitSphere * 0.5f;
                go.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.3f, 0.3f, 0.3f);
                Object.Destroy(go.GetComponent<Collider>());
                var rb = go.AddComponent<Rigidbody>();
                rb.linearVelocity = Random.insideUnitSphere * 5f + Vector3.up * 3f;
                Object.Destroy(go, 1.5f);
            }
        }

        public static void SpawnShieldEffect(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.up;
            go.transform.localScale = Vector3.one * 2.5f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0.3f, 0.6f, 1f, 0.3f);
                r.material = mat;
            }
            Object.Destroy(go.GetComponent<Collider>());
            Object.Destroy(go, 3f);
        }

        public static void SpawnDashEffect(Vector3 pos)
        {
            for (int i = 0; i < 5; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pos + Vector3.up * 0.5f + Random.insideUnitSphere * 0.3f;
                go.transform.localScale = Vector3.one * 0.15f;
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.8f, 0.8f, 1f, 0.6f);
                Object.Destroy(go.GetComponent<Collider>());
                Object.Destroy(go, 0.4f);
            }
        }

        public static void SpawnMeleeEffect(Vector3 pos, Vector3 dir)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos + dir * 0.5f;
            go.transform.localScale = new Vector3(2f, 0.3f, 0.3f);
            go.transform.rotation = Quaternion.LookRotation(dir);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 1f, 1f, 0.7f);
                r.material = mat;
            }
            Object.Destroy(go.GetComponent<Collider>());
            Object.Destroy(go, 0.2f);
        }
    }
}
