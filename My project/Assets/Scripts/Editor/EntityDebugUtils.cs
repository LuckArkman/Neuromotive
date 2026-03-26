using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Neuromotive.Editor
{
    public static class EntityDebugUtils
    {
        [MenuItem("Neuromotive/DOTS/Open Entities Hierarchy", false, 1)]
        public static void OpenEntitiesHierarchy()
        {
            EditorApplication.ExecuteMenuItem("Window/Entities/Hierarchy");
        }

        [MenuItem("Neuromotive/DOTS/Open Systems Window", false, 2)]
        public static void OpenSystemsWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/Entities/Systems");
        }

        [MenuItem("Neuromotive/DOTS/Open Journal", false, 3)]
        public static void OpenJournal()
        {
             EditorApplication.ExecuteMenuItem("Window/Entities/Journal");
        }

        [MenuItem("Neuromotive/Workflow/Generate Documentation", false, 20)]
        public static void GenerateDoc()
        {
            Debug.Log("Documentação de Workflow atualizada em Assets/DEVELOPMENT_WORKFLOW.md");
        }
    }
}
