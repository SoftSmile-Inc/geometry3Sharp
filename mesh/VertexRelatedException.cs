using System;

namespace g3
{
    public class VertexRelatedException : Exception
    {
        public readonly int VertexId;

        public VertexRelatedException(string message, int vertexId)
            : base(message)
        {
            VertexId = vertexId;
        }

        public VertexRelatedException(string message, int vertexId, Exception innerException)
            : base(message, innerException)
        {
            VertexId = vertexId;
        }
    }
}