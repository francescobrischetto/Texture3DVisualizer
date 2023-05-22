using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// This class is responsible of providing editors buttons with the common tasks.
/// </summary>
public class RunGame : Editor
{
    /// <summary>
    /// This function starts the game from the starting scene (saving the scene if necessary)
    /// </summary>
    [MenuItem("Game/Run Game")]
    static void StartGame()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity");
            EditorApplication.isPlaying = true;
        }
    }

}