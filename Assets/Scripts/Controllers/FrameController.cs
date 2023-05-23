using Unity.Burst;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// This class generates the Voxel Mesh from the Texture3D. It also has different merging methods for the cubes composing the mesh
/// </summary>
public class FrameController: MonoBehaviour
{
    [SerializeField] GameObject voxelPrefab;

    public void GeneratedVoxels(Texture3D texture, float size, float threshold, int selectedMethod)
    {
        //Spawns all the voxels
        SpawnVoxelsFromTex3D(texture, size, threshold);
        //Merges all the voxels into one single mesh to be optimized (the heavy process)
        switch (selectedMethod)
        {
            case 0: MergeChildsIntoSingleObjectMixedApproach(); break;
            case 1: MergeChildsIntoSingleObjectUnityJobs(); break;
        }
    }

    //This function instantiate all the materials in input in the parent gameObject
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

    //This function spawns voxels prefab reading the 3D texture
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

    //This was the very first "naive" approach to merge all the children considering also their material using CombineMeshes -> Really slow approach
    private void MergeChildsIntoSingleObjectNaive()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);
        List<Material> materials = new List<Material>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false);
        //Search for all the materials in our children
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

        //Create the materials in parent
        CreateMaterialsInParent(materials);
        
        // Each material will have a SubMesh for it.
        List<Mesh> submeshes = new List<Mesh>();
        foreach (Material material in materials)
        {
            // Make a combiner for each (sub)mesh that is mapped to the right material.
            List<CombineInstance> combiners = new List<CombineInstance>();
            foreach (MeshFilter filter in filters)
            {
                if (filter.transform == transform) continue;
                // The filter doesn't know what materials are involved, get the renderer.
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    UnityEngine.Debug.LogError(filter.name + " has no MeshRenderer");
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
                    //assigning the right material Index to the submesh
                    ci.subMeshIndex = materialIndex;
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
        List <CombineInstance> finalCombiners = new List<CombineInstance>();
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

        //Clearing all the children
        transform.Clear();
    }

    //This approach merge all the children not considering their Material using Unity Job system -> the fastest
    //Example from Unity Technology: https://github.com/Unity-Technologies/MeshApiExamples/blob/master/Assets/CreateMeshFromAllSceneMeshes/CreateMeshFromWholeScene.cs
    private void MergeChildsIntoSingleObjectUnityJobs()
    {
        // Find all MeshFilter objects in the scene
        var meshFilters = GetComponentsInChildren<MeshFilter>();

        // Need to figure out how large the output mesh needs to be (in terms of vertex/index count),
        // as well as get transforms and vertex/index location offsets for each mesh.
        var jobs = new ProcessMeshDataJob();
        jobs.CreateInputArrays(meshFilters.Length);
        var inputMeshes = new List<Mesh>(meshFilters.Length);

        var vertexStart = 0;
        var indexStart = 0;
        var meshCount = 0;
        for (var i = 0; i < meshFilters.Length; ++i)
        {
            var mf = meshFilters[i];
            var go = mf.gameObject;
            if (mf.sharedMesh == null)
            {
                continue;
            }

            var mesh = mf.sharedMesh;
            inputMeshes.Add(mesh);
            jobs.vertexStart[meshCount] = vertexStart;
            jobs.indexStart[meshCount] = indexStart;
            jobs.xform[meshCount] = go.transform.localToWorldMatrix;
            vertexStart += mesh.vertexCount;
            indexStart += (int)mesh.GetIndexCount(0);
            jobs.bounds[meshCount] = new float3x2(new float3(Mathf.Infinity), new float3(Mathf.NegativeInfinity));
            ++meshCount;
        }

        // Acquire read-only data for input meshes
        jobs.meshData = Mesh.AcquireReadOnlyMeshData(inputMeshes);

        // Create and initialize writable data for the output mesh
        var outputMeshData = Mesh.AllocateWritableMeshData(1);
        jobs.outputMesh = outputMeshData[0];
        jobs.outputMesh.SetIndexBufferParams(indexStart, IndexFormat.UInt32);
        jobs.outputMesh.SetVertexBufferParams(vertexStart,
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1));

        // Launch mesh processing jobs
        var handle = jobs.Schedule(meshCount, 4);

        // Create destination Mesh object
        var newMesh = new Mesh();
        newMesh.name = "CombinedMesh";
        var sm = new SubMeshDescriptor(0, indexStart, MeshTopology.Triangles);
        sm.firstVertex = 0;
        sm.vertexCount = vertexStart;

        // Wait for jobs to finish, since we'll have to access the produced mesh/bounds data at this point
        handle.Complete();

        // Final bounding box of the whole mesh is union of the bounds of individual transformed meshes
        var bounds = new float3x2(new float3(Mathf.Infinity), new float3(Mathf.NegativeInfinity));
        for (var i = 0; i < meshCount; ++i)
        {
            var b = jobs.bounds[i];
            bounds.c0 = math.min(bounds.c0, b.c0);
            bounds.c1 = math.max(bounds.c1, b.c1);
        }
        sm.bounds = new Bounds((bounds.c0 + bounds.c1) * 0.5f, bounds.c1 - bounds.c0);
        jobs.outputMesh.subMeshCount = 1;
        jobs.outputMesh.SetSubMesh(0, sm, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
        Mesh.ApplyAndDisposeWritableMeshData(outputMeshData, new[] { newMesh }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
        newMesh.bounds = sm.bounds;

        // Dispose of the read-only mesh data and temporary bounds array
        jobs.meshData.Dispose();
        jobs.bounds.Dispose();

        GetComponent<MeshFilter>().sharedMesh = newMesh;
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
        
        //Clearing all the children
        transform.Clear();
    }

    //This is an hybrid approach. All submeshes are merged with Unity Job System (considering their material), but at the end we use CombineMeshes
    //to keep the submesh separated in the final mesh
    private void MergeChildsIntoSingleObjectMixedApproach()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(false);
        List<Material> materials = new List<Material>();
        List<List<MeshFilter>> splittedSubmeshes = new List<List<MeshFilter>>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false);
        for (int j=0; j<renderers.Length; j++)
        {
            if (renderers[j].transform == transform)
                continue;
            Material[] localMats = renderers[j].sharedMaterials;
            foreach (Material localMat in localMats)
            {
                bool foundMat = false;
                for(int i= 0; i < materials.Count; i++)
                {
                    if (localMat.color == materials[i].color)
                    {
                        splittedSubmeshes[i].Add(filters[j]);
                        foundMat = true;
                        break;
                    }
                }
                if (!foundMat)
                {
                    //We subdivide the mesh and their materials in groups
                    materials.Add(localMat);
                    splittedSubmeshes.Add(new List<MeshFilter>());
                    splittedSubmeshes[splittedSubmeshes.Count - 1].Add(filters[j]);

                }
            }

        }

        //We create all the necessary materials in parent
        CreateMaterialsInParent(materials);

        //We merge all the objects with the same material into one mesh that will form one submesh of the final mesh
        List<Mesh> submeshes = MergeSubmeshesUnityJobs(splittedSubmeshes);

        // The final mesh: combine all the material-specific meshes as independent submeshes.
        List<CombineInstance> finalCombiners = new List<CombineInstance>();
        for (int i = 0; i < submeshes.Count; i++)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = submeshes[i];
            ci.subMeshIndex = 0;
            //ci.transform = Matrix4x4.identity;
            ci.transform = transform.localToWorldMatrix;
            finalCombiners.Add(ci);
        }
        Mesh finalMesh = new Mesh();
        finalMesh.name = "CombinedMesh";
        //Necessary to merge 32x32x32 voxels
        finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        finalMesh.CombineMeshes(finalCombiners.ToArray(), false);
        GetComponent<MeshFilter>().sharedMesh = finalMesh;

        //Clearing all the children
        transform.Clear();
    }

    //This function creates a List of Mesh merging all the objects in specific submeshes using Unity Job System
    public List<Mesh> MergeSubmeshesUnityJobs(List<List<MeshFilter>> splittedSubmeshes)
    {
        List<Mesh> result = new List<Mesh>();
        ProcessMeshDataJob[] allJobs = new ProcessMeshDataJob[splittedSubmeshes.Count];
        for(int j=0; j < allJobs.Length; j++)
        {
            allJobs[j] = new ProcessMeshDataJob();
            allJobs[j].CreateInputArrays(splittedSubmeshes[j].Count);
            var inputMeshes = new List<Mesh>(splittedSubmeshes[j].Count);
            var vertexStart = 0;
            var indexStart = 0;
            var meshCount = 0;
            for (var i = 0; i < splittedSubmeshes[j].Count; ++i)
            {
                var mf = splittedSubmeshes[j][i];
                var go = mf.gameObject;
                var mesh = mf.sharedMesh;
                inputMeshes.Add(mesh);
                allJobs[j].vertexStart[meshCount] = vertexStart;
                allJobs[j].indexStart[meshCount] = indexStart;
                allJobs[j].xform[meshCount] = go.transform.localToWorldMatrix;
                vertexStart += mesh.vertexCount;
                indexStart += (int)mesh.GetIndexCount(0);
                allJobs[j].bounds[meshCount] = new float3x2(new float3(Mathf.Infinity), new float3(Mathf.NegativeInfinity));
                ++meshCount;
            }

            // Acquire read-only data for input meshes
            allJobs[j].meshData = Mesh.AcquireReadOnlyMeshData(inputMeshes);

            // Create and initialize writable data for the output mesh
            var outputMeshData = Mesh.AllocateWritableMeshData(1);
            allJobs[j].outputMesh = outputMeshData[0];
            allJobs[j].outputMesh.SetIndexBufferParams(indexStart, IndexFormat.UInt32);
            allJobs[j].outputMesh.SetVertexBufferParams(vertexStart,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1));

            // Launch mesh processing jobs
            var handle = allJobs[j].Schedule(meshCount, 4);

            // Create destination Mesh object
            var newMesh = new Mesh();
            newMesh.name = "CombinedMesh";
            var sm = new SubMeshDescriptor(0, indexStart, MeshTopology.Triangles);
            sm.firstVertex = 0;
            sm.vertexCount = vertexStart;

            // Wait for jobs to finish, since we'll have to access the produced mesh/bounds data at this point
            handle.Complete();

            // Final bounding box of the whole mesh is union of the bounds of individual transformed meshes
            var bounds = new float3x2(new float3(Mathf.Infinity), new float3(Mathf.NegativeInfinity));
            for (var i = 0; i < meshCount; ++i)
            {
                var b = allJobs[j].bounds[i];
                bounds.c0 = math.min(bounds.c0, b.c0);
                bounds.c1 = math.max(bounds.c1, b.c1);
            }
            sm.bounds = new Bounds((bounds.c0 + bounds.c1) * 0.5f, bounds.c1 - bounds.c0);
            allJobs[j].outputMesh.subMeshCount = 1;
            allJobs[j].outputMesh.SetSubMesh(0, sm, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
            Mesh.ApplyAndDisposeWritableMeshData(outputMeshData, new[] { newMesh }, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
            newMesh.bounds = sm.bounds;

            // Dispose of the read-only mesh data and temporary bounds array
            allJobs[j].meshData.Dispose();
            allJobs[j].bounds.Dispose();
            result.Add(newMesh);
        }
        return result;
    }

    /// <summary>
    /// The struct needed by job system to work on meshData
    /// </summary>
    [BurstCompile]
    struct ProcessMeshDataJob : IJobParallelFor
    {
        [ReadOnly] public Mesh.MeshDataArray meshData;
        public Mesh.MeshData outputMesh;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> vertexStart;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> indexStart;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<float4x4> xform;
        public NativeArray<float3x2> bounds;

        [NativeDisableContainerSafetyRestriction] NativeArray<float3> tempVertices;
        [NativeDisableContainerSafetyRestriction] NativeArray<float3> tempNormals;

        public void CreateInputArrays(int meshCount)
        {
            vertexStart = new NativeArray<int>(meshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            indexStart = new NativeArray<int>(meshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            xform = new NativeArray<float4x4>(meshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            bounds = new NativeArray<float3x2>(meshCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        public void Execute(int index)
        {
            var data = meshData[index];
            var vCount = data.vertexCount;
            var mat = xform[index];
            var vStart = vertexStart[index];

            // Allocate temporary arrays for input mesh vertices/normals
            if (!tempVertices.IsCreated || tempVertices.Length < vCount)
            {
                if (tempVertices.IsCreated) tempVertices.Dispose();
                tempVertices = new NativeArray<float3>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            }
            if (!tempNormals.IsCreated || tempNormals.Length < vCount)
            {
                if (tempNormals.IsCreated) tempNormals.Dispose();
                tempNormals = new NativeArray<float3>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            }
            // Read input mesh vertices/normals into temporary arrays -- this will
            // do any necessary format conversions into float3 data
            data.GetVertices(tempVertices.Reinterpret<Vector3>());
            data.GetNormals(tempNormals.Reinterpret<Vector3>());

            var outputVerts = outputMesh.GetVertexData<Vector3>();
            var outputNormals = outputMesh.GetVertexData<Vector3>(stream: 1);

            // Transform input mesh vertices/normals, write into destination mesh,
            // compute transformed mesh bounds.
            var b = bounds[index];
            for (var i = 0; i < vCount; ++i)
            {
                var pos = tempVertices[i];
                pos = math.mul(mat, new float4(pos, 1)).xyz;
                outputVerts[i + vStart] = pos;
                var nor = tempNormals[i];
                nor = math.normalize(math.mul(mat, new float4(nor, 0)).xyz);
                outputNormals[i + vStart] = nor;
                b.c0 = math.min(b.c0, pos);
                b.c1 = math.max(b.c1, pos);
            }
            bounds[index] = b;

            // Write input mesh indices into destination index buffer
            var tStart = indexStart[index];
            var tCount = data.GetSubMesh(0).indexCount;
            var outputTris = outputMesh.GetIndexData<int>();
            if (data.indexFormat == IndexFormat.UInt16)
            {
                var tris = data.GetIndexData<ushort>();
                for (var i = 0; i < tCount; ++i)
                    outputTris[i + tStart] = vStart + tris[i];
            }
            else
            {
                var tris = data.GetIndexData<int>();
                for (var i = 0; i < tCount; ++i)
                    outputTris[i + tStart] = vStart + tris[i];
            }
        }
    }
}
