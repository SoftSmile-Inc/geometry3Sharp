#nullable enable

using System;
using System.Buffers;

namespace g3
{
    public class MeshNormals
    {
        public DMesh3 Mesh;
        public DVector<Vector3d> Normals;

        /// <summary>
        /// By default this is Mesh.GetVertex(). Can override to provide
        /// alternate vertex source.
        /// </summary>
        public Func<int, Vector3d> VertexF;

        public enum NormalsTypes
        {
            Vertex_OneRingFaceAverage_AreaWeighted
        }
        public NormalsTypes NormalType;


        public MeshNormals(DMesh3 mesh, NormalsTypes eType = NormalsTypes.Vertex_OneRingFaceAverage_AreaWeighted)
        {
            Mesh = mesh;
            NormalType = eType;
            Normals = new DVector<Vector3d>();
            VertexF = Mesh.GetVertex;
        }


        public void Compute(ArrayPool<Vector3f>? arrayPool = null)
        {
            arrayPool ??= ArrayPool<Vector3f>.Shared;
            Vector3f[] normalsArray = arrayPool.Rent(Mesh.MaxVertexID);
            QuickComputeToArray(Mesh, normalsArray);
            int NV = Mesh.MaxVertexID;
            if (NV != Normals.size)
                Normals.resize(NV);
            for (int vid = 0; vid < NV; ++vid)
                Normals[vid] = (Vector3d)normalsArray[vid];
            arrayPool.Return(normalsArray);
        }

        public Vector3d this[int vid] {
            get { return Normals[vid]; }
        }


        public void CopyTo(DMesh3 SetMesh)
        {
            if (SetMesh.MaxVertexID < Mesh.MaxVertexID)
                throw new Exception("MeshNormals.Set: SetMesh does not have enough vertices!");
            if (!SetMesh.HasVertexNormals)
                SetMesh.EnableVertexNormals(Vector3f.AxisY);
            int NV = Mesh.MaxVertexID;
            for ( int vi = 0; vi < NV; ++vi ) {
                if ( Mesh.IsVertex(vi) && SetMesh.IsVertex(vi) ) {
                    SetMesh.SetVertexNormal(vi, (Vector3f)Normals[vi]);
                }
            }
        }

        public static void QuickCompute(DMesh3 mesh, ArrayPool<Vector3f>? arrayPool = null)
        {
            arrayPool ??= ArrayPool<Vector3f>.Shared;
            Vector3f[] normalsArray = arrayPool.Rent(mesh.MaxVertexID);
            QuickComputeToArray(mesh, normalsArray);
            // Write to mesh
            mesh.EnableVertexNormals(Vector3f.Zero);
            for (int vid = 0; vid < mesh.MaxVertexID; vid++)
            {
                if (!mesh.IsVertex(vid))
                    continue;
                mesh.SetVertexNormal(vid, normalsArray[vid]);
            }
            arrayPool.Return(normalsArray);
        }

        private static void QuickComputeToArray(DMesh3 mesh, Vector3f[] normalsArray)
        {
            for (int vid = 0; vid < mesh.MaxVertexID; vid++)
                normalsArray[vid] = Vector3f.Zero;
            for (int tid = 0; tid < mesh.MaxTriangleID; tid++)
            {
                if (!mesh.IsTriangle(tid))
                    continue;
                Index3i triangle = mesh.GetTriangle(tid);
                Vector3d vertexA = mesh.GetVertexUnsafe(triangle.a);
                Vector3d vertexB = mesh.GetVertexUnsafe(triangle.b);
                Vector3d vertexC = mesh.GetVertexUnsafe(triangle.c);
                Vector3d triangleNormal = MathUtil.Normal(vertexA, vertexB, vertexC);
                double triangleArea = MathUtil.Area(vertexA, vertexB, vertexC);
                Vector3f areaNormalMultiplication = (Vector3f)(triangleArea * triangleNormal);
                normalsArray[triangle.a] += areaNormalMultiplication;
                normalsArray[triangle.b] += areaNormalMultiplication;
                normalsArray[triangle.c] += areaNormalMultiplication;
            }
            // Normalize array
            for (int vid = 0; vid < mesh.MaxVertexID; vid++)
            {
                Vector3f vertexNormal = normalsArray[vid];
                if (vertexNormal.LengthSquared > MathUtil.ZeroTolerancef)
                    normalsArray[vid] = vertexNormal.Normalized;
            }
        }


        public static Vector3d QuickCompute(DMesh3 mesh, int vid, NormalsTypes type = NormalsTypes.Vertex_OneRingFaceAverage_AreaWeighted)
        {
            Vector3d sum = Vector3d.Zero;
            Vector3d n, c; double a;
            foreach ( int tid in mesh.VtxTrianglesItr(vid)) {
                mesh.GetTriInfo(tid, out n, out a, out c);
                sum += a * n;
            }
            return sum.Normalized;
        }


    }
}
