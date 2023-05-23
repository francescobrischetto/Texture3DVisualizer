using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] GameObject MainPanel;
    [SerializeField] GameObject SettingsPanel;
    [SerializeField] GameObject CreditsPanel;
    [SerializeField] GameObject LoadingPanel;
    [SerializeField] GameObject SettingsDataObj;

    private SettingsData currentSettings;

    private void Awake()
    {
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        LoadingPanel.SetActive(false);
        SettingsData previousInstance = FindObjectOfType<SettingsData>();
        if (previousInstance != null)
        {
            Destroy(previousInstance.transform.gameObject);
        }
        GameObject instantiatedSettingsObject = Instantiate(SettingsDataObj, Vector3.zero, Quaternion.identity);
        currentSettings = instantiatedSettingsObject.GetComponent<SettingsData>();
        DontDestroyOnLoad(instantiatedSettingsObject);
    }

    public void ToggleSettings()
    {
        MainPanel.SetActive(SettingsPanel.activeSelf);
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }

    public void ToggleCredits()
    {
        MainPanel.SetActive(CreditsPanel.activeSelf);
        CreditsPanel.SetActive(!CreditsPanel.activeSelf);
    }

    public void ToggleLoading()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
        LoadingPanel.SetActive(!LoadingPanel.activeSelf);
    }

    public void QuitGame()
    {
        #if UNITY_STANDALONE
                Application.Quit();
        #endif
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void StartGame()
    {
        currentSettings.LoadTextures();
        ToggleLoading();
        SceneManager.LoadScene("MainScene");

    }

    public void thresholdUpdated(float num)
    {
        currentSettings.thresholdUpdated(num);
    }

    public void methodSelectedChanged(int num)
    {
        currentSettings.methodSelectedChanged(num);
    }
}
