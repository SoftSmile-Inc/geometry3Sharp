using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace g3
{
    //
    // Parse OFF mesh format
    // https://en.wikipedia.org/wiki/OFF_(file_format)
    // 
    class OFFReader : IMeshReader
    {
        // connect to this to get warning messages
        public event ParsingMessagesHandler warningEvent;

        //int nWarningLevel = 0;      // 0 == no diagnostics, 1 == basic, 2 == crazy
        Dictionary<string, int> warningCount = new Dictionary<string, int>();


        public async Task<IOReadResult> ReadAsync(TextReader reader, ReadOptions options, IMeshBuilder builder, CancellationToken cancellationToken = default)
        {
            // format is:
            //
            // OFF
            // VCOUNT TCOUNT     (2 ints)
            // x y z
            // ...
            // 3 va vb vc
            // ...
            //

            string first_line = await reader.ReadLineAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            if (first_line.StartsWith("OFF") == false)
                return new IOReadResult(IOCode.FileParsingError, "ascii OFF file must start with OFF header");

            int nVertexCount = 0;
            int nTriangleCount = 0;

            int nLines = 0;
            while (reader.Peek() >= 0)
            {
                string line = await reader.ReadLineAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                nLines++;
                string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0].StartsWith("#"))
                    continue;

                if (tokens.Length != 3)
                    return new IOReadResult(IOCode.FileParsingError, "first non-comment line of OFF must be vertex/tri/edge counts, found: " + line);
                nVertexCount = int.Parse(tokens[0]);
                nTriangleCount = int.Parse(tokens[1]);
                //int nEdgeCount = int.Parse(tokens[2]);
                break;
            }

            builder.AppendNewMesh(false, false, false, false);

            int vi = 0;
            while (vi < nVertexCount && reader.Peek() > 0)
            {
                string line = await reader.ReadLineAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                nLines++;
                string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0].StartsWith("#"))
                    continue;

                if (tokens.Length != 3)
                    emit_warning("found invalid OFF vertex line: " + line);

                double x = double.Parse(tokens[0]);
                double y = double.Parse(tokens[1]);
                double z = double.Parse(tokens[2]);
                builder.AppendVertex(x, y, z);
                vi++;
            }
            if (vi < nVertexCount)
                return new IOReadResult(IOCode.FileParsingError,
                    string.Format("File specified {0} vertices but only found {1}", nVertexCount, vi));


            int ti = 0;
            while (ti < nTriangleCount && reader.Peek() > 0)
            {
                string line = await reader.ReadLineAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                nLines++;
                string[] tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0].StartsWith("#"))
                    continue;

                if (tokens.Length < 4)
                    emit_warning("found invalid OFF triangle line: " + line);

                int nV = int.Parse(tokens[0]);
                if (nV != 3)
                    emit_warning("found non-triangle polygon in OFF, currently unsupported: " + line);

                int a = int.Parse(tokens[1]);
                int b = int.Parse(tokens[2]);
                int c = int.Parse(tokens[3]);

                builder.AppendTriangle(a, b, c);
                ti++;
            }
            if (ti < nTriangleCount)
                emit_warning(string.Format("File specified {0} triangles but only found {1}", nTriangleCount, ti));

            return new IOReadResult(IOCode.Ok, "");
        }

        private void emit_warning(string sMessage)
        {
            string sPrefix = sMessage.Substring(0, 15);
            int nCount = warningCount.ContainsKey(sPrefix) ? warningCount[sPrefix] : 0;
            nCount++; warningCount[sPrefix] = nCount;
            if (nCount > 10)
                return;
            else if (nCount == 10)
                sMessage += " (additional message surpressed)";

            warningEvent?.Invoke(sMessage, null);
        }

    }
}
