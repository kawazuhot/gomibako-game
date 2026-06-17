using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MvpSceneSetup
{
    private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";

    [MenuItem("Gomibako/Rebuild MVP Scene")]
    public static void RebuildMvpScene()
    {
        OpenOrCreateMainScene();
        TrashBinPrefabBuilder.EnsureTrashBinPrefab();
        MVPTrashGameManager.RebuildPlayableScene();

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
    }

    private static void OpenOrCreateMainScene()
    {
        if (File.Exists(MainScenePath))
        {
            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            return;
        }

        var directory = Path.GetDirectoryName(MainScenePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, MainScenePath);
    }
}
