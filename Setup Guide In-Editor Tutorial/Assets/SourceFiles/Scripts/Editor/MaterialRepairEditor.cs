using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Editor tool para diagnosticar y reparar materiales violetas (faltantes) en el proyecto
    /// </summary>
    public class MaterialRepairEditor : EditorWindow
    {
        private Vector2 scrollPos;
        private List<MaterialIssue> issues = new List<MaterialIssue>();
        private bool showOnlyErrors = true;
        private bool autoFixOnScan = false;
        private int totalObjects = 0;
        private int brokenMaterials = 0;
        private int fixedMaterials = 0;

        private struct MaterialIssue
        {
            public GameObject gameObject;
            public Renderer renderer;
            public Material material;
            public string issueType;
            public string originalShader;
            public bool wasFixed;
        }

        [MenuItem("Window/Arena Enhanced/Material Repair Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialRepairEditor>("Material Repair");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("Material Repair Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Stats
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Total Objects: {totalObjects}", EditorStyles.miniLabel);
            GUILayout.Label($"Broken Materials: {brokenMaterials}", EditorStyles.miniLabel);
            GUILayout.Label($"Fixed Materials: {fixedMaterials}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // Options
            EditorGUILayout.BeginHorizontal();
            showOnlyErrors = GUILayout.Toggle(showOnlyErrors, "Show Only Errors", GUILayout.Width(150));
            autoFixOnScan = GUILayout.Toggle(autoFixOnScan, "Auto-Fix on Scan", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
            {
                ScanScene();
            }
            if (GUILayout.Button("Scan All Scenes", GUILayout.Height(30)))
            {
                ScanAllScenes();
            }
            if (GUILayout.Button("Fix All", GUILayout.Height(30)))
            {
                FixAllMaterials();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Results list
            GUILayout.Label("Results:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            var filteredIssues = showOnlyErrors ? issues.Where(i => !i.wasFixed).ToList() : issues;

            foreach (var issue in filteredIssues)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                GUILayout.Label(issue.gameObject.name, EditorStyles.boldLabel);
                GUILayout.Label($"Shader: {issue.originalShader}", EditorStyles.miniLabel);
                GUILayout.Label($"Issue: {issue.issueType}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                if (!issue.wasFixed && GUILayout.Button("Fix", GUILayout.Height(40)))
                {
                    FixMaterial(issue);
                }
                else if (issue.wasFixed)
                {
                    GUILayout.Label("FIXED", EditorStyles.boldLabel);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanScene()
        {
            issues.Clear();
            totalObjects = 0;
            brokenMaterials = 0;
            fixedMaterials = 0;

            var allRenderers = FindObjectsByType<Renderer>();
            totalObjects = allRenderers.Length;

            foreach (var renderer in allRenderers)
            {
                if (renderer.sharedMaterials == null) continue;

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    if (mat == null)
                    {
                        issues.Add(new MaterialIssue
                        {
                            gameObject = renderer.gameObject,
                            renderer = renderer,
                            material = null,
                            issueType = "Missing Material (null)",
                            originalShader = "null",
                            wasFixed = false
                        });
                        brokenMaterials++;
                        continue;
                    }

                    var issue = AnalyzeMaterial(mat, renderer);
                    if (issue.HasValue)
                    {
                        issues.Add(issue.Value);
                        brokenMaterials++;
                    }
                }
            }

            if (autoFixOnScan)
            {
                FixAllMaterials();
            }

            Debug.Log($"[MaterialRepairEditor] Scan complete: {brokenMaterials} broken materials found in {totalObjects} renderers");
        }

        private void ScanAllScenes()
        {
            // Get all scene assets
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            int totalScenes = sceneGuids.Length;
            
            EditorUtility.DisplayProgressBar("Scanning All Scenes", "Starting...", 0);

            issues.Clear();
            totalObjects = 0;
            brokenMaterials = 0;
            fixedMaterials = 0;

            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar("Scanning All Scenes", path, (float)i / totalScenes);

                // Open scene additively to scan it
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                
                var sceneRenderers = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<Renderer>(true))
                    .ToArray();

                totalObjects += sceneRenderers.Length;

                foreach (var renderer in sceneRenderers)
                {
                    if (renderer.sharedMaterials == null) continue;

                    for (int j = 0; j < renderer.sharedMaterials.Length; j++)
                    {
                        var mat = renderer.sharedMaterials[j];
                        if (mat == null)
                        {
                            issues.Add(new MaterialIssue
                            {
                                gameObject = renderer.gameObject,
                                renderer = renderer,
                                material = null,
                                issueType = $"Missing Material in {scene.name}",
                                originalShader = "null",
                                wasFixed = false
                            });
                            brokenMaterials++;
                            continue;
                        }

                        var issue = AnalyzeMaterial(mat, renderer);
                        if (issue.HasValue)
                        {
                            issues.Add(issue.Value);
                            brokenMaterials++;
                        }
                    }
                }

                // Close the additive scene
                if (scene != currentScene)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                }
            }

            EditorUtility.ClearProgressBar();

            if (autoFixOnScan)
            {
                FixAllMaterials();
            }

            Debug.Log($"[MaterialRepairEditor] Full project scan complete: {brokenMaterials} broken materials found in {totalScenes} scenes");
        }

        private MaterialIssue? AnalyzeMaterial(Material mat, Renderer renderer)
        {
            string shaderName = mat.shader?.name ?? "null";
            string issueType = null;

            // Check for error shader
            if (mat.shader == null)
            {
                issueType = "Shader is NULL";
            }
            else if (shaderName == "Hidden/InternalErrorShader")
            {
                issueType = "Internal Error Shader";
            }
            else if (string.IsNullOrEmpty(shaderName))
            {
                issueType = "Empty Shader Name";
            }
            else if (!mat.shader.isSupported)
            {
                issueType = "Shader Not Supported";
            }
            // Check for broken Shader Graph shaders
            else if (shaderName.StartsWith("Shader Graphs/") && !IsShaderGraphValid(mat.shader))
            {
                issueType = "Broken Shader Graph";
            }
            else if (shaderName.Contains("Korean") || shaderName.Contains("Pattern") || shaderName.Contains("5946"))
            {
                issueType = "Missing KoreanTraditionalPattern Shader";
            }
            else if (shaderName.Contains("Hidden/") && !shaderName.Contains("InternalErrorShader"))
            {
                issueType = "Hidden Shader (possibly broken)";
            }

            if (issueType != null)
            {
                return new MaterialIssue
                {
                    gameObject = renderer.gameObject,
                    renderer = renderer,
                    material = mat,
                    issueType = issueType,
                    originalShader = shaderName,
                    wasFixed = false
                };
            }

            return null;
        }

        private bool IsShaderGraphValid(Shader shader)
        {
            // If shader graph file doesn't exist or has errors, it's invalid
            // This is a simplified check - in reality we'd need to check the shader asset
            return false; // Assume Shader Graph shaders need fixing for now
        }

        private void FixAllMaterials()
        {
            for (int i = 0; i < issues.Count; i++)
            {
                if (!issues[i].wasFixed)
                {
                    var issue = issues[i];
                    if (FixMaterial(issue))
                    {
                        issue.wasFixed = true;
                        issues[i] = issue;
                        fixedMaterials++;
                    }
                }
            }

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log($"[MaterialRepairEditor] Fixed {fixedMaterials} materials");
        }

        private bool FixMaterial(MaterialIssue issue)
        {
            if (issue.material == null)
            {
                // Create a new basic material
                var newMat = CreateBasicMaterial(issue.renderer);
                Undo.RecordObject(issue.renderer, "Fix Missing Material");
                
                var materials = issue.renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        materials[i] = newMat;
                    }
                }
                issue.renderer.sharedMaterials = materials;
                
                EditorUtility.SetDirty(issue.renderer);
                return true;
            }

            // Determine appropriate replacement shader
            string newShader = DetermineReplacementShader(issue.renderer, issue.material);

            // Create material asset if it doesn't exist
            string assetPath = AssetDatabase.GetAssetPath(issue.material);
            bool isAsset = !string.IsNullOrEmpty(assetPath);

            Undo.RecordObject(issue.material, "Fix Material Shader");
            
            var oldShader = issue.material.shader;
            issue.material.shader = Shader.Find(newShader);

            // Copy properties if possible
            CopyMaterialProperties(issue.material, oldShader);

            if (isAsset)
            {
                EditorUtility.SetDirty(issue.material);
            }
            else
            {
                // Runtime material - replace on renderer
                Undo.RecordObject(issue.renderer, "Replace Runtime Material");
                var newMat = new Material(Shader.Find(newShader));
                CopyMaterialProperties(issue.material, newMat);
                
                var materials = issue.renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == issue.material)
                    {
                        materials[i] = newMat;
                    }
                }
                issue.renderer.sharedMaterials = materials;
            }

            EditorUtility.SetDirty(issue.renderer);
            return true;
        }

        private Material CreateBasicMaterial(Renderer renderer)
        {
            string shaderName = DetermineReplacementShader(renderer, null);
            var mat = new Material(Shader.Find(shaderName));
            mat.name = "Auto_Fixed_Material_" + renderer.gameObject.name;

            // Set default colors based on renderer type
            if (renderer is ParticleSystemRenderer)
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.4f, 0f, 0.8f));
                mat.SetColor("_EmissionColor", new Color(2f, 0.5f, 0f, 1f));
                mat.EnableKeyword("_EMISSION");
            }
            else if (renderer is SpriteRenderer)
            {
                mat.SetColor("_Color", Color.white);
            }
            else
            {
                mat.SetColor("_BaseColor", Color.gray);
            }

            return mat;
        }

        private string DetermineReplacementShader(Renderer renderer, Material originalMat)
        {
            if (renderer is ParticleSystemRenderer)
            {
                return "Universal Render Pipeline/Particles/Unlit";
            }
            else if (renderer is SpriteRenderer)
            {
                return "Universal Render Pipeline/Sprites/Default";
            }
            else if (renderer is LineRenderer || renderer is TrailRenderer)
            {
                return "Universal Render Pipeline/Particles/Unlit";
            }
            else
            {
                // Check if original material had transparency
                if (originalMat != null)
                {
                    string shaderName = originalMat.shader?.name ?? "";
                    if (shaderName.Contains("Transparent") || shaderName.Contains("Unlit"))
                    {
                        // Check if emission was enabled
                        if (originalMat.IsKeywordEnabled("_EMISSION") || 
                            originalMat.HasProperty("_Emission") && originalMat.GetFloat("_Emission") > 0)
                        {
                            return "Universal Render Pipeline/Particles/Unlit"; // Good for glowing/transparent
                        }
                    }
                }

                return "Universal Render Pipeline/Lit";
            }
        }

        private void CopyMaterialProperties(Material mat, Shader oldShader)
        {
            // Try to preserve common properties
            if (mat.HasProperty("_Color"))
            {
                var color = mat.GetColor("_Color");
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
            }

            // Handle emission
            if (mat.HasProperty("_Emission") || mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
            }

            // Copy main texture
            if (mat.HasProperty("_MainTex"))
            {
                var tex = mat.GetTexture("_MainTex");
                if (tex != null && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
            }
        }

        private void CopyMaterialProperties(Material source, Material dest)
        {
            if (source.HasProperty("_Color"))
                dest.SetColor("_Color", source.GetColor("_Color"));
            if (source.HasProperty("_BaseColor"))
                dest.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            if (source.HasProperty("_MainTex"))
                dest.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
        }
    }
}
