using System.Globalization;
using System.Text;
using Doji.Convexify;
using Unity.Mathematics;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// Writes the hulls of a decomposition as a Wavefront OBJ file, one object for each hull.
    /// </summary>
    public static class OBJExporter {

        public static byte[] Serialize(NativeConvexDecomposition result, string name) {
            var sb = new StringBuilder(1 << 16);
            CultureInfo c = CultureInfo.InvariantCulture;
            sb.AppendLine("# Created with the Convexify demo");
            sb.AppendLine("# https://assetstore.unity.com/packages/slug/245029");
            sb.AppendLine("# https://www.doji-tech.com/convexify-demo/");
            sb.Append("# ").Append(result.HullCount).AppendLine(" convex hulls");

            int vertexOffset = 1; // OBJ indices are 1-based
            for (int h = 0; h < result.HullCount; h++) {
                sb.Append("o ").Append(name).Append("_ConvexHull_").Append(h).AppendLine();
                foreach (float3 v in result.GetPoints(h)) {
                    sb.Append("v ").Append(v.x.ToString("R", c)).Append(' ')
                      .Append(v.y.ToString("R", c)).Append(' ')
                      .Append(v.z.ToString("R", c)).AppendLine();
                }
                var idx = result.GetIndices(h);
                for (int i = 0; i < idx.Length; i += 3) {
                    sb.Append("f ").Append(idx[i] + vertexOffset).Append(' ')
                      .Append(idx[i + 1] + vertexOffset).Append(' ')
                      .Append(idx[i + 2] + vertexOffset).AppendLine();
                }
                vertexOffset += result.Hulls[h].PointCount;
            }
            return new UTF8Encoding(false).GetBytes(sb.ToString());
        }
    }
}
