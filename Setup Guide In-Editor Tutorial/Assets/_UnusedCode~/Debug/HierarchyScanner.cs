using UnityEngine;
using UnityEditor;

public class HierarchyScanner : EditorWindow
{
    [MenuItem("Tools/Debug/Scan Hierarchy High Pos")]
    public static void Scan()
    {
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go.transform.position.y > 10f)
            {
                Debug.LogWarning($"[SKY_OBJECT] {go.name} (EntityID: {go.GetEntityId()}) | pos: {go.transform.position} | active: {go.activeInHierarchy}");
            }
        }
    }
}
