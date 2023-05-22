using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class FramesSpawnerController : MonoBehaviour
{
    [SerializeField] private int index = 8;
    [RangeExtension(0.25f, 2f, 0.25f)]
    [SerializeField] private float animationSpeed = 0.25f;
    private List<GameObject> spawnedFrames = new List<GameObject>();
    private bool enableCommands = true;

    [field: Header("Voxel Generation Settings")]
    [RangeExtension(0.5f, 5f, 0.5f)]
    [SerializeField] float size = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] float threshold = 0.4f;
    [SerializeField] List<Texture3D> textures;
    [SerializeField] GameObject framePrefab;
    

    // Start is called before the first frame update
    void Awake()
    {
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
                spawnedFrameController.GeneratedVoxels(texture, size, threshold);
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
