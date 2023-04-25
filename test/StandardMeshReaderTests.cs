using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using g3;
using Xunit;

namespace geometry3sharp.Tests
{
    public class StandardMeshReaderTests
    {
        private static string ModelsDirectoryPath => Path.Combine("..", "..", "..", "models");
        private static string BoxPathWithoutExtension => Path.Combine(ModelsDirectoryPath, "box");
        private static readonly Lazy<DMesh3> BoxMesh =
            new Lazy<DMesh3>(() => new TrivialBox3Generator().Generate().MakeDMesh());

        [Fact(Skip = "Use this method to regenerate the box")]
        public void WriteAllFiles()
        {
            if (!Directory.Exists(ModelsDirectoryPath))
                Directory.CreateDirectory(ModelsDirectoryPath);

            DMesh3 boxMesh = BoxMesh.Value;
            WriteOptions writeOptios = WriteOptions.Defaults;
            writeOptios.bWriteBinary = false;
            StandardMeshWriter.WriteMesh($"{BoxPathWithoutExtension}.obj", boxMesh, WriteOptions.Defaults);
            StandardMeshWriter.WriteMesh($"{BoxPathWithoutExtension}.off", boxMesh, WriteOptions.Defaults);
            StandardMeshWriter.WriteMesh($"{BoxPathWithoutExtension}.g3mesh", boxMesh, WriteOptions.Defaults);
            // We don't have a GTS reader
            // StandardMeshWriter.WriteMesh($"{BoxPathWithoutExtension}.gts", boxMesh, WriteOptions.Defaults);
            StandardMeshWriter.WriteMesh($"{BoxPathWithoutExtension}.stl", boxMesh, WriteOptions.Defaults);

            // Manually call the binary stl writer
            using FileStream stream = File.OpenWrite(Path.Combine(ModelsDirectoryPath, "box_binary.stl"));
            using var writer = new BinaryWriter(stream);

            new STLWriter().Write(writer, new List<WriteMesh>() { new WriteMesh(boxMesh) }, WriteOptions.Defaults);
        }

        [Fact]
        public async Task ReadsBinaryStlVerticesAndTriangles()
        {
            Vector3d[] expectedVertices = BoxMesh.Value.Vertices().ToArray();
            // The expected triangles list is resorted because the binary stl format
            // doesn't have triangles indices and reading them requires welding vertices
            Index3i[] expectedTrianlges = new[]
            {
                new Index3i(0, 1, 2),
                new Index3i(0, 2, 3),
                new Index3i(4, 5, 6),
                new Index3i(4, 6, 7),
                new Index3i(1, 4, 7),
                new Index3i(1, 7, 2),
                new Index3i(5, 0, 3),
                new Index3i(5, 3, 6),
                new Index3i(1, 0, 5),
                new Index3i(1, 5, 4),
                new Index3i(7, 6, 3),
                new Index3i(7, 3, 2)
            };

            DMesh3 readMesh = await StandardMeshReader.ReadMeshAsync(Path.Combine(ModelsDirectoryPath, "box_binary.stl"));

            AssertMeshComponents(readMesh, expectedVertices, expectedTrianlges);
        }

        [Fact]
        public void ShouldReadObjVerticesAndTriangles()
        {
            using FileStream fileStream = File.OpenRead($"{BoxPathWithoutExtension}.obj");
            DMesh3 readMesh = StandardMeshReader.ReadMesh(fileStream, "obj");

            DMesh3 boxMesh = BoxMesh.Value;
            AssertMeshComponents(readMesh, boxMesh.Vertices(), boxMesh.Triangles());
        }

        [Fact]
        public void ShouldReadOffVerticesAndTriangles()
        {
            using FileStream fileStream = File.OpenRead($"{BoxPathWithoutExtension}.off");
            DMesh3 readMesh = StandardMeshReader.ReadMesh(fileStream, "off");

            DMesh3 boxMesh = BoxMesh.Value;
            AssertMeshComponents(readMesh, boxMesh.Vertices(), boxMesh.Triangles());
        }

        [Fact]
        public void ShouldReadGMesh3VerticesAndTriangles()
        {
            using FileStream fileStream = File.OpenRead($"{BoxPathWithoutExtension}.g3mesh");
            DMesh3 readMesh = StandardMeshReader.ReadMesh(fileStream, "g3mesh");

            DMesh3 boxMesh = BoxMesh.Value;
            AssertMeshComponents(readMesh, boxMesh.Vertices(), boxMesh.Triangles());
        }

        [Fact]
        public void ShouldReadTextStlVerticesAndTriangles()
        {
            // The expected triangles list is resorted because the binary stl format
            // doesn't have triangles indices and reading them requires welding vertices
            Index3i[] expectedTrianlges = new[]
            {
                new Index3i(0, 1, 2),
                new Index3i(0, 2, 3),
                new Index3i(4, 5, 6),
                new Index3i(4, 6, 7),
                new Index3i(1, 4, 7),
                new Index3i(1, 7, 2),
                new Index3i(5, 0, 3),
                new Index3i(5, 3, 6),
                new Index3i(1, 0, 5),
                new Index3i(1, 5, 4),
                new Index3i(7, 6, 3),
                new Index3i(7, 3, 2)
            };
            using FileStream fileStream = File.OpenRead($"{BoxPathWithoutExtension}.stl");
            DMesh3 readMesh = StandardMeshReader.ReadMesh(fileStream, "stl");

            DMesh3 boxMesh = BoxMesh.Value;
            AssertMeshComponents(readMesh, boxMesh.Vertices(), expectedTrianlges);
        }

        private static void AssertMeshComponents(DMesh3 mesh,
            IEnumerable<Vector3d> expectedVertices,
            IEnumerable<Index3i> expectedTrianlges)
        {
            mesh.Vertices().Should()
                .BeEquivalentTo(expectedVertices,
                    ao => ao.Using<Vector3d>(context => context.Subject.EpsilonEqual(context.Expectation, epsilon: 1e-5).Should().BeTrue())
                    .WhenTypeIs<Vector3d>());
            mesh.Triangles().Should()
                .BeEquivalentTo(expectedTrianlges);
        }
    }
}
