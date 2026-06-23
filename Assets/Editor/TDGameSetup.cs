using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NightWatch.Editor
{
    public static class TDGameSetup
    {
        [MenuItem("Night Watch/Setup Game Scene")]
        public static void SetupScene()
        {
            TDGameBootstrap.CreateGameRoot();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Night Watch] Сцена налаштована! Натисніть Play.");
        }

        [MenuItem("Night Watch/Add Game To New Scene")]
        public static void NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            TDGameBootstrap.CreateGameRoot();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/GameScene.unity");
            Debug.Log("[Night Watch] Створено Assets/Scenes/GameScene.unity");
        }
    }
}
