using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace g3
{

    public class BinaryG3Writer : IMeshWriter
    {
        public IOWriteResult Write(BinaryWriter writer, List<WriteMesh> vMeshes, WriteOptions options)
        {
            int nMeshes = vMeshes.Count;
            writer.Write(nMeshes);
            for (int k = 0; k < vMeshes.Count; ++k)
            {
                DMesh3 mesh = vMeshes[k].Mesh as DMesh3;
                if (mesh == null)
                    throw new NotImplementedException("BinaryG3Writer.Write: can only write DMesh3 meshes");
                gSerialization.Store(mesh, writer);
            }

            return new IOWriteResult(IOCode.Ok, "");
        }

        public IOWriteResult Write(TextWriter writer, List<WriteMesh> vMeshes, WriteOptions options)
        {
            throw new NotSupportedException("BinaryG3 Writer does not support ascii mode");
        }

    }



    public class BinaryG3Reader
    {
        public Task<IOReadResult> ReadAsync(Stream stream, IMeshBuilder builder)
        {
            using (var reader = new BinaryReader(stream))
            {
                int nMeshes = reader.ReadInt32();
                if (nMeshes != 4)
                {
                    throw new DataMisalignedException("BinaryG3Writer.ReadAsync: there should have been an 1-byte-integer representing the number of DMesh3 meshes.");
                }

                for (int k = 0; k < nMeshes; ++k)
                {
                    DMesh3 m = new DMesh3();
                    gSerialization.Restore(m, reader);
                    builder.AppendNewMesh(m);
                }

                return Task.FromResult(new IOReadResult(IOCode.Ok, ""));
            }
        }
    }
}