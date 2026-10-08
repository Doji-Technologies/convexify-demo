using Convexify;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// Writes all hulls of a <see cref="NativeConvexDecomposition"/> into one flat-shaded mesh with a parallel Burst job.
    /// </summary>
    /// <remarks>
    /// Each triangle gets its own three vertices: position, face normal, hull color, hull center (TEXCOORD0, for the
    /// explode offset in the shader) and edge coordinates (TEXCOORD1). An edge between two coplanar triangles of the
    /// same hull face gets coordinates of at least 1, so the shader draws only the real edges of the hull faces.
    /// </remarks>
    public static class HullMeshBuilder {

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex {
            public float3 Position;
            public float3 Normal;
            public Color32 Color;
            public float3 Center;
            public float3 Edge;
        }

        private static readonly VertexAttributeDescriptor[] s_layout = {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 3),
        };

        private const MeshUpdateFlags Flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers |
                                              MeshUpdateFlags.DontRecalculateBounds;

        /// <summary>The hull color for hull index <paramref name="hull"/>: golden-ratio hue steps, soft saturation.</summary>
        public static Color32 HullColor(int hull) {
            const float s = 0.36f, v = 0.95f;
            float hue = math.frac(0.58f + hull * 0.61803398875f);
            float3 rgb = math.saturate(math.abs(math.frac(hue + new float3(1f, 2f / 3f, 1f / 3f)) * 6f - 3f) - 1f);
            rgb = v * math.lerp(1f, rgb, s) * 255f + 0.5f;
            return new Color32((byte)rgb.x, (byte)rgb.y, (byte)rgb.z, 255);
        }

        /// <summary>Rebuilds <paramref name="mesh"/> from the hulls of <paramref name="result"/>.</summary>
        public static void Build(NativeConvexDecomposition result, Mesh mesh) {
            int hullCount = result.HullCount;
            var vertexStart = new NativeArray<int>(hullCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            int total = 0;
            float3 min = float.MaxValue, max = float.MinValue;
            for (int h = 0; h < hullCount; h++) {
                NativeConvexHull hull = result.Hulls[h];
                vertexStart[h] = total;
                total += hull.IndexCount;
                min = math.min(min, hull.MinBounds);
                max = math.max(max, hull.MaxBounds);
            }

            Mesh.MeshDataArray array = Mesh.AllocateWritableMeshData(1);
            Mesh.MeshData md = array[0];
            md.SetVertexBufferParams(total, s_layout);
            md.SetIndexBufferParams(total, IndexFormat.UInt32);

            // two chained jobs: the vertex and index data of one MeshData share a safety handle
            NativeArray<Vertex> vertexData = md.GetVertexData<Vertex>();
            NativeArray<int> indexData = md.GetIndexData<int>();
            JobHandle vertices = new BuildJob {
                Points = result.Points,
                Indices = result.Indices,
                Hulls = result.Hulls,
                VertexStart = vertexStart,
                Vertices = vertexData,
            }.Schedule(hullCount, 1);
            new IndexJob { Indices = indexData }.Schedule(total, 4096, vertices).Complete();
            vertexStart.Dispose();

            var bounds = hullCount > 0 ? new Bounds((min + max) * 0.5f, max - min) : new Bounds();
            md.subMeshCount = 1;
            md.SetSubMesh(0, new SubMeshDescriptor(0, total) { bounds = bounds, vertexCount = total }, Flags);
            Mesh.ApplyAndDisposeWritableMeshData(array, mesh, Flags);
            // room for the explode offset of the shader
            bounds.Expand(bounds.size * 2f);
            mesh.bounds = bounds;
        }

        [BurstCompile]
        private struct BuildJob : IJobParallelFor {
            [ReadOnly] public NativeArray<float3> Points;
            [ReadOnly] public NativeArray<int> Indices;
            [ReadOnly] public NativeArray<NativeConvexHull> Hulls;
            [ReadOnly] public NativeArray<int> VertexStart;
            [NativeDisableParallelForRestriction] public NativeArray<Vertex> Vertices;

            public void Execute(int h) {
                NativeConvexHull hull = Hulls[h];
                NativeArray<float3> p = Points.GetSubArray(hull.PointStart, hull.PointCount);
                NativeArray<int> idx = Indices.GetSubArray(hull.IndexStart, hull.IndexCount);
                int triCount = idx.Length / 3;
                int start = VertexStart[h];
                Color32 color = HullColor(h);
                float3 center = hull.Center;

                // face normals, oriented away from the hull center
                var normals = new NativeArray<float3>(triCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var flipped = new NativeArray<bool>(triCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int t = 0; t < triCount; t++) {
                    float3 a = p[idx[3 * t]], b = p[idx[3 * t + 1]], c = p[idx[3 * t + 2]];
                    float3 n = math.normalizesafe(math.cross(b - a, c - a));
                    bool flip = math.dot(n, (a + b + c) / 3f - center) < 0;
                    normals[t] = flip ? -n : n;
                    flipped[t] = flip;
                }

                for (int t = 0; t < triCount; t++) {
                    int i0 = idx[3 * t], i1 = idx[3 * t + 1], i2 = idx[3 * t + 2];
                    // the edge opposite to corner k is hidden when the neighbor triangle lies in the same plane
                    float3 hidden = new float3(
                        IsFlatEdge(idx, normals, t, i1, i2) ? 1 : 0,
                        IsFlatEdge(idx, normals, t, i2, i0) ? 1 : 0,
                        IsFlatEdge(idx, normals, t, i0, i1) ? 1 : 0);

                    int o = start + 3 * t;
                    // swap two corners of a flipped triangle to keep the winding consistent with the normal
                    int c1 = flipped[t] ? 2 : 1, c2 = flipped[t] ? 1 : 2;
                    Write(o, p[i0], normals[t], color, center, new float3(1, 0, 0) + hidden);
                    Write(o + c1, p[i1], normals[t], color, center, new float3(0, 1, 0) + hidden);
                    Write(o + c2, p[i2], normals[t], color, center, new float3(0, 0, 1) + hidden);
                }
            }

            private void Write(int i, float3 position, float3 normal, Color32 color, float3 center, float3 edge) {
                Vertices[i] = new Vertex { Position = position, Normal = normal, Color = color, Center = center, Edge = edge };
            }

            /// <summary>True if the triangle across edge (a, b) of triangle <paramref name="t"/> has the same normal.</summary>
            private static bool IsFlatEdge(NativeArray<int> idx, NativeArray<float3> normals, int t, int a, int b) {
                int triCount = idx.Length / 3;
                for (int u = 0; u < triCount; u++) {
                    if (u == t) {
                        continue;
                    }
                    int u0 = idx[3 * u], u1 = idx[3 * u + 1], u2 = idx[3 * u + 2];
                    bool hasA = u0 == a || u1 == a || u2 == a;
                    bool hasB = u0 == b || u1 == b || u2 == b;
                    if (hasA && hasB) {
                        return math.dot(normals[t], normals[u]) > 0.999f;
                    }
                }
                return false;
            }
        }

        /// <summary>Each triangle has its own vertices, so the index buffer counts up.</summary>
        [BurstCompile]
        private struct IndexJob : IJobParallelFor {
            public NativeArray<int> Indices;

            public void Execute(int i) {
                Indices[i] = i;
            }
        }
    }
}
