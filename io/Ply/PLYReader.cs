using System;
using System.IO;
using System.Text;
using Ply.Net;

namespace g3
{
    public class PLYReader : IMeshReader, IBinaryMeshReader
    {
        private const int MaxChunkSize = int.MaxValue;

        public event ParsingMessagesHandler warningEvent;

        public IOReadResult Read(TextReader reader, ReadOptions options, IMeshBuilder builder)
        {
            string text = reader.ReadToEnd();
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            return Read(new MemoryStream(bytes), options, builder);
        }

        public IOReadResult Read(Stream stream, ReadOptions options, IMeshBuilder builder)
        {
            builder.AppendNewMesh(false, false, false, false);

            PlyParser.Dataset dataset = PlyParser.Parse(stream, MaxChunkSize);

            foreach (PlyParser.ElementData elementData in dataset.Data)
            {
                if (elementData.Element.Type == PlyParser.ElementType.Vertex)
                {
                    float[] xData = (float[])elementData.Data[0].Data;
                    float[] yData = (float[])elementData.Data[1].Data;
                    float[] zData = (float[])elementData.Data[2].Data;

                    int length = Math.Min(MaxChunkSize, MathUtil.Min(xData.Length, yData.Length, zData.Length));
                    for (int i = 0; i < length; i++)
                    {
                        float x = xData[i];
                        float y = yData[i];
                        float z = zData[i];

                        builder.AppendVertex(x, y, z);
                    }
                }
                else if (elementData.Element.Type == PlyParser.ElementType.Face)
                {
                    foreach (PlyParser.PropertyData propertyData in elementData.Data)
                    {
                        foreach (object array in propertyData.Data)
                        {
                            if (array is int[] indices)
                            {
                                builder.AppendTriangle(indices[0], indices[1], indices[2]);
                            }
                        }
                    }
                }
            }

            return IOReadResult.Ok;
        }
    }
}