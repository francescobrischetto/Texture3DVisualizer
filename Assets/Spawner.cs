using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Spawner : MonoBehaviour
{
    [SerializeField] float size = 1.0f;
    [SerializeField] List<string> rawImagesPath;
    [SerializeField] Material mat;
    [SerializeField] GameObject cubeObj;
    private int index = 8;

    private void Start()
    {
        //CreateMaterials();
        SpawnCubesFromTex3D(rawImagesPath[index]);
        AdvancedMerge();
        Debug.Log($"Frame: {index+1}");
    }
    public void CreateMaterials()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Debug.Log($"mr.materials.Length = {mr.materials.Length}");
        Material[] materialsArray = new Material[mr.materials.Length];
        for (int i = 0; i < materialsArray.Length; i++)
        {
            
            materialsArray[i] = Instantiate(mat);
            materialsArray[i].color = new Color((float)i/255, (float)i / 255, (float)i / 255, (float)i / 255);
        }
        mr.materials = materialsArray;
    }

    public void CreateMaterials2(List<Material> mats)
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Material[] materialsArray = new Material[mats.Count];
        for (int i = 0; i < materialsArray.Length; i++)
        {

            materialsArray[i] = Instantiate(mats[i]);
        }
        mr.materials = materialsArray;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if(index < rawImagesPath.Count - 1)
            {
                GetComponent<MeshFilter>().mesh.Clear();
                index++;
                Debug.Log($"Frame: {index + 1}");
                SpawnCubesFromTex3D(rawImagesPath[index]);
                AdvancedMerge();
            }
            else
            {
                Debug.Log("--Reached the end!");
            }
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (index >= 1)
            {
                GetComponent<MeshFilter>().mesh.Clear();
                index--;
                Debug.Log($"Frame: {index + 1}");
                SpawnCubesFromTex3D(rawImagesPath[index]);
                AdvancedMerge();
            }
            else
            {
                Debug.Log("--Reached the start!");
            }
        }
    }

    Texture3D CreateTextureFromRaw(string aFileName)
    {
        //string aFileName = "Assets/Textures/Beating_Heart_frame2_texture3d_32.raw";
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
        using (var file = System.IO.File.OpenRead(aFileName))
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

        return texture;
    }
    void SpawnCubesFromTex3D(string aFileName)
    {
        Texture3D texture3D = CreateTextureFromRaw(aFileName);
        Color[] colors = texture3D.GetPixels();
        for (int z = 0; z < texture3D.width; z++)
        {
            int zOffset = z * texture3D.width * texture3D.width;
            for (int y = 0; y < texture3D.width; y++)
            {
                int yOffset = y * texture3D.width;
                for (int x = 0; x < texture3D.width; x++)
                {
                    if (colors[x + yOffset + zOffset].r >= 0.4f)
                    {
                        GameObject spawnedObj = GameObject.Instantiate(cubeObj, new Vector3(x*size, y * size, z * size), Quaternion.identity, transform);
                        spawnedObj.transform.localScale = new Vector3(size, size, size);
                        spawnedObj.GetComponent<Renderer>().material.color = new Color(colors[x + yOffset + zOffset].r, 
                            colors[x + yOffset + zOffset].g, colors[x + yOffset + zOffset].b, colors[x + yOffset + zOffset].r);                    }
                    
                }
            }
        }
    }

    public void AdvancedMerge()
    {
        // All our children (and us)
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);

        // All the meshes in our children (just a big list)
        List<Material> materials = new List<Material>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false); // <-- you can optimize this
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.transform == transform)
                continue;
            Material[] localMats = renderer.sharedMaterials;
            foreach (Material localMat in localMats)
            {
                bool foundMat = false;
                foreach(Material mat in materials)
                {
                    if(localMat.color == mat.color)
                    {
                        foundMat = true;
                        break;
                    }
                }
                if (!foundMat)
                {
                    materials.Add(localMat);
                }                    
            }
                
        }

        CreateMaterials2(materials);

        // Each material will have a mesh for it.
        List<Mesh> submeshes = new List<Mesh>();
        foreach (Material material in materials)
        {
            // Make a combiner for each (sub)mesh that is mapped to the right material.
            List<CombineInstance> combiners = new List<CombineInstance>();
            foreach (MeshFilter filter in filters)
            {
                if (filter.transform == transform) continue;
                // The filter doesn't know what materials are involved, get the renderer.
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();  // <-- (Easy optimization is possible here, give it a try!)
                if (renderer == null)
                {
                    Debug.LogError(filter.name + " has no MeshRenderer");
                    continue;
                }

                // Let's see if their materials are the one we want right now.
                Material[] localMaterials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < localMaterials.Length; materialIndex++)
                {
                    if (localMaterials[materialIndex].color != material.color)
                        continue;
                    // This submesh is the material we're looking for right now.
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = filter.sharedMesh;
                    ci.subMeshIndex = materialIndex;
                    //ci.transform = Matrix4x4.identity;
                    ci.transform = filter.transform.localToWorldMatrix;
                    combiners.Add(ci);
                }
            }
            // Flatten into a single mesh.
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combiners.ToArray(), true);
            submeshes.Add(mesh);
        }

        // The final mesh: combine all the material-specific meshes as independent submeshes.
        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        for (int i=0; i<submeshes.Count; i++)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = submeshes[i];
            ci.subMeshIndex = 0;
            //ci.transform = Matrix4x4.identity;
            ci.transform = transform.localToWorldMatrix;
            finalCombiners.Add(ci);
        }
        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        Debug.Log("Final mesh has " + submeshes.Count + " materials.");
        finalMesh.CombineMeshes(finalCombiners.ToArray(), false);
        GetComponent<MeshFilter>().sharedMesh = finalMesh;
        Debug.Log("Final mesh has " + submeshes.Count + " materials.");
        transform.Clear();
    }
}
