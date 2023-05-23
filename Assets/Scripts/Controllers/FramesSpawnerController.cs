using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class FramesSpawnerController : MonoBehaviour
{
    private int index = 0;
    private float animationSpeed = 1f;
    private List<GameObject> spawnedFrames = new List<GameObject>();
    private bool enableCommands = true;
    private int selectedMethod = 0;
    private List<Texture3D> textures = new List<Texture3D>();
    private float threshold = 0f;
    private float size = 1.0f;

    [field: Header("Voxel Generation Settings")]    
    [SerializeField] GameObject framePrefab;
    [SerializeField] UnityEvent<int> onIndexChanged;

    public void animationSpeedChanged(int dropdownChoice)
    {
        switch (dropdownChoice)
        {
            case 0: animationSpeed = 0.25f; break;
            case 1: animationSpeed = 0.5f;  break;
            case 2: animationSpeed = 1f;    break;
            case 3: animationSpeed = 2f;    break;
        }
    }

    public void ChangeFrame(bool next)
    {
        if (next)
        {
            DeActiveFrame(index);
            index++;
            index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
            onIndexChanged.Invoke(index);
            ActivateSelectedFrame(index);
        }
        else
        {
            DeActiveFrame(index);
            index--;
            index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
            onIndexChanged.Invoke(index);
            ActivateSelectedFrame(index);
        }
    }
    
    public void PlayAnimation()
    {
        StartCoroutine(ExecuteAnimation(index, animationSpeed));
        enableCommands = false;
    }

    // Start is called before the first frame update
    void Awake()
    {
        SettingsData menuSettings = FindObjectOfType<SettingsData>();
        selectedMethod = menuSettings.SelectedMethod;
        textures.AddRange(menuSettings.LoadedTextures);
        threshold = menuSettings.Threshold;
        float startTime = Time.time;
        SpawnFrames();
        index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
        ActivateSelectedFrame(index);
        Debug.Log($"Elapsed time: {Time.time - startTime}");

    }

    private IEnumerator ExecuteAnimation(int initialIndex, float animationSpeed)
    {
        DeActiveFrame(initialIndex);
        float startTime = Time.time;
        float initialFrameTime = Time.time;
        for (int localIndex = 0; localIndex < spawnedFrames.Count; localIndex++)
        {  
            ActivateSelectedFrame(localIndex);
            onIndexChanged.Invoke(localIndex);
            //20 FPS
            yield return new WaitForSeconds(1f /animationSpeed / 20 - (Time.time - initialFrameTime));
            initialFrameTime = Time.time;
            DeActiveFrame(localIndex);
        }
        Debug.Log($"Elapsed time for the animation: {Time.time - startTime}");
        ActivateSelectedFrame(spawnedFrames.Count - 1);
        yield return new WaitForSeconds(0.25f);
        DeActiveFrame(spawnedFrames.Count - 1);
        enableCommands = true;
        ActivateSelectedFrame(initialIndex);
        onIndexChanged.Invoke(initialIndex);

    }

    void SpawnFrames()
    {
        var sw = Stopwatch.StartNew();
        foreach (Texture3D texture in textures)
        {
            GameObject spawnedFrame = GameObject.Instantiate(framePrefab, transform.position, Quaternion.identity, transform);
            FrameController spawnedFrameController = spawnedFrame.GetComponent<FrameController>();
            if (spawnedFrameController != null)
            {
                spawnedFrameController.GeneratedVoxels(texture, size, threshold, selectedMethod);
            }
            spawnedFrames.Add(spawnedFrame);
            spawnedFrame.SetActive(false);
        }
        var dur = sw.ElapsedMilliseconds;
        UIController.Instance.InitializeUI(index, spawnedFrames.Count, dur / 1000.0f);
    }

    

    void ActivateSelectedFrame(int indexToActive)
    {
        spawnedFrames[indexToActive].SetActive(true);
    }

    void DeActiveFrame(int indexToDeactive)
    {
        spawnedFrames[indexToDeactive].SetActive(false);
    }

}
