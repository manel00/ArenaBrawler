using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Utilidad de editor para convertir materiales Built-in Render Pipeline a URP.
    /// Soluciona problemas de materiales magenta/pink en KoreanTraditionalPattern_Effect y otros assets.
    /// </summary>
    public class MaterialURPConverter : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<Material> materialsToConvert = new List<Material>();
        private bool showConverted = true;
        private string statusMessage = "";
        private Color statusColor = Color.white;

        [MenuItem("Tools/Arena Enhanced/Convert Materials to URP")]
        public static void ShowWindow()
        {
            GetWindow<MaterialURPConverter>("URP Material Converter");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            EditorGUILayout.LabelField("URP Material Converter", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Convierte materiales Built-in a URP automáticamente", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(10);
            
            // Botón para escanear KoreanTraditionalPattern_Effect
            EditorGUILayout.LabelField("1. Escanear Assets Problemáticos", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Escanear KoreanTraditionalPattern_Effect", GUILayout.Height(30)))
            {
                ScanKoreanTraditionalPatternEffect();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Escanear Free_Forest", GUILayout.Height(25)))
            {
                ScanFreeForest();
            }
            if (GUILayout.Button("Escanear Todo", GUILayout.Height(25)))
            {
                ScanAllBuiltInMaterials();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(15);
            
            // Lista de materiales encontrados
            EditorGUILayout.LabelField($"2. Materiales Encontrados: {materialsToConvert.Count}", EditorStyles.boldLabel);
            
            showConverted = EditorGUILayout.Foldout(showConverted, "Mostrar/Ocultar Lista");
            if (showConverted && materialsToConvert.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (var mat in materialsToConvert)
                {
                    if (mat != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(mat, typeof(Material), false);
                        EditorGUILayout.LabelField(mat.shader.name, EditorStyles.miniLabel, GUILayout.Width(200));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.Space(10);
            
            // Botón de conversión
            EditorGUILayout.LabelField("3. Conversión", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CONVERTIR A URP", GUILayout.Height(40)))
            {
                ConvertMaterialsToURP();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
            
            // Status
            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUI.color = statusColor;
                EditorGUILayout.HelpBox(statusMessage, MessageType.None);
                GUI.color = Color.white;
            }
            
            EditorGUILayout.Space(10);
            
            // Info
            EditorGUILayout.LabelField("Información:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Shaders Built-in (Standard) se ven MAGENTA en URP\n" +
                "• Esta herramienta convierte automáticamente a URP/Lit o URP/Particles/Unlit\n" +
                "• KoreanTraditionalPattern_Effect usa 'Particles/Standard Unlit' (Built-in)\n" +
                "• Después de convertir, los fireballs deberían verse correctamente",
                MessageType.Info
            );
        }

        private void ScanKoreanTraditionalPatternEffect()
        {
            materialsToConvert.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/KoreanTraditionalPattern_Effect" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && IsBuiltInShader(mat.shader))
                {
                    materialsToConvert.Add(mat);
                }
            }
            
            UpdateStatus($"Encontrados {materialsToConvert.Count} materiales Built-in en KoreanTraditionalPattern_Effect", Color.yellow);
        }

        private void ScanFreeForest()
        {
            materialsToConvert.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Free_Forest" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && IsBuiltInShader(mat.shader))
                {
                    materialsToConvert.Add(mat);
                }
            }
            
            UpdateStatus($"Encontrados {materialsToConvert.Count} materiales Built-in en Free_Forest", Color.yellow);
        }

        private void ScanAllBuiltInMaterials()
        {
            materialsToConvert.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && IsBuiltInShader(mat.shader))
                {
                    materialsToConvert.Add(mat);
                }
            }
            
            UpdateStatus($"Encontrados {materialsToConvert.Count} materiales Built-in en todo el proyecto", Color.yellow);
        }

        private bool IsBuiltInShader(Shader shader)
        {
            if (shader == null) return false;
            string shaderName = shader.name.ToLower();
            
            // Lista de shaders Built-in que causan problemas en URP
            string[] builtInShaders = new[]
            {
                "standard",
                "particles/",
                "mobile/particles/",
                "unlit/",
                "legacy/"
            };
            
            // Verificar que NO sea ya URP o HDRP
            if (shaderName.Contains("universal") || shaderName.Contains("hdrp"))
                return false;
            
            // Verificar si es Built-in
            foreach (var builtIn in builtInShaders)
            {
                if (shaderName.Contains(builtIn))
                    return true;
            }
            
            return false;
        }

        private void ConvertMaterialsToURP()
        {
            if (materialsToConvert.Count == 0)
            {
                UpdateStatus("No hay materiales para convertir. Primero escanea.", Color.red);
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var mat in materialsToConvert)
            {
                if (mat == null) continue;

                try
                {
                    string originalShader = mat.shader.name.ToLower();
                    Shader newShader = null;

                    // Seleccionar shader URP apropiado
                    if (originalShader.Contains("particle"))
                    {
                        // Para sistemas de partículas
                        if (originalShader.Contains("unlit") || originalShader.Contains("alpha"))
                        {
                            newShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                        }
                        else
                        {
                            newShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
                        }
                    }
                    else if (originalShader.Contains("unlit"))
                    {
                        newShader = Shader.Find("Universal Render Pipeline/Unlit");
                    }
                    else
                    {
                        // Default: URP Lit
                        newShader = Shader.Find("Universal Render Pipeline/Lit");
                    }

                    if (newShader != null)
                    {
                        // Guardar propiedades importantes
                        Color originalColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                        Color originalEmission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
                        Texture originalMainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                        float originalCutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;

                        // Cambiar shader
                        mat.shader = newShader;

                        // Restaurar propiedades si existen en el nuevo shader
                        if (mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", originalColor);
                        if (mat.HasProperty("_BaseMap") && originalMainTex != null)
                            mat.SetTexture("_BaseMap", originalMainTex);
                        if (mat.HasProperty("_Cutoff"))
                            mat.SetFloat("_Cutoff", originalCutoff);
                        if (mat.HasProperty("_EmissionColor"))
                            mat.SetColor("_EmissionColor", originalEmission);

                        successCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[MaterialURPConverter] No se encontró shader URP para: {mat.name}");
                        failCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[MaterialURPConverter] Error convirtiendo {mat.name}: {ex.Message}");
                    failCount++;
                }
            }

            // Guardar cambios
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UpdateStatus(
                $"Conversión completada: {successCount} exitosos, {failCount} fallidos de {materialsToConvert.Count} totales",
                failCount > 0 ? Color.yellow : Color.green
            );

            Debug.Log($"[MaterialURPConverter] Conversión completada. Exitosos: {successCount}, Fallidos: {failCount}");
        }

        private void UpdateStatus(string message, Color color)
        {
            statusMessage = message;
            statusColor = color;
            Repaint();
        }
    }
}
