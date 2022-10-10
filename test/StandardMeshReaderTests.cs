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
        private string ModelsDirectoryPath => Path.Combine("..", "..", "..", "models");

        [Fact(Skip = "Use this method to regenerate the binary STL")]
        public void Write()
        {
            DMesh3 boxMesh = new TrivialBox3Generator().Generate().MakeDMesh();

            Directory.CreateDirectory(ModelsDirectoryPath);
            using FileStream stream = File.OpenWrite(Path.Combine(ModelsDirectoryPath, "box_binary.stl"));
            using var writer = new BinaryWriter(stream);

            new STLWriter().Write(writer, new List<WriteMesh>() { new WriteMesh(boxMesh) }, WriteOptions.Defaults);
        }

        [Fact]
        public async Task ReadsBinaryStlVerticesAndTriangles()
        {
            Vector3d[] expectedVertices = new TrivialBox3Generator().Generate().MakeDMesh().Vertices().ToArray();
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

            readMesh.Vertices().Should()
                .BeEquivalentTo(expectedVertices,
                    ao => ao.Using<Vector3d>(context => context.Subject.EpsilonEqual(context.Expectation, epsilon: 1e-5).Should().BeTrue())
                    .WhenTypeIs<Vector3d>());
            readMesh.Triangles().Should()
                .BeEquivalentTo(expectedTrianlges);
        }
    }
}
