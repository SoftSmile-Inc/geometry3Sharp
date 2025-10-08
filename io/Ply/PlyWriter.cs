using System;
using System.Collections.Generic;
using System.IO;

namespace g3
{
    public class PlyWriter : IMeshWriter
    {
        public IOWriteResult Write(TextWriter writer, List<WriteMesh> vMeshes, WriteOptions options)
        {
            if (vMeshes.Count != 1)
                throw new Exception("Ply writer supports only single mesh exporting");

            if (options.bWriteBinary)
                throw new Exception("Binary format is not supported by the PLY writer");

            IMesh mesh = vMeshes[0].Mesh;

            // write header
            writer.WriteLine("ply");
            writer.WriteLine("format ascii 1.0");
            writer.WriteLine($"element vertex {mesh.VertexCount}");
            writer.WriteLine("property float x");
            writer.WriteLine("property float y");
            writer.WriteLine("property float z");
            writer.WriteLine($"element face {mesh.TriangleCount}");
            writer.WriteLine("property list uchar int vertex_indices");
            writer.WriteLine("end_header");

            // write vertexes
            foreach (int vertexId in mesh.VertexIndices())
            {
                Vector3d vertex = mesh.GetVertex(vertexId);
                writer.WriteLine($"{vertex.x} {vertex.y} {vertex.z}");
            }

            // write triangles
            foreach (int triangleId in mesh.TriangleIndices())
            {
                Index3i triangle = mesh.GetTriangle(triangleId);
                writer.WriteLine($"3 {triangle.a} {triangle.b} {triangle.c}");
            }

            return IOWriteResult.Ok;
        }


        public IOWriteResult Write(BinaryWriter writer, List<WriteMesh> vMeshes, WriteOptions options)
        {
            throw new NotImplementedException();
        }
    }
}