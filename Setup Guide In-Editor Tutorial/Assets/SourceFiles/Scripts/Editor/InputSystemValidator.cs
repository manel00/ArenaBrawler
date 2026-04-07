#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;
using System.Linq;

namespace ArenaEnhanced.Editor
{
    /// <summary>
    /// Valida y auto-corrije configuración de Input System en el proyecto.
    /// Previene errores de StandaloneInputModule cuando se usa el Nuevo Input System.
    /// </summary>
    public static class InputSystemValidator
    {
        private const string VALIDATED_KEY = "ArenaEnhanced_InputSystem_Validated";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(VALIDATED_KEY, false);
            }
            else if (state == PlayModeStateChange.ExitingEditMode)
            {
                ValidateAndFixInputSystem();
            }
        }

        private static void OnHierarchyChanged()
        {
            // Revalidar después de cambios en la jerarquía
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(VALIDATED_KEY, false))
                {
                    ValidateAndFixInputSystem();
                }
            };
        }

        /// <summary>
        /// Valida y corrige automáticamente la configuración de Input System
        /// </summary>
        [MenuItem("Arena/Validate Input System", priority = 100)]
        public static void ValidateAndFixInputSystem()
        {
            // Verificar que el proyecto usa el Nuevo Input System
            if (!IsNewInputSystemActive())
            {
                SessionState.SetBool(VALIDATED_KEY, true);
                return;
            }

            bool fixedAny = false;
            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);

            foreach (var eventSystem in eventSystems)
            {
                var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                var inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();

                if (standaloneModule != null && inputSystemModule == null)
                {
                    Debug.LogWarning($"[InputSystemValidator] StandaloneInputModule detectado en '{eventSystem.name}' con Nuevo Input System activo. Auto-corrigiendo...", eventSystem);

                    Object.DestroyImmediate(standaloneModule, true);
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

                    EditorUtility.SetDirty(eventSystem.gameObject);
                    fixedAny = true;
                }
                else if (standaloneModule != null && inputSystemModule != null)
                {
                    // Ambos módulos presentes - eliminar el antiguo
                    Debug.LogWarning($"[InputSystemValidator] Ambos módulos detectados en '{eventSystem.name}'. Eliminando StandaloneInputModule...", eventSystem);
                    Object.DestroyImmediate(standaloneModule, true);
                    EditorUtility.SetDirty(eventSystem.gameObject);
                    fixedAny = true;
                }
            }

            if (fixedAny)
            {
                Debug.Log("[InputSystemValidator] Configuración de Input System corregida automáticamente.");
                SessionState.SetBool(VALIDATED_KEY, true);
            }
            else
            {
                SessionState.SetBool(VALIDATED_KEY, true);
            }
        }

        /// <summary>
        /// Verifica si el Nuevo Input System está activo
        /// </summary>
        private static bool IsNewInputSystemActive()
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
                return false;
            #else
                return true;
            #endif
        }

        /// <summary>
        /// Crea un EventSystem configurado correctamente para la escena actual
        /// </summary>
        [MenuItem("Arena/Create EventSystem (Input System)", priority = 101)]
        public static void CreateEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("EventSystem Existente", 
                    $"Ya existe un EventSystem en la escena: '{existing.name}'\n\nUsa 'Validate Input System' para corregirlo si es necesario.", 
                    "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            
            EditorUtility.SetDirty(go);
            Selection.activeGameObject = go;
            
            Debug.Log("[InputSystemValidator] EventSystem creado correctamente con InputSystemUIInputModule.");
        }
    }

    /// <summary>
    /// Validador de compilación para detectar problemas de tipo
    /// </summary>
    public class PreCompileValidator : AssetModificationProcessor
    {
        private static readonly HashSet<string> KNOWN_MISSING_TYPES = new HashSet<string>
        {
            // Add types here that have been renamed/removed to catch remaining references
        };

        private static string[] OnWillSaveAssets(string[] paths)
        {
            ValidateScriptReferences();
            return paths;
        }

        private static void ValidateScriptReferences()
        {
            var scripts = AssetDatabase.FindAssets("t:MonoScript");
            bool foundIssues = false;

            foreach (var guid in scripts)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs")) continue;

                var text = System.IO.File.ReadAllText(path);
                
                foreach (var missingType in KNOWN_MISSING_TYPES)
                {
                    if (text.Contains(missingType))
                    {
                        Debug.LogError($"[PreCompileValidator] Referencia a tipo inexistente '{missingType}' encontrada en: {path}", 
                            AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                        foundIssues = true;
                    }
                }
            }

            if (foundIssues)
            {
                Debug.LogWarning("[PreCompileValidator] Se encontraron referencias a tipos inexistentes. Revisa los errores arriba.");
            }
        }
    }
}
#endif
