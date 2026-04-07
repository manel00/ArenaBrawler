using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Procesador por lotes para reparar materiales en múltiples escenas
    /// </summary>
    public class MaterialBatchProcessor : EditorWindow
    {
        private List<SceneAsset> scenesToProcess = new List<SceneAsset>();
        private Vector2 scrollPos;
        private bool processAllScenes = false;
        private string statusMessage = "";
        private bool isProcessing = false;
        private int processedCount = 0;
        private int fixedCount = 0;

        // Material replacement rules
        private List<ShaderReplacementRule> replacementRules = new List<ShaderReplacementRule>();

        private struct ShaderReplacementRule
        {
            public string brokenShaderPattern;
            public string replacementShader;
            public bool enabled;
        }

        [MenuItem("Window/Arena Enhanced/Batch Material Processor")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialBatchProcessor>("Batch Material Processor");
            window.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            // Default replacement rules
            replacementRules = new List<ShaderReplacementRule>
            {
                new ShaderReplacementRule { 
                    brokenShaderPattern = "KoreanTraditionalPattern", 
                    replacementShader = "Universal Render Pipeline/Particles/Unlit",
                    enabled = true 
                },
                new ShaderReplacementRule { 
                    brokenShaderPattern = "Shader Graphs", 
                    replacementShader = "Universal Render Pipeline/Lit",
                    enabled = true 
                },
                new ShaderReplacementRule { 
                    brokenShaderPattern = "Hidden/InternalErrorShader", 
                    replacementShader = "Universal Render Pipeline/Lit",
                    enabled = true 
                },
                new ShaderReplacementRule { 
                    brokenShaderPattern = "5946", 
                    replacementShader = "Universal Render Pipeline/Particles/Unlit",
                    enabled = true 
                }
            };
        }

        private void OnGUI()
        {
            GUILayout.Label("Batch Material Processor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (isProcessing)
            {
                EditorGUILayout.HelpBox($"Processing... {processedCount} scenes processed, {fixedCount} materials fixed", MessageType.Info);
                GUILayout.FlexibleSpace();
                return;
            }

            // Options
            processAllScenes = GUILayout.Toggle(processAllScenes, "Process All Scenes in Project");
            EditorGUILayout.Space();

            if (!processAllScenes)
            {
                GUILayout.Label("Scenes to Process:", EditorStyles.boldLabel);
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                for (int i = 0; i < scenesToProcess.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    scenesToProcess[i] = (SceneAsset)EditorGUILayout.ObjectField(
                        scenesToProcess[i], typeof(SceneAsset), false);
                    
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        scenesToProcess.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Add Scene"))
                {
                    scenesToProcess.Add(null);
                }
            }

            EditorGUILayout.Space();
            
            // Replacement rules
            GUILayout.Label("Replacement Rules:", EditorStyles.boldLabel);
            for (int i = 0; i < replacementRules.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                var rule = replacementRules[i];
                rule.enabled = GUILayout.Toggle(rule.enabled, "", GUILayout.Width(20));
                GUILayout.Label("If shader contains:", GUILayout.Width(100));
                rule.brokenShaderPattern = EditorGUILayout.TextField(rule.brokenShaderPattern, GUILayout.Width(150));
                GUILayout.Label("→ Replace with:", GUILayout.Width(90));
                rule.replacementShader = EditorGUILayout.TextField(rule.replacementShader);
                replacementRules[i] = rule;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("START BATCH PROCESS", GUILayout.Height(40)))
            {
                StartBatchProcess();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Auto-Detect Broken Shaders", GUILayout.Height(40)))
            {
                AutoDetectBrokenShaders();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Status
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Quick Actions:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix KoreanTraditionalPattern"))
            {
                FixKoreanTraditionalPatternMaterials();
            }
            if (GUILayout.Button("Fix All Shader Graphs"))
            {
                FixAllShaderGraphMaterials();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void StartBatchProcess()
        {
            if (!processAllScenes && scenesToProcess.All(s => s == null))
            {
                statusMessage = "Error: No scenes selected!";
                return;
            }

            isProcessing = true;
            processedCount = 0;
            fixedCount = 0;

            // Get scenes to process
            List<string> scenePaths = new List<string>();
            
            if (processAllScenes)
            {
                var guids = AssetDatabase.FindAssets("t:Scene");
                scenePaths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToList();
            }
            else
            {
                scenePaths = scenesToProcess
                    .Where(s => s != null)
                    .Select(s => AssetDatabase.GetAssetPath(s))
                    .ToList();
            }

            ProcessScenes(scenePaths);
        }

        private void ProcessScenes(List<string> scenePaths)
        {
            int total = scenePaths.Count;
            
            for (int i = 0; i < scenePaths.Count; i++)
            {
                string path = scenePaths[i];
                EditorUtility.DisplayProgressBar("Processing Scenes", 
                    $"{Path.GetFileNameWithoutExtension(path)} ({i+1}/{total})", 
                    (float)i / total);

                // Open scene
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, 
                    UnityEditor.SceneManagement.OpenSceneMode.Additive);

                int sceneFixed = ProcessScene(scene);
                fixedCount += sceneFixed;
                processedCount++;

                // Save if changes were made
                if (sceneFixed > 0)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                }

                // Close scene
                if (scene != currentScene)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                }
            }

            EditorUtility.ClearProgressBar();
            
            isProcessing = false;
            statusMessage = $"Batch complete! Processed {processedCount} scenes, fixed {fixedCount} materials.";
            
            Debug.Log($"[MaterialBatchProcessor] {statusMessage}");
            
            // Refresh asset database
            AssetDatabase.Refresh();
        }

        private int ProcessScene(UnityEngine.SceneManagement.Scene scene)
        {
            int fixedInScene = 0;
            var renderers = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Renderer>(true))
                .ToArray();

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null) continue;

                var materials = renderer.sharedMaterials;
                bool modified = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat == null) continue;

                    string shaderName = mat.shader?.name ?? "";
                    
                    // Check if this material needs fixing
                    foreach (var rule in replacementRules)
                    {
                        if (!rule.enabled) continue;
                        
                        if (shaderName.Contains(rule.brokenShaderPattern) || 
                            shaderName == "Hidden/InternalErrorShader" ||
                            string.IsNullOrEmpty(shaderName))
                        {
                            Undo.RecordObject(mat, "Fix Material Shader");
                            
                            // Store original color/texture
                            Color? originalColor = null;
                            Texture originalTexture = null;
                            
                            if (mat.HasProperty("_Color"))
                                originalColor = mat.GetColor("_Color");
                            if (mat.HasProperty("_MainTex"))
                                originalTexture = mat.GetTexture("_MainTex");

                            // Apply new shader
                            var newShader = Shader.Find(rule.replacementShader);
                            if (newShader != null)
                            {
                                mat.shader = newShader;
                                
                                // Restore color/texture if possible
                                if (originalColor.HasValue && mat.HasProperty("_BaseColor"))
                                    mat.SetColor("_BaseColor", originalColor.Value);
                                if (originalTexture != null && mat.HasProperty("_BaseMap"))
                                    mat.SetTexture("_BaseMap", originalTexture);

                                EditorUtility.SetDirty(mat);
                                modified = true;
                                fixedInScene++;
                            }
                            break;
                        }
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (fixedInScene > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            }

            return fixedInScene;
        }

        private void AutoDetectBrokenShaders()
        {
            var allMaterials = AssetDatabase.FindAssets("t:Material")
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null)
                .ToList();

            var brokenShaders = new HashSet<string>();
            
            foreach (var mat in allMaterials)
            {
                string shaderName = mat.shader?.name ?? "";
                
                if (mat.shader == null ||
                    shaderName == "Hidden/InternalErrorShader" ||
                    string.IsNullOrEmpty(shaderName) ||
                    !mat.shader.isSupported)
                {
                    brokenShaders.Add(shaderName);
                }
            }

            statusMessage = $"Found {brokenShaders.Count} unique broken shaders:\n" + 
                string.Join("\n", brokenShaders.Take(10));

            // Add rules for detected broken shaders
            foreach (var shaderName in brokenShaders.Take(5))
            {
                if (!string.IsNullOrEmpty(shaderName) && 
                    !replacementRules.Any(r => shaderName.Contains(r.brokenShaderPattern)))
                {
                    replacementRules.Add(new ShaderReplacementRule
                    {
                        brokenShaderPattern = shaderName.Substring(0, Mathf.Min(20, shaderName.Length)),
                        replacementShader = "Universal Render Pipeline/Lit",
                        enabled = true
                    });
                }
            }
        }

        private void FixKoreanTraditionalPatternMaterials()
        {
            // Find all materials with KoreanTraditionalPattern
            var materials = AssetDatabase.FindAssets("t:Material")
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && m.shader != null && 
                    m.shader.name.Contains("KoreanTraditionalPattern"))
                .ToList();

            int fixedCount = 0;
            foreach (var mat in materials)
            {
                Undo.RecordObject(mat, "Fix KoreanTraditionalPattern Material");
                
                // Store texture reference
                Texture mainTex = null;
                if (mat.HasProperty("_MainTex"))
                    mainTex = mat.GetTexture("_MainTex");
                if (mat.HasProperty("_Flow"))
                    mainTex = mat.GetTexture("_Flow");
                if (mat.HasProperty("_Mask"))
                    mainTex = mat.GetTexture("_Mask");

                // Apply URP particle shader
                mat.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                
                // Restore texture
                if (mainTex != null && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", mainTex);

                // Set default particle color
                mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.8f));
                mat.SetColor("_EmissionColor", new Color(2f, 0.5f, 0f, 1f));
                mat.EnableKeyword("_EMISSION");

                // Configure for transparency
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 1); // Additive
                mat.renderQueue = 3000;

                EditorUtility.SetDirty(mat);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            statusMessage = $"Fixed {fixedCount} KoreanTraditionalPattern materials!";
            Debug.Log($"[MaterialBatchProcessor] {statusMessage}");
        }

        private void FixAllShaderGraphMaterials()
        {
            var materials = AssetDatabase.FindAssets("t:Material")
                .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && m.shader != null && 
                    m.shader.name.StartsWith("Shader Graphs/"))
                .ToList();

            int fixedCount = 0;
            foreach (var mat in materials)
            {
                Undo.RecordObject(mat, "Fix Shader Graph Material");
                
                // Determine appropriate shader
                string newShader = "Universal Render Pipeline/Lit";
                if (mat.shader.name.Contains("Particle") || mat.shader.name.Contains("VFX"))
                {
                    newShader = "Universal Render Pipeline/Particles/Unlit";
                }
                else if (mat.shader.name.Contains("Sprite"))
                {
                    newShader = "Universal Render Pipeline/Sprites/Default";
                }

                // Store properties
                Color? baseColor = null;
                Texture mainTex = null;
                
                if (mat.HasProperty("_Color"))
                    baseColor = mat.GetColor("_Color");
                if (mat.HasProperty("_MainTex"))
                    mainTex = mat.GetTexture("_MainTex");

                // Apply new shader
                mat.shader = Shader.Find(newShader);

                // Restore properties
                if (baseColor.HasValue && mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", baseColor.Value);
                if (mainTex != null && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", mainTex);

                EditorUtility.SetDirty(mat);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            statusMessage = $"Fixed {fixedCount} Shader Graph materials!";
            Debug.Log($"[MaterialBatchProcessor] {statusMessage}");
        }
    }
}
