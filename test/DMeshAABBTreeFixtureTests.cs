using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using g3;
using Xunit;

namespace geometry3sharp.Tests;

public class DMeshAABBTreeFixtureTests
{
    [Fact]
    public void CubeNearestTriangleFixtureMatchesDefaultDMeshAABBTree()
    {
        string actual = File.ReadAllText(FixturePath);
        string expected = JsonSerializer.Serialize(CreateFixture(), SerializerOptions);

        Assert.Equal(expected, actual);
    }

    [Fact(Skip = "Run this method to regenerate the DMeshAABBTree fixture.")]
    public void ExportCubeNearestTriangleQueries()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FixturePath)!);
        File.WriteAllText(FixturePath, JsonSerializer.Serialize(CreateFixture(), SerializerOptions));
    }

    private static DMeshAABBTreeFixture CreateFixture()
    {
        DMesh3 mesh = new TrivialBox3Generator().Generate().MakeDMesh();
        var tree = new DMeshAABBTree3(mesh, autoBuild: true);
        var random = new Random(1_847_219);
        CubeQuery[] queries = Enumerable.Range(0, 128)
            .Select(index => CreateQueryPoint(random, index % 3, index % 2 == 0 ? -1 : 1))
            .Select(point =>
            {
                int triangleId = tree.FindNearestTriangle(point);
                Vector3d nearestPoint = MeshQueries.TriangleDistance(mesh, triangleId, point).TriangleClosest;
                return new CubeQuery
                {
                    X = point.x,
                    Y = point.y,
                    Z = point.z,
                    TriangleId = triangleId,
                    NearestPoint = new Point
                    {
                        X = nearestPoint.x,
                        Y = nearestPoint.y,
                        Z = nearestPoint.z,
                    },
                };
            })
            .ToArray();

        return new DMeshAABBTreeFixture
        {
            Vertices = mesh.Vertices().SelectMany(vertex => new[] { vertex.x, vertex.y, vertex.z }).ToArray(),
            Triangles = mesh.Triangles().SelectMany(triangle => new uint[]
            {
                (uint)triangle.a,
                (uint)triangle.b,
                (uint)triangle.c,
            }).ToArray(),
            Queries = queries,
        };
    }

    private static string FixturePath => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "..", "..",
        "lib", "tests", "fixtures", "dmesh-aabb-tree.json"));

    private static JsonSerializerOptions SerializerOptions => new JsonSerializerOptions { WriteIndented = true };

    private static Vector3d CreateQueryPoint(Random random, int normalAxis, int direction)
    {
        double normalCoordinate = direction * (0.6 + random.NextDouble() * 2.4);
        double firstTangentCoordinate = random.NextDouble() * 0.8 - 0.4;
        double secondTangentCoordinate = random.NextDouble() * 0.8 - 0.4;

        return normalAxis switch
        {
            0 => new Vector3d(normalCoordinate, firstTangentCoordinate, secondTangentCoordinate),
            1 => new Vector3d(firstTangentCoordinate, normalCoordinate, secondTangentCoordinate),
            _ => new Vector3d(firstTangentCoordinate, secondTangentCoordinate, normalCoordinate),
        };
    }

    private sealed class DMeshAABBTreeFixture
    {
        public double[] Vertices { get; set; } = Array.Empty<double>();

        public uint[] Triangles { get; set; } = Array.Empty<uint>();

        public CubeQuery[] Queries { get; set; } = Array.Empty<CubeQuery>();
    }

    private sealed class CubeQuery
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public int TriangleId { get; set; }

        public Point NearestPoint { get; set; } = new Point();
    }

    private sealed class Point
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }
    }
}
