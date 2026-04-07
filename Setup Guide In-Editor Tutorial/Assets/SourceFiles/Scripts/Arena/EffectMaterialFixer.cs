using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ArenaEnhanced
{
    /// <summary>
    /// Fix específico para materiales de KoreanTraditionalPattern_Effect
    /// Arregla los shaders magenta en efectos de fireball y disparos
    /// </summary>
    public class EffectMaterialFixer : MonoBehaviour
    {
        [Header("Auto Fix Settings")]
        public bool fixOnStart = true;
        public float fixDelay = 0.5f;
        
        [Header("URP Shaders")]
        public Shader particleUnlitShader;
        public Shader particleLitShader;
        
        void Start()
        {
            // Cachear shaders URP
            if (particleUnlitShader == null)
                particleUnlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleLitShader == null)
                particleLitShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            
            if (fixOnStart)
                StartCoroutine(FixEffectMaterialsDelayed());
        }
        
        private IEnumerator FixEffectMaterialsDelayed()
        {
            yield return new WaitForSeconds(fixDelay);
            
            int fixedCount = 0;
            
            // Buscar TODOS los renderers en la escena incluyendo efectos instanciados
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                
                // Verificar si es un efecto de KoreanTraditionalPattern
                bool isKoreanEffect = renderer.gameObject.name.Contains("Fly") || 
                                     renderer.gameObject.name.Contains("Hit") ||
                                     renderer.gameObject.name.Contains("Pattern") ||
                                     (renderer.transform.parent != null && 
                                      renderer.transform.parent.name.Contains("Fly"));
                
                if (!isKoreanEffect && !(renderer is ParticleSystemRenderer))
                    continue;
                
                var materials = renderer.sharedMaterials;
                bool needsFix = false;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat == null) continue;
                    
                    // Verificar si el material está roto (magenta)
                    if (IsBrokenMaterial(mat))
                    {
                        needsFix = true;
                        
                        // Crear material URP replacement
                        Shader newShader = particleUnlitShader != null ? particleUnlitShader : particleLitShader;
                        if (newShader == null) continue;
                        
                        Material newMat = new Material(newShader);
                        newMat.name = $"Fixed_{mat.name}";
                        
                        // Copiar textura si existe
                        if (mat.HasProperty("_MainTex"))
                        {
                            var tex = mat.GetTexture("_MainTex");
                            if (tex != null && newMat.HasProperty("_BaseMap"))
                                newMat.SetTexture("_BaseMap", tex);
                        }
                        
                        // Configurar como transparente/additive para efectos
                        newMat.SetFloat("_Surface", 1); // Transparent
                        newMat.SetFloat("_Blend", 1); // Additive
                        newMat.SetFloat("_ZWrite", 0);
                        newMat.renderQueue = 3000;
                        
                        // Color basado en el nombre del objeto
                        SetEffectColor(newMat, renderer.gameObject.name);
                        
                        materials[i] = newMat;
                        fixedCount++;
                        
                        Debug.Log($"[EffectMaterialFixer] Fixed material on {renderer.gameObject.name}: {mat.name} (shader: {mat.shader?.name})");
                    }
                }
                
                if (needsFix)
                {
                    renderer.materials = materials;
                }
            }
            
            if (fixedCount > 0)
                Debug.Log($"[EffectMaterialFixer] Fixed {fixedCount} effect materials");
        }
        
        private bool IsBrokenMaterial(Material mat)
        {
            if (mat == null) return false;
            if (mat.shader == null) return true;
            
            string shaderName = mat.shader.name;
            
            // Shaders problemáticos conocidos
            if (shaderName == "Hidden/InternalErrorShader") return true;
            if (string.IsNullOrEmpty(shaderName)) return true;
            if (!mat.shader.isSupported) return true;
            
            // Colores magenta indican error
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                if (c.r > 0.9f && c.g < 0.1f && c.b > 0.9f)
                    return true;
            }
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                if (c.r > 0.9f && c.g < 0.1f && c.b > 0.9f)
                    return true;
            }
            
            // Shaders antiguos de KoreanTraditionalPattern
            if (shaderName.Contains("5946") || 
                shaderName.Contains("AlphaBlend") ||
                shaderName.Contains("AdditiveBlend"))
            {
                // Verificar si el shader existe
                var shader = Shader.Find(shaderName);
                if (shader == null) return true;
            }
            
            return false;
        }
        
        private void SetEffectColor(Material mat, string objectName)
        {
            string name = objectName.ToLower();
            
            // Fire/Fly effects - naranja/rojo
            if (name.Contains("fly") || name.Contains("fire") || name.Contains("03") || name.Contains("05"))
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.9f));
                mat.SetColor("_EmissionColor", new Color(2f, 0.6f, 0f, 1f));
                mat.EnableKeyword("_EMISSION");
            }
            // Hit/Explosion effects - rojo intenso
            else if (name.Contains("hit") || name.Contains("explosion") || name.Contains("impact"))
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.2f, 0f, 0.9f));
                mat.SetColor("_EmissionColor", new Color(3f, 0.3f, 0f, 1f));
                mat.EnableKeyword("_EMISSION");
            }
            // Pattern effects - magenta/rosa (pero URP funcional)
            else if (name.Contains("pattern") || name.Contains("12") || name.Contains("aura"))
            {
                mat.SetColor("_BaseColor", new Color(0.9f, 0.3f, 0.7f, 0.8f));
                mat.SetColor("_EmissionColor", new Color(1.5f, 0.5f, 1f, 1f));
                mat.EnableKeyword("_EMISSION");
            }
            // Default - blanco suave
            else
            {
                mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.8f));
            }
        }
        
        [ContextMenu("Force Fix Now")]
        public void ForceFix()
        {
            StartCoroutine(FixEffectMaterialsDelayed());
        }
    }
}
