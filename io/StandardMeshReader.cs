﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;

namespace g3
{

    public delegate void ParsingMessagesHandler(string message, object extra_data);


    public interface MeshFormatReader
    {
        List<string> SupportedExtensions { get; }
        IOReadResult ReadFile(string sFilename, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler warnings);
        IOReadResult ReadFile(Stream stream, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler warnings);
    }


    public class StandardMeshReader
    {
        /// <summary>
        /// If the mesh format we are writing is text, then the OS will write in the number style
        /// of the current language. So in Germany, numbers are written 1,00 instead of 1.00, for example.
        /// If this flag is true, we override this to always write in a consistent way.
        /// </summary>
        public bool ReadInvariantCulture = true;


        // connect to this to get warning status messages
        public event ParsingMessagesHandler warningEvent;

        /// <summary>
        /// The various format handlers will use this IMeshBuilder to construct meshes
        /// based on the file data and read options.
        /// Default is initialized to DMesh3Builder
        /// </summary>
        public IMeshBuilder MeshBuilder { get; set; }

        /// <summary>
        /// Set of format handlers
        /// </summary>
        List<MeshFormatReader> Readers = new List<MeshFormatReader>();


        /// <summary>
        /// Construct a MeshReader, optionally with default format handlers
        /// Initializes MeshBuilder to a DMesh3Builder
        /// </summary>
        public StandardMeshReader(bool bIncludeDefaultReaders = true)
        {
            Readers = new List<MeshFormatReader>();
            MeshBuilder = new DMesh3Builder();

            if (bIncludeDefaultReaders)
            {
                Readers.Add(new OBJFormatReader());
                Readers.Add(new STLFormatReader());
                Readers.Add(new OFFFormatReader());
                Readers.Add(new BinaryG3FormatReader());
            }
        }


        /// <summary>
        /// Check if extension type is supported
        /// </summary>
        public bool SupportsFormat(string sExtension)
        {
            foreach (MeshFormatReader reader in Readers)
                foreach (string ext in reader.SupportedExtensions)
                    if (ext.Equals(sExtension, StringComparison.OrdinalIgnoreCase))
                        return true;
            return false;
        }


        /// <summary>
        /// Add a handler for a given formta
        /// </summary>
        public void AddFormatHandler(MeshFormatReader reader)
        {
            List<string> formats = reader.SupportedExtensions;
            foreach (string s in formats)
                if (SupportsFormat(s))
                    throw new Exception("StandardMeshReader.AddFormatHandler: format " + s + " is already registered!");

            Readers.Add(reader);
        }

        /// <summary>
        /// Read mesh file at path, with given Options. Result is stored in MeshBuilder parameter
        /// </summary>
        public IOReadResult Read(string sFilename, ReadOptions options)
        {
            string? fileExtension = GetExtension(sFilename);
            if (fileExtension is null)
                return new IOReadResult(IOCode.InvalidFilenameError, $"The extension of '{fileExtension}' is not parsed");
            using FileStream fileStream = File.OpenRead(sFilename);
            return Read(fileStream, fileExtension, options);
        }

        /// <summary>
        /// Read mesh file at path, with given Options. Result is stored in MeshBuilder parameter
        /// </summary>
        public async Task<IOReadResult> ReadAsync(string sFilename, ReadOptions options, CancellationToken cancellationToken = default)
        {
            string? fileExtension = GetExtension(sFilename);
            if (fileExtension is null)
                return new IOReadResult(IOCode.InvalidFilenameError, $"The extension of '{fileExtension}' is not parsed");
            using FileStream fileStream = File.OpenRead(sFilename);
            byte[] buffer = new byte[fileStream.Length];
            using MemoryStream memoryStream = new MemoryStream(buffer);
            int readBytes =
                await fileStream.ReadAsync(buffer, offset: 0, count: buffer.Length, cancellationToken)
                    .ConfigureAwait(false);
            if (readBytes != buffer.Length)
                throw new Exception("Looks like the buffer length is not equal to the stream length");
            return Read(memoryStream, fileExtension, options);
        }

        private static string? GetExtension(string filename)
        {
            string extensionWithDot = Path.GetExtension(filename);
            if (extensionWithDot.Length < 2)
                return null;
            return extensionWithDot.Substring(1);
        }

        /// <summary>
        /// Read mesh file at path, with given Options. Result is stored in MeshBuilder parameter
        /// </summary>
        public async Task<IOReadResult> ReadAsync(Stream stream, string sExtension, ReadOptions options, CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream(capacity: (int)stream.Length);
            // 81_920 is a default value for the buffer size
            await stream.CopyToAsync(memoryStream, 81_920, cancellationToken).ConfigureAwait(false);
            memoryStream.Seek(offset: 0, loc: SeekOrigin.Begin);
            return Read(memoryStream, sExtension, options);
        }

        /// <summary>
        /// Read mesh file at path, with given Options. Result is stored in MeshBuilder parameter
        /// </summary>
        public IOReadResult Read(Stream stream, string sExtension, ReadOptions options)
        {
            if (MeshBuilder == null)
                return new IOReadResult(IOCode.GenericReaderError, "MeshBuilder is null!");
            MeshFormatReader useReader = null;
            foreach (MeshFormatReader reader in Readers)
            {
                foreach (string ext in reader.SupportedExtensions)
                {
                    if (ext.Equals(sExtension, StringComparison.OrdinalIgnoreCase))
                        useReader = reader;
                }
                if (useReader != null)
                    break;
            }
            if (useReader == null)
                return new IOReadResult(IOCode.UnknownFormatError, "format " + sExtension + " is not supported");

            // save current culture
            CultureInfo current_culture = Thread.CurrentThread.CurrentCulture;

            try
            {
                // push invariant culture for write
                if (ReadInvariantCulture)
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                IOReadResult result = useReader.ReadFile(stream, MeshBuilder, options, on_warning);

                // restore culture
                if (ReadInvariantCulture)
                    Thread.CurrentThread.CurrentCulture = current_culture;

                return result;

            }
            catch (Exception e)
            {
                // restore culture
                if (ReadInvariantCulture)
                    Thread.CurrentThread.CurrentCulture = current_culture;

                return new IOReadResult(IOCode.GenericReaderError, "Unknown error : exception : " + e.Message);
            }
        }





        /// <summary>
        /// Read mesh file using options and builder. You must provide our own Builder
        /// here because the reader is not returned
        /// </summary>
        static public async Task<IOReadResult> ReadFileAsync(string sFilename, ReadOptions options, IMeshBuilder builder, CancellationToken cancellationToken = default)
        {
            StandardMeshReader reader = new StandardMeshReader();
            reader.MeshBuilder = builder;
            return await reader.ReadAsync(sFilename, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Read mesh file using options and builder. You must provide our own Builder
        /// here because the reader is not returned
        /// </summary>
        static public IOReadResult ReadFile(string sFilename, ReadOptions options, IMeshBuilder builder)
        {
            StandardMeshReader reader = new StandardMeshReader();
            reader.MeshBuilder = builder;
            return reader.Read(sFilename, options);
        }

        /// <summary>
        /// Read mesh file using options and builder. You must provide our own Builder
        /// here because the reader is not returned
        /// </summary>
        static public async Task<IOReadResult> ReadFileAsync
            (Stream stream, string sExtension, ReadOptions options, IMeshBuilder builder, CancellationToken cancellationToken = default)
        {
            StandardMeshReader reader = new StandardMeshReader();
            reader.MeshBuilder = builder;
            return await reader.ReadAsync(stream, sExtension, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Read mesh file using options and builder. You must provide our own Builder
        /// here because the reader is not returned
        /// </summary>
        static public IOReadResult ReadFile(Stream stream, string sExtension, ReadOptions options, IMeshBuilder builder)
        {
            StandardMeshReader reader = new StandardMeshReader();
            reader.MeshBuilder = builder;
            return reader.Read(stream, sExtension, options);
        }



        /// <summary>
        /// This is basically a utility function, returns first mesh in file, with default options.
        /// </summary>
        static public async Task<DMesh3> ReadMeshAsync(string sFilename, CancellationToken cancellationToken = default)
        {
            DMesh3Builder builder = new DMesh3Builder();
            IOReadResult result = await ReadFileAsync(sFilename, ReadOptions.Defaults, builder, cancellationToken).ConfigureAwait(false);
            return (result.code == IOCode.Ok) ? builder.Meshes[0] : null;
        }


        /// <summary>
        /// This is basically a utility function, returns first mesh in file, with default options.
        /// </summary>
        static public async Task<DMesh3> ReadMeshAsync(Stream stream, string sExtension, CancellationToken cancellationToken = default)
        {
            DMesh3Builder builder = new DMesh3Builder();
            IOReadResult result = await ReadFileAsync(stream, sExtension, ReadOptions.Defaults, builder, cancellationToken).ConfigureAwait(false);
            return (result.code == IOCode.Ok) ? builder.Meshes[0] : null;
        }

        /// <summary>
        /// This is basically a utility function, returns first mesh in file, with default options.
        /// </summary>
        static public DMesh3 ReadMesh(Stream stream, string sExtension)
        {
            DMesh3Builder builder = new DMesh3Builder();
            IOReadResult result = ReadFile(stream, sExtension, ReadOptions.Defaults, builder);
            return (result.code == IOCode.Ok) ? builder.Meshes[0] : null;
        }

        /// <summary>
        /// This is basically a utility function, returns first mesh in file, with default options.
        /// </summary>
        static public DMesh3 ReadMesh(string sFilename)
        {
            DMesh3Builder builder = new DMesh3Builder();
            IOReadResult result = ReadFile(sFilename, ReadOptions.Defaults, builder);
            return (result.code == IOCode.Ok) ? builder.Meshes[0] : null;
        }

        private void on_warning(string message, object extra_data)
        {
            if (warningEvent != null)
                warningEvent(message, extra_data);
        }
    }



    // MeshFormatReader impl for OBJ
    public class OBJFormatReader : MeshFormatReader
    {
        public List<string> SupportedExtensions
        {
            get
            {
                return new List<string>() { "obj" };
            }
        }


        public IOReadResult ReadFile(string sFilename, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler messages)
        {
            try
            {
                using (FileStream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    OBJReader reader = new OBJReader();
                    if (options.ReadMaterials)
                        reader.MTLFileSearchPaths.Add(Path.GetDirectoryName(sFilename));
                    reader.warningEvent += messages;

                    IOReadResult result = reader.Read(new StreamReader(stream), options, builder);
                    return result;
                }
            }
            catch (Exception e)
            {
                return new IOReadResult(IOCode.FileAccessError, "Could not open file " + sFilename + " for reading : " + e.Message);
            }
        }

        public IOReadResult ReadFile(Stream stream, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler messages)
        {
            OBJReader reader = new OBJReader();
            reader.warningEvent += messages;
            return reader.Read(new StreamReader(stream), options, builder);
        }
    }



    // MeshFormatReader impl for STL
    public class STLFormatReader : MeshFormatReader
    {
        public List<string> SupportedExtensions
        {
            get
            {
                return new List<string>() { "stl" };
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
            // detect binary STL
            //BinaryReader binReader = new BinaryReader(stream);
            //byte[] header = binReader.ReadBytes(80);
            bool bIsBinary = Util.IsBinaryStream(stream, 500);

            // [RMS] Thingi10k includes some files w/ unicode string in ascii header...
            //   How can we detect this? can we check that each character is a character?
            //string sUTF8 = System.Text.Encoding.UTF8.GetString(header);
            //string sAscii = System.Text.Encoding.ASCII.GetString(header);
            //if (sUTF8.Contains("solid") == false && sAscii.Contains("solid") == false)
            //    bIsBinary = true;

            // if first 80 bytes contain non-text chars, probably a binary file
            //if (Util.IsTextString(header) == false)
            //    bIsBinary = true;
            //// if we don't see "solid" string in first 80 chars, probably binary
            //if (bIsBinary == false) {
            //    string sText = System.Text.Encoding.ASCII.GetString(header);
            //    if (sText.Contains("solid") == false)
            //        bIsBinary = true;
            //}

            stream.Seek(0, SeekOrigin.Begin); // reset stream

            STLReader reader = new STLReader();
            reader.warningEvent += messages;
            IOReadResult result = (bIsBinary) ?
                reader.Read(stream, options, builder) :
                reader.Read(new StreamReader(stream), options, builder);

            return result;
        }

    }




    // MeshFormatReader impl for OFF
    public class OFFFormatReader : MeshFormatReader
    {
        public List<string> SupportedExtensions
        {
            get
            {
                return new List<string>() { "off" };
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
            OFFReader reader = new OFFReader();
            reader.warningEvent += messages;
            return reader.Read(new StreamReader(stream), options, builder);
        }
    }



    // MeshFormatReader impl for g3mesh
    public class BinaryG3FormatReader : MeshFormatReader
    {
        public List<string> SupportedExtensions
        {
            get
            {
                return new List<string>() { "g3mesh" };
            }
        }

        public IOReadResult ReadFile(string sFilename, IMeshBuilder builder, ReadOptions options, ParsingMessagesHandler messages)
        {
            try
            {
                using (FileStream stream = File.Open(sFilename, FileMode.Open, FileAccess.Read))
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
            BinaryG3Reader reader = new BinaryG3Reader();
            //reader.warningEvent += messages;
            return reader.Read(stream, options, builder);
        }

    }
}
