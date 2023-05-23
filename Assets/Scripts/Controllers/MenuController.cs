using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is responsible of controlling the UI of the Main Menu and his workflow
/// </summary>
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
        //We need to check if we are going back to the menu -> In this case we destroy the previous settings
        SettingsData previousInstance = FindObjectOfType<SettingsData>();
        if (previousInstance != null)
        {
            Destroy(previousInstance.transform.gameObject);
        }
        //We create the settings and flag them as "DontDestroyOnLoad" to pass through the MainScene
        GameObject instantiatedSettingsObject = Instantiate(SettingsDataObj, Vector3.zero, Quaternion.identity);
        currentSettings = instantiatedSettingsObject.GetComponent<SettingsData>();
        DontDestroyOnLoad(instantiatedSettingsObject);
    }

    //Functions that toggles the various panels
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
    
    //The settings updated in the UI must be reflected to the settings object
    public void thresholdUpdated(float num)
    {
        currentSettings.thresholdUpdated(num);
    }

    public void methodSelectedChanged(int num)
    {
        currentSettings.methodSelectedChanged(num);
    }
}
