using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Collections.Specialized;

[ScriptedImporter(1, "raw")]
public class RawImporter : ScriptedImporter
{
    public int size = 32;

    public override void OnImportAsset(AssetImportContext ctx)
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
        using (var file = System.IO.File.OpenRead(ctx.assetPath))
        using (var reader = new System.IO.BinaryReader(file))
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

        // Assets must be assigned a unique identifier string consistent across imports
        ctx.AddObjectToAsset("my 3D Texture", texture);
    }
}