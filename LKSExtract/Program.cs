using System;
using System.IO;
using System.Text;

namespace LKSExtract
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (Environment.GetCommandLineArgs().Length < 2)
            {
                Console.WriteLine("[Error] No arguments provided. Please provide the file path to the archive or folder.");
            }
            else
            {
                string path = Environment.GetCommandLineArgs()[1];
                if (Directory.Exists(path))
                {
                    Repack(path);
                }
                else if (File.Exists(path))
                {
                    Extract(path);
                }
                else
                {
                    Console.WriteLine("[Error] Invalid filepath or directory provided. Please check the path provided and try again.");
                }

            }
        }



        static void Extract(string filepath)
        {
            Console.WriteLine("[Extract] File detected, extracting contents...");
            BinaryReader br = new BinaryReader(new FileStream(filepath, FileMode.Open));

            //Check file signature
            if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "PCKG")
            {
                Console.WriteLine("[Error] Signature check failed, file is not valid archive...");
                return;
            };

            //Skip empty header space
            br.BaseStream.Position = 0x20;

            Directory.CreateDirectory($"{filepath}_extracted");
            while (true)
            {
                ArchiveFileEntry fileEntry = new ArchiveFileEntry();
                fileEntry.Offset = (uint)br.BaseStream.Position;
                fileEntry.Length = br.ReadUInt32();
                fileEntry.FileSize = br.ReadUInt32();
                fileEntry.HeaderSize = br.ReadUInt32();
                char c = br.ReadChar();
                while (c != 0x0)
                {
                    fileEntry.FileName += c;
                    c = br.ReadChar();
                }


                Console.WriteLine($"[Extract] Extracting file {fileEntry.FileName}");
                br.BaseStream.Position = fileEntry.Offset + fileEntry.HeaderSize;

                Console.WriteLine($"{filepath}_extracted\\{fileEntry.FileName}");
                BinaryWriter bw = new BinaryWriter(new FileStream($"{filepath}_extracted\\{fileEntry.FileName}", FileMode.Create));
                bw.Write(br.ReadBytes((int)fileEntry.FileSize));
                bw.Close();



                br.BaseStream.Position = fileEntry.Offset + fileEntry.Length;

                if (fileEntry.Length == 0)
                {
                    break;
                }

            }
            br.Close();
        }

        static void Repack(string filepath)
        {
            Console.WriteLine("[Repack] Folder detected, repacking contents...");
            BinaryWriter bw = new BinaryWriter(new FileStream($"{filepath}_packed.pac", FileMode.Create));

            //Write Header
            bw.Write(Encoding.ASCII.GetBytes("PCKG"));
            //Write padding
            bw.BaseStream.Position = 0x20;


            string[] filesToImport = Directory.GetFiles(filepath);

            for (int i = 0; i < filesToImport.Length; i++)
            {
                BinaryReader br = new BinaryReader(new FileStream(filesToImport[i], FileMode.Open));
                long offset = bw.BaseStream.Position;
                uint fileSize = (uint)br.BaseStream.Length;
                uint headerSize = 0x20;

                uint entrySize = fileSize + headerSize;
                if (i == filesToImport.Length - 1) entrySize = 0;

                byte[] valueBytes = BitConverter.GetBytes(entrySize);
                Array.Reverse(valueBytes);
                bw.Write(valueBytes);

                valueBytes = BitConverter.GetBytes(fileSize);
                Array.Reverse(valueBytes);
                bw.Write(valueBytes);

                valueBytes = BitConverter.GetBytes(headerSize);
                Array.Reverse(valueBytes);
                bw.Write(valueBytes);
                bw.Write(Encoding.ASCII.GetBytes(Path.GetFileName(filesToImport[i])));

                bw.BaseStream.Position = offset + headerSize;

                bw.Write(br.ReadBytes((int)br.BaseStream.Length));

            }


            bw.Close();
        }


        class ArchiveFileEntry
        {
            private uint length = 0;
            private uint headersize = 0;
            private uint filesize = 0;

            public uint Offset = 0;
            public uint Length
            {
                get { return length; }
                set
                {
                    length = FlipEndians(value);
                }
            }
            public uint HeaderSize
            {
                get { return headersize; }
                set
                {
                    headersize = FlipEndians(value);
                }
            }
            public uint FileSize
            {
                get { return filesize; }
                set
                {
                    filesize = FlipEndians(value);
                }
            }

            public string FileName = "";

            uint FlipEndians(uint value)
            {
                byte[] valueBytes = BitConverter.GetBytes(value);
                Array.Reverse(valueBytes);
                return BitConverter.ToUInt32(valueBytes, 0);
            }
        }


    }
}
