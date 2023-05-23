using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class FramesSpawnerController : MonoBehaviour
{
    //current displayed frame
    private int index = 0;
    //speed of the animation
    private float animationSpeed = 1f;
    //list of frames created
    private List<GameObject> spawnedFrames = new List<GameObject>();
    //guard bool that controls if animation is playing before allowing to navigate thorugh frames
    private bool doingAnimation = false;
    //this integer let the user choose between multiple generation methods of the voxel meshes
    private int selectedMethod = 0;
    private List<Texture3D> textures = new List<Texture3D>();
    private float threshold = 0f;
    private float size = 1.0f;

    [field: Header("Voxel Generation Settings")]    
    [SerializeField] GameObject framePrefab;
    [SerializeField] UnityEvent<int> onIndexChanged;

    // Start is called before the first frame update
    private void Awake()
    {
        //Searching settings and updating internal variables
        SettingsData menuSettings = FindObjectOfType<SettingsData>();
        if(menuSettings != null)
        {
            selectedMethod = menuSettings.SelectedMethod;
            textures.AddRange(menuSettings.LoadedTextures);
            threshold = menuSettings.Threshold;
        }
        //Creating all voxel Meshes
        SpawnFrames();
        index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
        //Activate current frame object
        ActivateSelectedFrame(index);
    }

    /// <summary>
    /// This coroutine allows to play the animation by activating/deactivating the gameobjects waiting in-between
    /// </summary>
    /// <param name="initialIndex"></param>
    /// <param name="animationSpeed"></param>
    /// <returns></returns>
    private IEnumerator ExecuteAnimation(int initialIndex, float animationSpeed)
    {
        //Saving the initial index to restore it later
        DeActiveFrame(initialIndex);
        float initialFrameTime = Time.time;
        for (int localIndex = 0; localIndex < spawnedFrames.Count; localIndex++)
        {
            ActivateSelectedFrame(localIndex);
            onIndexChanged.Invoke(localIndex);
            int FramePerSeconds = 20;
            //Waiting a time that depends on the FPS, the animation speed and the time elapsed from the start of the loop
            yield return new WaitForSeconds(1f / animationSpeed / FramePerSeconds - (Time.time - initialFrameTime));
            initialFrameTime = Time.time;
            DeActiveFrame(localIndex);
        }
        ActivateSelectedFrame(spawnedFrames.Count - 1);
        //Last frame of the animation stays for a bit before being deactivated
        yield return new WaitForSeconds(0.25f);
        DeActiveFrame(spawnedFrames.Count - 1);
        //Restore the initial index before ending the function
        doingAnimation = false;
        ActivateSelectedFrame(initialIndex);
        onIndexChanged.Invoke(initialIndex);

    }

    /// <summary>
    /// This method spawn all the frames based on the textures loaded from disk. It also measure how much time passed for the operation to be completed.
    /// </summary>
    private void SpawnFrames()
    {
        var sw = Stopwatch.StartNew();
        foreach (Texture3D texture in textures)
        {
            GameObject spawnedFrame = GameObject.Instantiate(framePrefab, transform.position, Quaternion.identity, transform);
            FrameController spawnedFrameController = spawnedFrame.GetComponent<FrameController>();
            if (spawnedFrameController != null)
            {
                //Generating the voxel mesh with the given texture, threshold and method
                spawnedFrameController.GeneratedVoxels(texture, size, threshold, selectedMethod);
            }
            //Adding to the list and deactivating it
            spawnedFrames.Add(spawnedFrame);
            spawnedFrame.SetActive(false);
        }
        var dur = sw.ElapsedMilliseconds;
        UIController.Instance.InitializeUI(index, spawnedFrames.Count, dur / 1000.0f);
    }

    private void ActivateSelectedFrame(int indexToActive)
    {
        spawnedFrames[indexToActive].SetActive(true);
    }

    private void DeActiveFrame(int indexToDeactive)
    {
        spawnedFrames[indexToDeactive].SetActive(false);
    }

    //Functions that reacts to player input based on the UI
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
        if (!doingAnimation)
        {
            //The user wants to go to the next index (or the previous index) in the list of frames
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
    }
    
    public void PlayAnimation()
    {
        StartCoroutine(ExecuteAnimation(index, animationSpeed));
        doingAnimation = true;
    }
}
