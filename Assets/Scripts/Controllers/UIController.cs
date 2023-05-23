using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This class is responsible of updating the application UI properly to react to player input
/// </summary>
public class UIController : MonoBehaviour
{
    [SerializeField] Button PlayButton;
    [SerializeField] Slider PlaySlider;
    [SerializeField] TMP_Dropdown SpeedDropdown;
    [SerializeField] Button BackToMenuButton;
    [SerializeField] Button PrevFrameButton;
    [SerializeField] Button NextFrameButton;
    [SerializeField] TMP_Text TotalFramesText;
    [SerializeField] TMP_Text CurrentFramesText;
    [SerializeField] TMP_Text LoadedTimeText;

    //Those unityEvents allows to react to player buttons press or dropdown changes
    public UnityEvent onPlayButton;
    public UnityEvent<bool> onFrameManagementButtons;
    public UnityEvent<int> onSpeedDropdownChanged;
    public static UIController Instance { get; private set; }

    private void Awake()
    {
        //Singleton Setup
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        //Observer Setup
        PlayButton.onClick.AddListener(ReactToPlayButton);
        PrevFrameButton.onClick.AddListener(ReactPrevFrameButton);
        NextFrameButton.onClick.AddListener(ReactNextFrameButton);
        BackToMenuButton.onClick.AddListener(ToMainMenuScene);
        SpeedDropdown.onValueChanged.AddListener(ReactToSpeedDropdown);
    }

    private void ReactToSpeedDropdown(int value)
    {
        onSpeedDropdownChanged.Invoke(value);
    }

    private void ReactToPlayButton()
    {
        onPlayButton.Invoke();
    }

    private void ReactPrevFrameButton()
    {
        onFrameManagementButtons.Invoke(false);
    }

    private void ReactNextFrameButton()
    {
        onFrameManagementButtons.Invoke(true);
    }

    public void PlayIndexChanged(int newIndex)
    {
        PlaySlider.value = newIndex + 1;
        CurrentFramesText.text = (newIndex + 1).ToString();
    }

    public void ToMainMenuScene()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void InitializeUI(int currentFrame, int totalFrames, float spawningDuration)
    {
        CurrentFramesText.text = (currentFrame + 1).ToString();
        TotalFramesText.text = totalFrames.ToString();
        PlaySlider.maxValue = totalFrames;
        LoadedTimeText.text = $"Frames loaded in {spawningDuration:F2} seconds";
    }

}
