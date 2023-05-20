using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramesSpawnerController : MonoBehaviour
{
    [SerializeField] private int index = 8;
    private List<GameObject> spawnedFrames = new List<GameObject>();

    [field: Header("Voxel Generation Settings")]
    [RangeExtension(0.5f, 5f, 0.5f)]
    [SerializeField] float size = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] float threshold = 0.4f;
    [SerializeField] List<Texture3D> textures;
    [SerializeField] GameObject framePrefab;
    

    // Start is called before the first frame update
    void Start()
    {
        SpawnFrames();
        index = Mathf.Clamp(index, 0, spawnedFrames.Count - 1);
        ActivateSelectedFrame(index);

    }

    // Update is called once per frame
    void Update()
    {
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

    void SpawnFrames()
    {
        foreach(Texture3D texture in textures)
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
