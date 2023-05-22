using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class FramesSpawnerController : MonoBehaviour
{
    private int index = 0;
    private float animationSpeed = 0.25f;
    private List<GameObject> spawnedFrames = new List<GameObject>();
    private bool enableCommands = true;
    private int selectedMethod = 0;
    private List<Texture3D> textures = new List<Texture3D>();
    private float threshold = 0f;
    private float size = 1.0f;

    [field: Header("Voxel Generation Settings")]    
    [SerializeField] GameObject framePrefab;
    

    // Start is called before the first frame update
    void Awake()
    {
        selectedMethod = SettingsData.Instance.SelectedMethod;
        textures.AddRange(SettingsData.Instance.LoadedTextures);
        threshold = SettingsData.Instance.Threshold;
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
            yield return new WaitForSeconds(1f /animationSpeed / spawnedFrames.Count - (Time.time - initialFrameTime)); 
            initialFrameTime = Time.time;
            DeActiveFrame(localIndex);
        }
        Debug.Log($"Elapsed time for the animation: {Time.time - startTime}");
        ActivateSelectedFrame(spawnedFrames.Count - 1);
        yield return new WaitForSeconds(0.25f);
        DeActiveFrame(spawnedFrames.Count - 1);
        enableCommands = true;
        ActivateSelectedFrame(initialIndex);

    }

    // Update is called once per frame
    void Update()
    {
        if (enableCommands)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                StartCoroutine(ExecuteAnimation(index, animationSpeed));
                enableCommands = false;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                DeActiveFrame(index);
                index++;
                index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
                Debug.Log($"Frame: {index + 1}");
                ActivateSelectedFrame(index);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                DeActiveFrame(index);
                index--;
                index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
                Debug.Log($"Frame: {index + 1}");
                ActivateSelectedFrame(index);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene("MenuScene");
            }
        }

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
        Debug.Log($"TOTAL SPAWNING Took {dur / 1000.0:F2}sec");
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
