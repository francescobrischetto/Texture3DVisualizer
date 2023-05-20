using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class FrameController: MonoBehaviour
{
    [SerializeField] GameObject voxelPrefab;

    public void GeneratedVoxels(Texture3D texture, float size, float threshold)
    {
        SpawnVoxelsFromTex3D(texture, size, threshold);
        MergeChildsIntoSingleObject();
    }


    private void CreateMaterialsInParent(List<Material> materials)
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Material[] materialsArray = new Material[materials.Count];
        for (int i = 0; i < materialsArray.Length; i++)
        {

            materialsArray[i] = Instantiate(materials[i]);
        }
        mr.materials = materialsArray;
    }

    private void SpawnVoxelsFromTex3D(Texture3D texture, float size, float threshold)
    {
        Color[] colors = texture.GetPixels();
        for (int z = 0; z < texture.width; z++)
        {
            int zOffset = z * texture.width * texture.width;
            for (int y = 0; y < texture.width; y++)
            {
                int yOffset = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    if (colors[x + yOffset + zOffset].r >= threshold)
                    {
                        GameObject spawnedObj = GameObject.Instantiate(voxelPrefab, new Vector3(x*size, y * size, z * size), Quaternion.identity, transform);
                        //localsize of the voxel should be adjusted to the final desidered size
                        spawnedObj.transform.localScale = new Vector3(size, size, size);
                        //trasparency is also adjusted
                        spawnedObj.GetComponent<Renderer>().material.color = new Color(colors[x + yOffset + zOffset].r, 
                            colors[x + yOffset + zOffset].g, colors[x + yOffset + zOffset].b, colors[x + yOffset + zOffset].r);                    
                    }
                    
                }
            }
        }
    }

    public void MergeChildsIntoSingleObject()
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

        CreateMaterialsInParent(materials);

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
        //Necessary to merge 32x32x32 voxels
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        finalMesh.CombineMeshes(finalCombiners.ToArray(), false);
        GetComponent<MeshFilter>().sharedMesh = finalMesh;

        transform.Clear();
    }
}
