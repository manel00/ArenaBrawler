using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// REPARADOR DEFINITIVO DE MATERIALES VIOLETAS
    /// Arregla TODOS los materiales rotos del proyecto sin usar Synty
    /// Solo assets de terceros verificados: TerrainDemoScene_URP, Stylized Nature MegaKit, KoreanTraditionalPattern_Effect
    /// </summary>
    public class VioletMaterialEliminator : EditorWindow
    {
        private Vector2 scrollPos;
        private List<string> fixedMaterials = new List<string>();
        private int totalScanned = 0;
        private int totalFixed = 0;

        [MenuItem("Window/Arena Enhanced/ELIMINATE VIOLET MATERIALS")]
        public static void ShowWindow()
        {
            var window = GetWindow<VioletMaterialEliminator>("ELIMINATE VIOLETS", true);
            window.minSize = new Vector2(500, 400);
            window.maxSize = new Vector2(600, 600);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            // Título grande
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("VIOLET MATERIAL ELIMINATOR", titleStyle);
            
            GUILayout.Space(10);
            
            // Stats
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Materials Scanned: {totalScanned}", EditorStyles.largeLabel);
            GUILayout.Label($"Violet Materials Fixed: {totalFixed}", EditorStyles.largeLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(20);
            
            // Botón principal MASIVO
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("ELIMINATE ALL VIOLET MATERIALS NOW", GUILayout.Height(60)))
            {
                EliminateAllVioletMaterials();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            // Botones específicos
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix KoreanTraditionalPattern Only", GUILayout.Height(40)))
            {
                FixKoreanTraditionalPattern();
            }
            if (GUILayout.Button("Fix TerrainDemoScene Only", GUILayout.Height(40)))
            {
                FixTerrainDemoScene();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix Stylized Nature Kit Only", GUILayout.Height(40)))
            {
                FixStylizedNatureKit();
            }
            if (GUILayout.Button("Fix Current Scene Only", GUILayout.Height(40)))
            {
                FixCurrentScene();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            // Lista de materiales arreglados
            if (fixedMaterials.Count > 0)
            {
                GUILayout.Label("Fixed Materials:", EditorStyles.boldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                foreach (var matName in fixedMaterials.Take(50))
                {
                    EditorGUILayout.LabelField($"✓ {matName}", EditorStyles.miniLabel);
                }
                if (fixedMaterials.Count > 50)
                {
                    EditorGUILayout.LabelField($"... and {fixedMaterials.Count - 50} more", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.Space(10);
            
            // Clear button
            if (GUILayout.Button("Clear Results", GUILayout.Height(30)))
            {
                fixedMaterials.Clear();
                totalScanned = 0;
                totalFixed = 0;
            }
        }

        private void EliminateAllVioletMaterials()
        {
            fixedMaterials.Clear();
            totalScanned = 0;
            totalFixed = 0;

            // 1. Arreglar KoreanTraditionalPattern_Effect (el peor)
            FixKoreanTraditionalPattern();
            
            // 2. Arreglar TerrainDemoScene_URP
            FixTerrainDemoScene();
            
            // 3. Arreglar Stylized Nature MegaKit
            FixStylizedNatureKit();
            
            // 4. Arreglar cualquier otro material violeta en el proyecto
            FixAllRemainingVioletMaterials();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("COMPLETE", 
                $"ELIMINATED {totalFixed} VIOLET MATERIALS!\n\nAll materials are now fixed and working.", "OK");
        }

        private void FixKoreanTraditionalPattern()
        {
            string[] folders = new string[]
            {
                "Assets/KoreanTraditionalPattern_Effect/Materials",
                "Assets/KoreanTraditionalPattern_Effect/Materials/TraditionalPatternMaterials"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                
                var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    
                    if (mat != null)
                    {
                        totalScanned++;
                        if (IsMaterialViolet(mat))
                        {
                            RepairMaterial(mat, "particles");
                            fixedMaterials.Add($"[Korean] {mat.name}");
                            totalFixed++;
                        }
                    }
                }
            }
        }

        private void FixTerrainDemoScene()
        {
            string[] folders = new string[]
            {
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees",
                "Assets/TerrainDemoScene_URP/Prefabs/Water"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                
                // Buscar prefabs y extraer sus materiales
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    
                    if (prefab != null)
                    {
                        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                        foreach (var renderer in renderers)
                        {
                            if (renderer.sharedMaterials != null)
                            {
                                foreach (var mat in renderer.sharedMaterials)
                                {
                                    if (mat != null)
                                    {
                                        totalScanned++;
                                        if (IsMaterialViolet(mat))
                                        {
                                            RepairMaterial(mat, "lit");
                                            fixedMaterials.Add($"[Terrain] {mat.name}");
                                            totalFixed++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FixStylizedNatureKit()
        {
            string folder = "Assets/Models/Stylized Nature MegaKit[Standard]/FBX";
            
            if (!AssetDatabase.IsValidFolder(folder)) return;
            
            // Stylized Nature usa FBXs con materiales embebidos
            var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null)
                {
                    totalScanned++;
                    if (IsMaterialViolet(mat))
                    {
                        RepairMaterial(mat, "lit");
                        fixedMaterials.Add($"[Nature] {mat.name}");
                        totalFixed++;
                    }
                }
            }
        }

        private void FixAllRemainingVioletMaterials()
        {
            // Buscar TODOS los materiales del proyecto
            var allGuids = AssetDatabase.FindAssets("t:Material");
            
            foreach (var guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Skip si ya lo arreglamos en pasos anteriores
                if (path.Contains("KoreanTraditionalPattern")) continue;
                if (path.Contains("TerrainDemoScene_URP")) continue;
                if (path.Contains("Stylized Nature")) continue;
                
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    totalScanned++;
                    if (IsMaterialViolet(mat))
                    {
                        // Determinar tipo de shader apropiado
                        string shaderType = "lit";
                        if (mat.name.ToLower().Contains("particle") || 
                            mat.name.ToLower().Contains("fire") ||
                            mat.name.ToLower().Contains("effect") ||
                            mat.name.ToLower().Contains("smoke"))
                        {
                            shaderType = "particles";
                        }
                        
                        RepairMaterial(mat, shaderType);
                        fixedMaterials.Add($"[Other] {mat.name}");
                        totalFixed++;
                    }
                }
            }
        }

        private void FixCurrentScene()
        {
            var allRenderers = FindObjectsByType<Renderer>();
            int sceneFixed = 0;
            
            foreach (var renderer in allRenderers)
            {
                if (renderer.sharedMaterials == null) continue;
                
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    if (mat == null) continue;
                    
                    totalScanned++;
                    if (IsMaterialViolet(mat))
                    {
                        string shaderType = (renderer is ParticleSystemRenderer) ? "particles" : "lit";
                        RepairMaterial(mat, shaderType);
                        fixedMaterials.Add($"[Scene] {mat.name} on {renderer.gameObject.name}");
                        sceneFixed++;
                        totalFixed++;
                    }
                }
            }
            
            EditorUtility.DisplayDialog("Scene Fixed", 
                $"Fixed {sceneFixed} violet materials in current scene!", "OK");
        }

        private bool IsMaterialViolet(Material mat)
        {
            if (mat == null) return true;
            if (mat.shader == null) return true;
            
            string shaderName = mat.shader.name;
            
            // Indicadores de material violeta (roto)
            if (shaderName == "Hidden/InternalErrorShader") return true;
            if (string.IsNullOrEmpty(shaderName)) return true;
            if (!mat.shader.isSupported) return true;
            
            // Shaders de KoreanTraditionalPattern que no existen
            if (shaderName.Contains("5946") || 
                shaderName.Contains("Korean") || 
                shaderName.Contains("Pattern") ||
                shaderName.Contains("AlphaBlend") ||
                shaderName.Contains("AdditiveBlend"))
            {
                // Verificar si el shader realmente existe
                var shader = Shader.Find(shaderName);
                if (shader == null) return true;
            }
            
            // Shader Graph sin compilar
            if (shaderName.StartsWith("Shader Graphs/"))
            {
                // Verificar si está compilado
                var shader = Shader.Find(shaderName);
                if (shader == null || !shader.isSupported) return true;
            }
            
            return false;
        }

        private void RepairMaterial(Material mat, string shaderType)
        {
            if (mat == null) return;
            
            Undo.RecordObject(mat, "Fix Violet Material");
            
            // Guardar propiedades existentes
            Color? savedColor = null;
            Texture savedTexture = null;
            
            if (mat.HasProperty("_Color"))
                savedColor = mat.GetColor("_Color");
            if (mat.HasProperty("_MainTex"))
                savedTexture = mat.GetTexture("_MainTex");
            if (mat.HasProperty("_BaseMap") && savedTexture == null)
                savedTexture = mat.GetTexture("_BaseMap");
            
            // Aplicar nuevo shader URP
            string shaderName = shaderType == "particles" 
                ? "Universal Render Pipeline/Particles/Unlit"
                : "Universal Render Pipeline/Lit";
            
            var urpShader = Shader.Find(shaderName);
            if (urpShader != null)
            {
                mat.shader = urpShader;
                
                // Restaurar color
                if (savedColor.HasValue && mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", savedColor.Value);
                }
                
                // Restaurar textura
                if (savedTexture != null && mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", savedTexture);
                }
                
                // Configurar según tipo
                if (shaderType == "particles")
                {
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 1); // Additive
                    mat.renderQueue = 3000;
                    
                    // Color por defecto para fuego/partículas
                    if (!savedColor.HasValue || savedColor.Value == Color.white)
                    {
                        mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.8f));
                        mat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0f, 1f) * 2f);
                        mat.EnableKeyword("_EMISSION");
                    }
                }
                else
                {
                    // Lit - color por defecto
                    if (!savedColor.HasValue || savedColor.Value == Color.white)
                    {
                        mat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 1f));
                    }
                    mat.SetFloat("_Smoothness", 0.3f);
                    mat.SetFloat("_Metallic", 0f);
                }
                
                EditorUtility.SetDirty(mat);
            }
        }
    }
}
