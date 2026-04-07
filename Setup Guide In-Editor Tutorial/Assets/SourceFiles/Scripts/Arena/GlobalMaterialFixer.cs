using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// REPARADOR GLOBAL DE MATERIALES - Se ejecuta al inicio de la escena
    /// Arregla TODOS los materiales violetas (magenta) del mapa Forest Valley
    /// </summary>
    public class GlobalMaterialFixer : MonoBehaviour
    {
        [Header("Configuración de Reparación")]
        public bool fixOnStart = true;
        public bool fixEveryFrame = false;
        public float fixInterval = 5f;
        public bool showDebugLogs = true;
        public bool fixInstantiatedEffects = true;

        private float lastFixTime = 0f;
        private int totalFixed = 0;
        private List<GameObject> processedObjects = new List<GameObject>();

        void Start()
        {
            if (fixOnStart)
            {
                StartCoroutine(FixAllMaterialsDelayed());
            }
        }

        void Update()
        {
            if (fixEveryFrame && Time.time - lastFixTime > fixInterval)
            {
                FixAllVioletMaterials();
                lastFixTime = Time.time;
            }
            
            // Fix dinámico para efectos instanciados
            if (fixInstantiatedEffects)
            {
                FixNewEffectObjects();
            }
        }
        
        /// <summary>
        /// Arregla efectos que se instancian dinámicamente (fireballs, disparos, etc.)
        /// </summary>
        private void FixNewEffectObjects()
        {
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            
            foreach (var renderer in allRenderers)
            {
                if (renderer == null || renderer.gameObject == null) continue;
                
                // Solo procesar objetos que no hemos procesado antes
                if (processedObjects.Contains(renderer.gameObject)) continue;
                
                // Verificar si es un efecto dinámico
                bool isEffect = renderer is ParticleSystemRenderer ||
                                 renderer.gameObject.name.Contains("Fly") ||
                                 renderer.gameObject.name.Contains("Hit") ||
                                 renderer.gameObject.name.Contains("Fireball") ||
                                 renderer.gameObject.name.Contains("Impact") ||
                                 renderer.gameObject.name.Contains("Pattern") ||
                                 (renderer.transform.parent != null && 
                                  (renderer.transform.parent.name.Contains("Fly") ||
                                   renderer.transform.parent.name.Contains("Fireball")));
                
                if (!isEffect) 
                {
                    processedObjects.Add(renderer.gameObject);
                    continue;
                }
                
                // Procesar materiales
                if (renderer.sharedMaterials != null)
                {
                    bool needsFix = false;
                    var materials = renderer.sharedMaterials;
                    
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var mat = materials[i];
                        if (mat != null && IsMaterialViolet(mat))
                        {
                            FixEffectMaterial(renderer, mat, i, ref materials);
                            totalFixed++;
                            needsFix = true;
                        }
                    }
                    
                    if (needsFix)
                    {
                        renderer.materials = materials;
                    }
                }
                
                processedObjects.Add(renderer.gameObject);
            }
            
            // Limpiar lista si crece demasiado
            if (processedObjects.Count > 5000)
            {
                processedObjects.RemoveRange(0, 1000);
            }
        }
        
        private void FixEffectMaterial(Renderer renderer, Material oldMat, int index, ref Material[] materials)
        {
            // Crear material URP para efectos
            Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (urpShader == null) return;
            
            Material newMat = new Material(urpShader);
            newMat.name = $"Fixed_{oldMat.name}";
            
            // Configurar para efectos transparentes
            newMat.SetFloat("_Surface", 1); // Transparent
            newMat.SetFloat("_Blend", 1); // Additive
            newMat.SetFloat("_ZWrite", 0);
            newMat.renderQueue = 3000;
            
            // Copiar textura si existe
            if (oldMat.HasProperty("_MainTex"))
            {
                var tex = oldMat.GetTexture("_MainTex");
                if (tex != null && newMat.HasProperty("_BaseMap"))
                    newMat.SetTexture("_BaseMap", tex);
            }
            
            // Color según tipo de efecto
            string objName = renderer.gameObject.name.ToLower();
            if (objName.Contains("fire") || objName.Contains("fly") || objName.Contains("03") || objName.Contains("05"))
            {
                newMat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.9f));
                newMat.SetColor("_EmissionColor", new Color(2f, 0.6f, 0f, 1f));
                newMat.EnableKeyword("_EMISSION");
            }
            else if (objName.Contains("hit") || objName.Contains("explosion"))
            {
                newMat.SetColor("_BaseColor", new Color(1f, 0.2f, 0f, 0.9f));
                newMat.SetColor("_EmissionColor", new Color(3f, 0.3f, 0f, 1f));
                newMat.EnableKeyword("_EMISSION");
            }
            else
            {
                newMat.SetColor("_BaseColor", new Color(0.9f, 0.3f, 0.7f, 0.8f));
            }
            
            materials[index] = newMat;
            
            if (showDebugLogs)
                Debug.Log($"[GlobalMaterialFixer] Fixed effect: {renderer.gameObject.name} -> {oldMat.name}");
        }

        private IEnumerator FixAllMaterialsDelayed()
        {
            // Esperar 1 frame para que todo cargue
            yield return null;
            yield return new WaitForEndOfFrame();
            
            if (showDebugLogs)
                Debug.Log("[GlobalMaterialFixer] === INICIANDO REPARACIÓN MASIVA ===");
            
            totalFixed = 0;
            
            // ARREGLAR TODOS LOS RENDERERS
            var allRenderers = FindObjectsByType<Renderer>();
            
            if (showDebugLogs)
                Debug.Log($"[GlobalMaterialFixer] Encontrados {allRenderers.Length} renderers");
            
            foreach (var renderer in allRenderers)
            {
                if (renderer == null) continue;
                
                // Procesar materiales
                if (renderer.sharedMaterials != null)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var mat = renderer.sharedMaterials[i];
                        if (mat != null && IsMaterialViolet(mat))
                        {
                            FixMaterial(renderer, mat, i);
                            totalFixed++;
                        }
                    }
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"[GlobalMaterialFixer] === REPARACIÓN COMPLETA: {totalFixed} materiales arreglados ===");
        }

        [ContextMenu("Fix All Materials Now")]
        public void FixAllVioletMaterials()
        {
            totalFixed = 0;
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            
            foreach (var renderer in allRenderers)
            {
                if (renderer == null || renderer.sharedMaterials == null) continue;
                
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    if (mat != null && IsMaterialViolet(mat))
                    {
                        FixMaterial(renderer, mat, i);
                        totalFixed++;
                    }
                }
            }
            
            if (showDebugLogs && totalFixed > 0)
                Debug.Log($"[GlobalMaterialFixer] Arreglados {totalFixed} materiales");
        }

        private bool IsMaterialViolet(Material mat)
        {
            if (mat == null) return true;
            if (mat.shader == null) return true;
            
            string shaderName = mat.shader.name;
            
            // Check si es magenta/violet (shader roto)
            if (shaderName == "Hidden/InternalErrorShader") return true;
            if (string.IsNullOrEmpty(shaderName)) return true;
            if (!mat.shader.isSupported) return true;
            
            // Check colores característicos de error
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                if (c.r > 0.9f && c.g < 0.1f && c.b > 0.9f) // Magenta
                    return true;
            }
            
            // Check por nombre de shader problemático
            if (shaderName.Contains("5946") || 
                shaderName.Contains("Korean") || 
                shaderName.Contains("Pattern") ||
                shaderName.Contains("AlphaBlend") ||
                shaderName.Contains("AdditiveBlend"))
            {
                var shader = Shader.Find(shaderName);
                if (shader == null) return true;
            }
            
            // Shader Graph sin compilar
            if (shaderName.StartsWith("Shader Graphs/"))
            {
                var shader = Shader.Find(shaderName);
                if (shader == null || !shader.isSupported) return true;
            }
            
            return false;
        }

        private void FixMaterial(Renderer renderer, Material oldMat, int materialIndex)
        {
            if (oldMat == null || renderer == null) return;
            
            // Determinar tipo de shader
            bool isParticle = renderer is ParticleSystemRenderer;
            bool isTree = renderer.gameObject.name.ToLower().Contains("tree") || 
                         renderer.transform.parent != null && renderer.transform.parent.name.ToLower().Contains("tree");
            
            string shaderName = isParticle 
                ? "Universal Render Pipeline/Particles/Unlit"
                : "Universal Render Pipeline/Lit";
            
            var urpShader = Shader.Find(shaderName);
            if (urpShader == null) return;
            
            // Crear nuevo material
            Material newMat = new Material(urpShader);
            newMat.name = $"Fixed_{oldMat.name}";
            
            // Guardar y copiar textura si existe
            Texture savedTex = null;
            if (oldMat.HasProperty("_MainTex"))
                savedTex = oldMat.GetTexture("_MainTex");
            if (savedTex == null && oldMat.HasProperty("_BaseMap"))
                savedTex = oldMat.GetTexture("_BaseMap");
            
            if (savedTex != null && newMat.HasProperty("_BaseMap"))
                newMat.SetTexture("_BaseMap", savedTex);
            
            // Configurar color según tipo
            if (isParticle)
            {
                // Partículas - fuego/naranja por defecto
                newMat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.8f));
                newMat.SetColor("_EmissionColor", new Color(2f, 0.5f, 0f, 1f));
                newMat.EnableKeyword("_EMISSION");
                newMat.SetFloat("_Surface", 1); // Transparent
                newMat.SetFloat("_Blend", 1); // Additive
                newMat.renderQueue = 3000;
            }
            else if (isTree)
            {
                // Árboles - verde
                newMat.SetColor("_BaseColor", new Color(0.25f, 0.5f, 0.15f, 1f));
                newMat.SetFloat("_Smoothness", 0.1f);
            }
            else
            {
                // Default - gris neutro
                newMat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 1f));
                newMat.SetFloat("_Smoothness", 0.3f);
            }
            
            // Aplicar material
            var materials = renderer.materials;
            if (materialIndex < materials.Length)
            {
                materials[materialIndex] = newMat;
                renderer.materials = materials;
            }
            
            if (showDebugLogs)
                Debug.Log($"[GlobalMaterialFixer] Fixed: {renderer.gameObject.name} -> {oldMat.name} (was {oldMat.shader?.name})");
        }

        [ContextMenu("Force Deep Repair")]
        public void ForceDeepRepair()
        {
            StartCoroutine(FixAllMaterialsDelayed());
        }
    }
}
