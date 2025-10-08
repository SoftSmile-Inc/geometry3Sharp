using System;
using System.Collections.Generic;
using System.IO;

namespace g3
{
    public class PlyFormatReader : MeshFormatReader
    {
        public List<string> SupportedExtensions
        {
            get
            {
                return new List<string>() { "ply" };
            }
        }


        public IOReadResult ReadFile(string sFilename, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler messages)
        {
            try
            {
                using (FileStream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return ReadFile(stream, builder, options, messages);
                }
            }
            catch (Exception e)
            {
                return new IOReadResult(IOCode.FileAccessError, "Could not open file " + sFilename + " for reading : " + e.Message);
            }
        }

        public IOReadResult ReadFile(Stream stream, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler messages)
        {
            PLYReader reader = new PLYReader();
            reader.warningEvent += messages;
            return reader.Read(new StreamReader(stream), options, builder);
        }
    }
}