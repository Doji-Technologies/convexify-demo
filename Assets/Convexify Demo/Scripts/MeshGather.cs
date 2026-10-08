using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// Collects the triangles of all meshes below a root object into one vertex and index array,
    /// in the local space of the root. A Burst job reads the mesh data.
    /// </summary>
    public static class MeshGather {

        /// <summary>
        /// Gathers the meshes of all <see cref="MeshFilter"/>s below <paramref name="root"/>.
        /// The caller owns the output arrays (persistent allocator).
        /// </summary>
        public static bool Gather(GameObject root, out NativeArray<float3> vertices, out NativeArray<int> indices) {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var meshes = new List<Mesh>(filters.Length);
            var matrices = new List<float4x4>(filters.Length);
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            foreach (MeshFilter mf in filters) {
                if (mf.sharedMesh == null || !mf.sharedMesh.isReadable) {
                    continue;
                }
                meshes.Add(mf.sharedMesh);
                matrices.Add(toRoot * mf.transform.localToWorldMatrix);
            }

            using Mesh.MeshDataArray data = Mesh.AcquireReadOnlyMeshData(meshes);

            // offsets of each mesh and sub mesh in the output arrays
            var vertexStart = new NativeArray<int>(meshes.Count, Allocator.TempJob);
            var subMeshes = new NativeList<SubMeshRef>(meshes.Count, Allocator.TempJob);
            int vertexCount = 0, indexCount = 0;
            for (int m = 0; m < meshes.Count; m++) {
                Mesh.MeshData md = data[m];
                vertexStart[m] = vertexCount;
                for (int s = 0; s < md.subMeshCount; s++) {
                    var desc = md.GetSubMesh(s);
                    if (desc.topology != MeshTopology.Triangles || desc.indexCount == 0) {
                        continue;
                    }
                    subMeshes.Add(new SubMeshRef { Mesh = m, SubMesh = s, IndexStart = indexCount, IndexCount = desc.indexCount });
                    indexCount += desc.indexCount;
                }
                vertexCount += md.vertexCount;
            }

            vertices = new NativeArray<float3>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            indices = new NativeArray<int>(indexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var transforms = new NativeArray<float4x4>(matrices.ToArray(), Allocator.TempJob);

            new GatherJob {
                Data = data,
                Matrices = transforms,
                VertexStart = vertexStart,
                SubMeshes = subMeshes.AsArray(),
                Vertices = vertices,
                Indices = indices,
            }.Run();

            transforms.Dispose();
            vertexStart.Dispose();
            subMeshes.Dispose();
            return indexCount >= 3;
        }

        private struct SubMeshRef {
            public int Mesh;
            public int SubMesh;
            public int IndexStart;
            public int IndexCount;
        }

        [BurstCompile]
        private struct GatherJob : IJob {
            [ReadOnly] public Mesh.MeshDataArray Data;
            [ReadOnly] public NativeArray<float4x4> Matrices;
            [ReadOnly] public NativeArray<int> VertexStart;
            [ReadOnly] public NativeArray<SubMeshRef> SubMeshes;
            public NativeArray<float3> Vertices;
            public NativeArray<int> Indices;

            public void Execute() {
                for (int m = 0; m < Data.Length; m++) {
                    Mesh.MeshData md = Data[m];
                    NativeArray<float3> v = Vertices.GetSubArray(VertexStart[m], md.vertexCount);
                    md.GetVertices(v.Reinterpret<Vector3>());
                    float4x4 matrix = Matrices[m];
                    for (int i = 0; i < v.Length; i++) {
                        v[i] = math.transform(matrix, v[i]);
                    }
                }
                for (int s = 0; s < SubMeshes.Length; s++) {
                    SubMeshRef r = SubMeshes[s];
                    NativeArray<int> idx = Indices.GetSubArray(r.IndexStart, r.IndexCount);
                    Data[r.Mesh].GetIndices(idx, r.SubMesh);
                    int offset = VertexStart[r.Mesh];
                    for (int i = 0; i < idx.Length; i++) {
                        idx[i] += offset;
                    }
                }
            }
        }
    }
}
