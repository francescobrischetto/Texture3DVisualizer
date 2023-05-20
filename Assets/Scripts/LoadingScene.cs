using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScene : MonoBehaviour
{
    [SerializeField] GameObject loadingPanel;
    [SerializeField] Image loadingBarFill;

    private IEnumerator LoadSceneASync(int sceneId)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneId);
        loadingPanel.SetActive(true);
        while (!operation.isDone)
        {
            Debug.Log($"Operation progress: {operation.progress}");
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBarFill.fillAmount = progressValue;
            yield return null;
        }
    }

    public void LoadScene(int sceneId)
    {
        StartCoroutine(LoadSceneASync(sceneId));    
    }
}
