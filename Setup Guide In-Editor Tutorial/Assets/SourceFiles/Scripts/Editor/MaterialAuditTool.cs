using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Analizador de materiales para detectar shaders rotos o faltantes
    /// </summary>
    public class MaterialAuditTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<MaterialIssue> issues = new List<MaterialIssue>();
        private bool isScanning = false;
        
        // GUIDs de shaders URP válidos conocidos
        private static readonly HashSet<string> ValidURPShaders = new HashSet<string>
        {
            "933532a4fcc9baf4fa0491de14d08ed7", // URP/Lit
            "0406db5a14f94604a8c57ccfbc9f3b46", // URP/Particles/Unlit
            "b7839dad96f4f8c4594e0c2e6b8b0e4f", // URP/Simple Lit
            "8516d5a9a52d94c46b9f4f7e62b091c3", // URP/Particles/Lit
            "69c1f799e772487aeb168dc95bf3e9a0", // URP/Unlit
        };
        
        private enum IssueType
        {
            MissingShader,      // Shader GUID no encontrado
            NullShader,         // fileID: 0
            UnknownShader,      // Shader no reconocido
            ShaderGraphMissing, // Shader Graph sin compilar
            ErrorShader         // InternalErrorShader
        }
        
        private class MaterialIssue
        {
            public string MaterialPath;
            public string MaterialName;
            public IssueType Type;
            public string ShaderGuid;
            public string ShaderName;
            public string Description;
        }
        
        [MenuItem("Window/Arena Enhanced/Material Audit Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialAuditTool>("Material Audit");
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Material Audit Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Escanea todos los materiales del proyecto para detectar:\n" +
                "• Shaders faltantes o rotos\n" +
                "• Referencias nulas\n" +
                "• Shader Graphs sin compilar\n" +
                "• Materiales que causarían color magenta", 
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("SCAN ALL MATERIALS", GUILayout.Height(40)) && !isScanning)
            {
                StartScan();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            
            // Mostrar resultados
            if (isScanning)
            {
                EditorGUILayout.LabelField("Scanning...", EditorStyles.boldLabel);
            }
            else if (issues.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {issues.Count} problematic materials:", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var issue in issues)
                {
                    EditorGUILayout.BeginVertical("box");
                    
                    GUI.color = GetIssueColor(issue.Type);
                    EditorGUILayout.LabelField($"⚠ {issue.MaterialName}", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                    
                    EditorGUILayout.LabelField($"Path: {issue.MaterialPath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Issue: {issue.Description}", EditorStyles.wordWrappedLabel);
                    
                    if (!string.IsNullOrEmpty(issue.ShaderGuid))
                    {
                        EditorGUILayout.LabelField($"Shader GUID: {issue.ShaderGuid}", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
                
                EditorGUILayout.EndScrollView();
            }
            else if (!isScanning)
            {
                EditorGUILayout.LabelField("No issues found. Click 'SCAN ALL MATERIALS' to start.", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        private Color GetIssueColor(IssueType type)
        {
            return type switch
            {
                IssueType.MissingShader => Color.red,
                IssueType.NullShader => Color.magenta,
                IssueType.ErrorShader => Color.red,
                _ => Color.yellow
            };
        }
        
        private void StartScan()
        {
            isScanning = true;
            issues.Clear();
            
            try
            {
                ScanMaterials();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MaterialAudit] Error during scan: {e}");
            }
            finally
            {
                isScanning = false;
                Repaint();
            }
        }
        
        private void ScanMaterials()
        {
            string assetsPath = Application.dataPath;
            string[] materialFiles = Directory.GetFiles(assetsPath, "*.mat", SearchOption.AllDirectories);
            
            Debug.Log($"[MaterialAudit] Scanning {materialFiles.Length} materials...");
            
            for (int i = 0; i < materialFiles.Length; i++)
            {
                string fullPath = materialFiles[i];
                string relativePath = "Assets" + fullPath.Replace(assetsPath, "").Replace("\\", "/");
                
                try
                {
                    ScanMaterialFile(fullPath, relativePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MaterialAudit] Error scanning {relativePath}: {e.Message}");
                }
                
                // Progreso cada 100 materiales
                if (i % 100 == 0)
                {
                    EditorUtility.DisplayProgressBar("Scanning Materials", 
                        $"Scanned {i}/{materialFiles.Length} materials...", 
                        (float)i / materialFiles.Length);
                }
            }
            
            EditorUtility.ClearProgressBar();
            
            Debug.Log($"[MaterialAudit] Scan complete. Found {issues.Count} issues.");
        }
        
        private void ScanMaterialFile(string fullPath, string relativePath)
        {
            string content = File.ReadAllText(fullPath);
            
            // Extraer nombre del material
            var nameMatch = Regex.Match(content, @"m_Name:\s*(.+)$", RegexOptions.Multiline);
            string materialName = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : Path.GetFileNameWithoutExtension(relativePath);
            
            // Buscar referencia al shader
            var shaderMatch = Regex.Match(content, @"m_Shader:\s*\{fileID:\s*(\d+),\s*guid:\s*([a-f0-9]+)");
            
            if (!shaderMatch.Success)
            {
                // No se encontró referencia de shader
                issues.Add(new MaterialIssue
                {
                    MaterialPath = relativePath,
                    MaterialName = materialName,
                    Type = IssueType.MissingShader,
                    Description = "No shader reference found in material"
                });
                return;
            }
            
            string fileID = shaderMatch.Groups[1].Value;
            string guid = shaderMatch.Groups[2].Value;
            
            // Verificar si es null
            if (fileID == "0" && guid == "00000000000000000000000000000000")
            {
                issues.Add(new MaterialIssue
                {
                    MaterialPath = relativePath,
                    MaterialName = materialName,
                    Type = IssueType.NullShader,
                    ShaderGuid = guid,
                    Description = "Shader reference is null (fileID: 0)"
                });
                return;
            }
            
            // Verificar si es GUID conocido URP
            if (!ValidURPShaders.Contains(guid))
            {
                // Verificar si el shader existe en la base de datos
                string shaderPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (string.IsNullOrEmpty(shaderPath))
                {
                    // Shader no encontrado - verificar si es Built-in
                    if (!IsBuiltInShaderGuid(guid))
                    {
                        issues.Add(new MaterialIssue
                        {
                            MaterialPath = relativePath,
                            MaterialName = materialName,
                            Type = IssueType.MissingShader,
                            ShaderGuid = guid,
                            Description = $"Shader GUID not found in project: {guid.Substring(0, 8)}..."
                        });
                    }
                }
            }
        }
        
        private bool IsBuiltInShaderGuid(string guid)
        {
            // GUIDs built-in comienzan con muchos ceros o tienen patrones específicos
            return guid.StartsWith("0000000000000000") || 
                   guid == "0000000000000000f000000000000000";
        }
    }
}
