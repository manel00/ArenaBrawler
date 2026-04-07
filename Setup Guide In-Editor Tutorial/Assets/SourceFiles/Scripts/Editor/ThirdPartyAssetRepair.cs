using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Arreglo específico para assets de terceros que generan materiales violetas
    /// KoreanTraditionalPattern_Effect, TerrainDemoScene_URP, Synty, etc.
    /// </summary>
    public class ThirdPartyAssetRepair : EditorWindow
    {
        private Vector2 scrollPos;
        private List<AssetIssue> issues = new List<AssetIssue>();
        private AddRequest shaderGraphRequest;

        private struct AssetIssue
        {
            public string assetName;
            public string assetPath;
            public string issue;
            public bool canFix;
            public bool fixed_status;
        }

        [MenuItem("Window/Arena Enhanced/Third-Party Asset Repair")]
        public static void ShowWindow()
        {
            var window = GetWindow<ThirdPartyAssetRepair>("3rd Party Asset Repair");
            window.minSize = new Vector2(600, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Third-Party Asset Repair Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Herramienta para arreglar assets de terceros que muestran materiales violetas:\n" +
                "• KoreanTraditionalPattern_Effect\n" +
                "• TerrainDemoScene_URP\n" +
                "• Synty Polygon\n" +
                "• Cualquier asset con Shader Graph faltante", 
                MessageType.Info);

            EditorGUILayout.Space();

            // Check Package Status
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Shader Graph Package Status:", EditorStyles.boldLabel);
            
            bool hasShaderGraph = CheckShaderGraphPackage();
            if (hasShaderGraph)
            {
                EditorGUILayout.LabelField("✓ Shader Graph package is INSTALLED", 
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
            }
            else
            {
                EditorGUILayout.LabelField("✗ Shader Graph package is MISSING", 
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                
                if (GUILayout.Button("Install Shader Graph Package", GUILayout.Height(30)))
                {
                    InstallShaderGraph();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Quick Actions
            EditorGUILayout.LabelField("Quick Fix Actions:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix KoreanTraditionalPattern", GUILayout.Height(35)))
            {
                FixKoreanTraditionalPattern();
            }
            if (GUILayout.Button("Fix TerrainDemoScene", GUILayout.Height(35)))
            {
                FixTerrainDemoScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix Synty Materials", GUILayout.Height(35)))
            {
                FixSyntyMaterials();
            }
            if (GUILayout.Button("Fix ALL Third-Party Assets", GUILayout.Height(35)))
            {
                FixAllThirdPartyAssets();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Deep repair
            if (GUILayout.Button("DEEP REPAIR: Reimport All Shaders", GUILayout.Height(40)))
            {
                DeepRepairAllShaders();
            }

            EditorGUILayout.Space();

            // Status
            if (issues.Count > 0)
            {
                GUILayout.Label($"Issues Found: {issues.Count}", EditorStyles.boldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                
                foreach (var issue in issues)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label($"{issue.assetName}: {issue.issue}", GUILayout.Width(400));
                    if (issue.fixed_status)
                    {
                        GUILayout.Label("FIXED", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }

            // Refresh Database
            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Asset Database", GUILayout.Height(30)))
            {
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Refresh Complete", "Asset database refreshed!", "OK");
            }
        }

        private bool CheckShaderGraphPackage()
        {
            // Check if Shader Graph is available
            var shader = Shader.Find("Shader Graphs/Test");
            // Alternative check: look for Shader Graph package in manifest
            string manifestPath = "Packages/manifest.json";
            if (System.IO.File.Exists(manifestPath))
            {
                string manifest = System.IO.File.ReadAllText(manifestPath);
                return manifest.Contains("com.unity.shadergraph");
            }
            return false;
        }

        private void InstallShaderGraph()
        {
            #if UNITY_2021_1_OR_NEWER
            shaderGraphRequest = Client.Add("com.unity.shadergraph");
            EditorUtility.DisplayDialog("Installing", 
                "Shader Graph package installation started. Please wait for import to complete.", "OK");
            #else
            EditorUtility.DisplayDialog("Manual Install Required", 
                "Please install Shader Graph via Window > Package Manager > Unity Registry > Shader Graph", "OK");
            #endif
        }

        private void FixKoreanTraditionalPattern()
        {
            int fixedCount = 0;
            issues.Clear();

            // 1. Find all Shader Graph files
            string shaderFolder = "Assets/KoreanTraditionalPattern_Effect/Shader";
            var shaderGraphGuids = AssetDatabase.FindAssets("t:Shader", new[] { shaderFolder });
            
            foreach (var guid in shaderGraphGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            // 2. Find all materials that use these broken shaders
            string materialFolder = "Assets/KoreanTraditionalPattern_Effect/Materials";
            var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialFolder });

            foreach (var guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.shader != null)
                {
                    string shaderName = mat.shader.name;
                    
                    // Check if shader is broken
                    bool isBroken = shaderName.Contains("5946") ||
                                   shaderName == "Hidden/InternalErrorShader" ||
                                   string.IsNullOrEmpty(shaderName) ||
                                   (shaderName.StartsWith("Shader Graphs/") && !mat.shader.isSupported);

                    if (isBroken)
                    {
                        issues.Add(new AssetIssue
                        {
                            assetName = mat.name,
                            assetPath = path,
                            issue = $"Broken shader: {shaderName}",
                            canFix = true,
                            fixed_status = false
                        });

                        // FIX: Replace with URP Particles shader
                        Undo.RecordObject(mat, "Fix KoreanTraditionalPattern Material");
                        
                        // Store texture and color before switching
                        Texture savedTex = null;
                        Color savedColor = Color.white;
                        
                        if (mat.HasProperty("_MainTex"))
                            savedTex = mat.GetTexture("_MainTex");
                        if (mat.HasProperty("_Flow"))
                            savedTex = mat.GetTexture("_Flow");
                        if (mat.HasProperty("_Mask"))
                            savedTex = mat.GetTexture("_Mask");
                        if (mat.HasProperty("_Color"))
                            savedColor = mat.GetColor("_Color");

                        // Apply URP shader
                        var urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                        if (urpShader != null)
                        {
                            mat.shader = urpShader;
                            
                            // Restore properties
                            if (savedTex != null && mat.HasProperty("_BaseMap"))
                                mat.SetTexture("_BaseMap", savedTex);
                            
                            // Set appropriate particle colors
                            mat.SetColor("_BaseColor", savedColor != Color.white ? savedColor : new Color(1f, 0.4f, 0f, 0.8f));
                            mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0f, 1f) * 1.5f);
                            mat.EnableKeyword("_EMISSION");
                            
                            // Configure as transparent/additive
                            mat.SetFloat("_Surface", 1); // Transparent
                            mat.SetFloat("_Blend", 1); // Additive
                            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                            mat.SetFloat("_ZWrite", 0);
                            mat.renderQueue = 3000;

                            EditorUtility.SetDirty(mat);
                            fixedCount++;
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            
            // Update issues status
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                issue.fixed_status = true;
                issues[i] = issue;
            }

            EditorUtility.DisplayDialog("KoreanTraditionalPattern Fixed", 
                $"Fixed {fixedCount} materials in KoreanTraditionalPattern_Effect!", "OK");
            
            Debug.Log($"[ThirdPartyAssetRepair] Fixed {fixedCount} KoreanTraditionalPattern materials");
        }

        private void FixTerrainDemoScene()
        {
            int fixedCount = 0;
            
            // Find all materials in TerrainDemoScene
            string[] folders = new string[]
            {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Materials",
                "Assets/TerrainDemoScene_URP/Prefabs/Terrain/Materials",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees",
                "Assets/TerrainDemoScene_URP/Prefabs/Water/Materials"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                
                var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    
                    if (mat != null && mat.shader != null)
                    {
                        // Check if material is using built-in instead of URP
                        string shaderName = mat.shader.name;
                        if (shaderName.StartsWith("Standard") || 
                            shaderName.StartsWith("Legacy Shaders/") ||
                            shaderName == "Hidden/InternalErrorShader")
                        {
                            Undo.RecordObject(mat, "Fix TerrainDemoScene Material");
                            
                            // Store color
                            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                            
                            // Switch to URP Lit
                            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                            if (urpShader != null)
                            {
                                mat.shader = urpShader;
                                mat.SetColor("_BaseColor", color);
                                EditorUtility.SetDirty(mat);
                                fixedCount++;
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("TerrainDemoScene Fixed", 
                $"Fixed {fixedCount} materials in TerrainDemoScene_URP!", "OK");
        }

        private void FixSyntyMaterials()
        {
            int fixedCount = 0;
            
            string[] syntyFolders = new string[]
            {
                "Assets/Synty/PolygonGeneric/Materials",
                "Assets/Synty/PolygonPrideWeapons/Materials"
            };

            foreach (var folder in syntyFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                
                var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    
                    if (mat != null && mat.shader != null)
                    {
                        string shaderName = mat.shader.name;
                        
                        // Synty materials often use Standard shader, convert to URP
                        if (shaderName.StartsWith("Standard") && !shaderName.Contains("Render Pipeline"))
                        {
                            Undo.RecordObject(mat, "Fix Synty Material");
                            
                            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                            
                            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                            if (urpShader != null)
                            {
                                mat.shader = urpShader;
                                mat.SetColor("_BaseColor", color);
                                if (mainTex != null && mat.HasProperty("_BaseMap"))
                                    mat.SetTexture("_BaseMap", mainTex);
                                
                                EditorUtility.SetDirty(mat);
                                fixedCount++;
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Synty Materials Fixed", 
                $"Fixed {fixedCount} Synty materials!", "OK");
        }

        private void FixAllThirdPartyAssets()
        {
            int totalFixed = 0;
            
            // Fix all third-party asset categories
            FixKoreanTraditionalPattern();
            FixTerrainDemoScene();
            FixSyntyMaterials();
            
            // Additional fixes
            totalFixed += FixGenericBrokenMaterials();
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("All Third-Party Assets Fixed", 
                "All third-party assets have been processed! Check Console for details.", "OK");
        }

        private int FixGenericBrokenMaterials()
        {
            int fixedCount = 0;
            
            // Find ALL materials in the project
            var allMaterialGuids = AssetDatabase.FindAssets("t:Material");
            
            foreach (var guid in allMaterialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Skip our own materials
                if (path.Contains("/SourceFiles/Materials/")) continue;
                
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                
                // Check for broken shader
                bool needsFix = false;
                string shaderName = mat.shader?.name ?? "";
                
                if (mat.shader == null || 
                    shaderName == "Hidden/InternalErrorShader" ||
                    string.IsNullOrEmpty(shaderName))
                {
                    needsFix = true;
                }
                
                if (needsFix)
                {
                    Undo.RecordObject(mat, "Fix Generic Broken Material");
                    
                    var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader != null)
                    {
                        mat.shader = urpShader;
                        mat.SetColor("_BaseColor", Color.gray);
                        EditorUtility.SetDirty(mat);
                        fixedCount++;
                    }
                }
            }
            
            return fixedCount;
        }

        private void DeepRepairAllShaders()
        {
            // Force reimport of all shader-related assets
            string[] shaderFolders = new string[]
            {
                "Assets/KoreanTraditionalPattern_Effect/Shader",
                "Assets/Shaders",
                "Assets/VFX"
            };

            foreach (var folder in shaderFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                
                var guids = AssetDatabase.FindAssets("t:Shader", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
            }

            // Also reimport Shader Graph assets
            var shaderGraphGuids = AssetDatabase.FindAssets("t:Shader t:ShaderGraph", new[] { "Assets" });
            foreach (var guid in shaderGraphGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Deep Repair Complete", 
                "All shaders have been force-reimported. Please wait for compilation to finish.", "OK");
        }
    }
}
