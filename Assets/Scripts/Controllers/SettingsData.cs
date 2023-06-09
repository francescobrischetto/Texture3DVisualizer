using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// This class is responsible of mantaining all the settings data created in the menu scene, passing them to the application scene
/// </summary>
public class SettingsData : MonoBehaviour
{
    private string absolutePath = "";
    private string rawFolder = "/SampleRawFiles/";
    public float Threshold { get; private set; } = 0.4f;
    public int SelectedMethod { get; private set; } = 0;
    public List<Texture3D> LoadedTextures { get; private set; } = new List<Texture3D>();
     
    private void Awake()
    {
        //The path from which the raw files are imported varies if we are in unity_editor or in build
        #if UNITY_EDITOR
            absolutePath = Application.dataPath;
        #else
            absolutePath = System.IO.Directory.GetCurrentDirectory();
        #endif
    }

    public void thresholdUpdated(float num)
    {
        Threshold = num;
    }

    public void methodSelectedChanged(int num)
    {
        SelectedMethod = num;
    }

    //This is the same function as the custom importer, but reads alla raw files from a folder and populates a List of Texture3D
    public void LoadTextures()
    {
        string rawFilesFullPath = absolutePath + rawFolder;
        DirectoryInfo dir = new DirectoryInfo(rawFilesFullPath);
        //Reading raw files from directory and sorting the names alphanumerically to solve an issue in ordering the names
        FileInfo[] info = dir.GetFiles("*.raw").OrderBy(f => Regex.Replace(f.Name, @"\d+", m => m.Value.PadLeft(50, '0'))).ToArray();
        foreach (FileInfo f in info)
        {
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
                            colors[x + yOffset + zOffset] = new Color(v, v, v, 1.0f);
                            count++;
                        }
                    }
                }

            // Copy the color values to the texture
            texture.SetPixels(colors);

            // Apply the changes to the texture and upload the updated texture to the GPU
            texture.Apply();

            //Adding the created texture to the array
            LoadedTextures.Add(texture);
        }
    }
}
