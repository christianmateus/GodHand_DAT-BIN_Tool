using System;
using System.IO;
using System.Text;

namespace GodHand_BIN_DAT_Tool
{
    public class DATMain
    {
        public string filepath { get; set; }
        public DATMain(string filename, string command)
        {
            filepath = filename;
            if (command == "extract")
            {
                Extract();
            }
            else
            {
                Repack();
            }
        }
        public void Extract()
        {
            if (!Directory.Exists(Path.GetFileNameWithoutExtension(filepath)))
            {
                Directory.CreateDirectory(Path.GetFileNameWithoutExtension(filepath));
            }

            string folderName = Path.GetFileNameWithoutExtension(filepath);

            BinaryReader br = new BinaryReader(File.OpenRead(filepath));
            StreamWriter txt = new StreamWriter(folderName + ".txt");

            uint fileCount = br.ReadUInt32();
            string[] fileExtensions = new string[fileCount];
            uint[] fileOffsets = new uint[fileCount];
            br.BaseStream.Position = 0x04 + fileCount * 4;
            txt.WriteLine("FileCount = " + fileCount);
            txt.WriteLine(Path.GetExtension(filepath));

            // Get all extensions
            for (int j = 0; j != fileCount; j++)
            {
                byte[] bytes = br.ReadBytes(4);
                byte indexOfZero = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (bytes[i] == 0x00)
                    {
                        indexOfZero = (byte)i;
                        break;
                    }
                }
                bytes = RemoveIndices(bytes, indexOfZero);
                string extension = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                if (extension == "")
                {
                    fileExtensions[j] = "DMY";
                }
                else
                {
                    fileExtensions[j] = extension;
                }
            }

            // Get all offsets
            br.BaseStream.Position = 0x04;
            for (int j = 0; j != fileCount; j++)
            {
                fileOffsets[j] = br.ReadUInt32();
            }

            // Get all files
            for (int i = 0; i != fileCount; i++)
            {
                if (i != fileCount - 1)
                {
                    BinaryWriter bw = new BinaryWriter(File.Create(folderName + "\\" + folderName + $"_{i}.{fileExtensions[i]}"));
                    txt.WriteLine($"File_{i} = " + folderName + "\\" + folderName + $"_{i}.{fileExtensions[i]}");
                    br.BaseStream.Position = fileOffsets[i];
                    byte[] bytes = br.ReadBytes((int)(fileOffsets[i + 1] - fileOffsets[i]));
                    bw.Write(bytes);
                    bw.Close();
                }
                else
                {
                    BinaryWriter bw = new BinaryWriter(File.Create(folderName + "/" + folderName + $"_{i}.{fileExtensions[i]}"));
                    txt.WriteLine($"File_{i} = " + folderName + "\\" + folderName + $"_{i}.{fileExtensions[i]}");
                    br.BaseStream.Position = fileOffsets[i];
                    byte[] bytes = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
                    bw.Write(bytes);
                    bw.Close();
                }
            }
            txt.Close();
            br.Close();
        }

        public void Repack()
        {
            string folderName = Path.GetFileNameWithoutExtension(filepath);

            StreamReader txt = new StreamReader(folderName + ".txt");
            string line = txt.ReadLine();
            int fileCount = Convert.ToInt16(line.Substring(12));
            uint[] fileLength = new uint[fileCount];
            string[] extensions = new string[fileCount];

            // Get extension
            line = txt.ReadLine();
            BinaryWriter bw = new BinaryWriter(File.Create(folderName + line));

            // Add .dat file count 
            bw.Write((uint)fileCount);

            // Create area for offsets and extensions
            for (int offset = 0; offset < fileCount; offset++)
            {
                bw.Write((uint)0x00);
                bw.Write((uint)0x00);
            }

            // Add padding if needed
            for (int i = 0; i < 4; i++)
            {
                if (bw.BaseStream.Position % 16 != 0)
                {
                    bw.Write((uint)0x00);
                }
            }
            uint firstFileOffset = (uint)bw.BaseStream.Position;


            // Iterate through each file in the folder
            for (int i = 0; i < fileCount; i++)
            {
                line = txt.ReadLine();
                string fileName = line.Substring(line.IndexOf(folderName));
                extensions[i] = Path.GetExtension(line.Substring(line.IndexOf(folderName))).Substring(1);

                BinaryReader br = new BinaryReader(File.OpenRead(fileName));
                byte[] fileBytes = br.ReadBytes((int)br.BaseStream.Length);
                fileLength[i] = (uint)fileBytes.Length;
                br.Close();

                bw.Write(fileBytes);
            }

            // Write offsets
            bw.BaseStream.Position = 0x04;
            uint acumulator = firstFileOffset;
            for (int i = 0; i < fileCount; i++)
            {
                bw.Write(acumulator);
                acumulator += fileLength[i];
            }

            // Write extensions
            for (int i = 0; i < fileCount; i++)
            {
                switch (extensions[i].Length)
                {
                    case 0:
                        bw.Write((uint)0x00);
                        break;
                    case 1:
                        bw.Write(extensions[i][0]);
                        bw.Write((byte)0x00);
                        bw.Write((byte)0x00);
                        bw.Write((byte)0x00);
                        break;
                    case 2:
                        bw.Write(extensions[i][0]);
                        bw.Write(extensions[i][1]);
                        bw.Write((byte)0x00);
                        bw.Write((byte)0x00);
                        break;
                    case 3:
                        bw.Write(extensions[i][0]);
                        bw.Write(extensions[i][1]);
                        bw.Write(extensions[i][2]);
                        bw.Write((byte)0x00);
                        break;
                    default:
                        Console.WriteLine($"Extension {extensions[i]} has more than 3 characters...");
                        break;
                }
            }

            txt.Close();
            bw.Close();
        }

        private byte[] RemoveIndices(byte[] oldArray, byte Length)
        {
            byte[] newArray = new byte[Length];

            for (int i = 0; i < Length; i++)
            {
                newArray[i] = oldArray[i];
            }
            return newArray;
        }
    }
}
