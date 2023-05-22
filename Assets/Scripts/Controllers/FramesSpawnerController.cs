using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class FramesSpawnerController : MonoBehaviour
{
    [SerializeField] private int index = 8;
    [RangeExtension(0.25f, 2f, 0.25f)]
    [SerializeField] private float animationSpeed = 0.25f;
    private List<GameObject> spawnedFrames = new List<GameObject>();
    private bool enableCommands = true;
    private List<Texture3D> textures = new List<Texture3D>();

    [field: Header("Voxel Generation Settings")]
    [RangeExtension(0.5f, 5f, 0.5f)]
    [SerializeField] float size = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] float threshold = 0.4f;
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
        LoadTextures();
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

    void LoadTextures()
    {
        string rawFilesPath = "/SampleRawFiles/";
        #if UNITY_EDITOR
        string localRawFilsFullPath = Application.dataPath + rawFilesPath;
        #else
        string localRawFilsFullPath = System.IO.Directory.GetCurrentDirectory() + rawFilesPath;
        #endif
        DirectoryInfo dir = new DirectoryInfo(localRawFilsFullPath);
        //Reading raw files from directory and sorting the names alphanumerically to solve an issue in ordering the names
        FileInfo[] info = dir.GetFiles("*.raw").OrderBy(f => Regex.Replace(f.Name, @"\d+", m => m.Value.PadLeft(50, '0'))).ToArray();
        foreach (FileInfo f in info)
        {
            Debug.Log($"Processing {f.Name}");
               
            // Configure the texture
            int size = 32;
            int count = 0;
            TextureFormat format = TextureFormat.RGBA32;
            TextureWrapMode wrapMode = TextureWrapMode.Clamp;

            // Create the texture and apply the configuration
            Texture3D texture = new Texture3D(size, size, size, format, false);
            texture.wrapMode = wrapMode;

            // Create a 3-dimensional array to store color data
            Color[] colors = new Color[size * size * size];
            using (var file = File.OpenRead(f.FullName))
            using (var reader = new BinaryReader(file))
                for (int z = 0; z < size; z++)
                {
                    int zOffset = z * size * size;
                    for (int y = 0; y < size; y++)
                    {
                        int yOffset = y * size;
                        for (int x = 0; x < size; x++)
                        {
                            float v = (float)reader.ReadByte() / 0xFF;
                            //v = (float) Math.Round(v, 3);
                            colors[x + yOffset + zOffset] = new Color(v, v, v, 1.0f);
                            count++;
                        }
                    }
                }

            // Copy the color values to the texture
            texture.SetPixels(colors);

            // Apply the changes to the texture and upload the updated texture to the GPU
            texture.Apply();
            textures.Add(texture);
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
