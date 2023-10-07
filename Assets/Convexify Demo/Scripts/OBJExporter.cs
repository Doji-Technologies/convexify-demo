using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class OBJExporter {

    public static byte[] Serialize(Mesh mesh) {
        using (MemoryStream stream = new MemoryStream()) {
            using (StreamWriter writer = new StreamWriter(stream)) {
                writer.WriteHeader();
                writer.WriteMesh(mesh);
            }
            return stream.ToArray();
        }
    }

    public static byte[] Serialize(IEnumerable<Mesh> meshes) {
        using (MemoryStream stream = new MemoryStream()) {
            using (StreamWriter writer = new StreamWriter(stream)) {
                writer.WriteHeader();
                writer.WriteMeshes(meshes);
            }
            return stream.ToArray();
        }
    }

    public static void WriteToFile(Mesh mesh, string path) {
        StreamWriter writer = new StreamWriter(path);
        writer.WriteHeader();
        writer.WriteMesh(mesh);
        writer.Close();
    }

    public static void WriteToFile(IEnumerable<Mesh> meshes, string path) {
        StreamWriter writer = new StreamWriter(path);
        writer.WriteHeader();
        writer.WriteMeshes(meshes);
        writer.Close();
    }

    private static void WriteHeader(this StreamWriter writer) {
        writer.WriteLine("# Created with Convexify Demo");
        writer.WriteLine("# https://assetstore.unity.com/packages/slug/245029");
        writer.WriteLine("# https://www.doji-tech.com/convexify-demo/");
    }

    private static void WriteMesh(this StreamWriter writer, Mesh mesh, int vertexOffset = 0) {
        // Export vertices
        foreach (Vector3 vertex in mesh.vertices) {
            writer.WriteLine($"v {vertex.x} {vertex.y} {vertex.z}");
        }

        // Export triangles
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3) {
            // calculate the 1-based indices
            // including optional offset when writing multiple objects
            int i1 = triangles[i    ] + 1 + vertexOffset;
            int i2 = triangles[i + 1] + 1 + vertexOffset;
            int i3 = triangles[i + 2] + 1 + vertexOffset;
            writer.WriteLine($"f {i1} {i2} {i3}");
        }
    }

    private static void WriteMeshes(this StreamWriter writer, IEnumerable<Mesh> meshes) {
        int vertexOffset = 0;
        foreach (Mesh mesh in meshes) {
            writer.WriteLine("o " + mesh.name);
            writer.WriteMesh(mesh, vertexOffset);
            vertexOffset += mesh.vertices.Length;
        }
    }
}
