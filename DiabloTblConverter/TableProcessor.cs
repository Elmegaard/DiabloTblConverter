using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

// Code taken from: https://github.com/kambala-decapitator/QTblEditor
public class TableProcessor
{
    [StructLayout(LayoutKind.Explicit)]
    private struct TblHeader
    {
        [FieldOffset(0x00)]
        public ushort CRC;             // +0x00 - CRC value for string table

        [FieldOffset(0x02)]
        public ushort NodesNumber;     // +0x02 - size of Indices array

        [FieldOffset(0x04)]
        public uint HashTableSize;   // +0x04 - size of TblHashNode array

        [FieldOffset(0x08)]
        public byte Version;         // +0x08 - file version, either 0 or 1, doesn't matter

        [FieldOffset(0x09)]
        public uint DataStartOffset; // +0x09 - string table start offset

        [FieldOffset(0x0D)]
        public uint HashMaxTries;    // +0x0D - max number of collisions for string key search based on its hash value

        [FieldOffset(0x011)]
        public uint FileSize;        // +0x11 - size of the file

        public const int size = 0x15;
    };

    [StructLayout(LayoutKind.Explicit)]
    private struct TblHashNode // node of the hash table in string *.tbl file
    {
        [FieldOffset(0x00)]
        public byte Active;          // +0x00 - shows if the entry is used. if 0, then it has been "deleted" from the table

        [FieldOffset(0x01)]
        public ushort Index;           // +0x01 - index in Indices array

        [FieldOffset(0x03)]
        public uint HashValue;       // +0x03 - hash value of the current string key

        [FieldOffset(0x07)]
        public uint StringKeyOffset; // +0x07 - offset of the current string key

        [FieldOffset(0x0B)]
        public uint StringValOffset; // +0x0B - offset of the current string value

        [FieldOffset(0x0F)]
        public ushort StringValLength; // +0x0F - length of the current string value

        public const int size = 0x11;
    };

    public struct TableList
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public ushort Index { get; set; }

        public override string ToString()
        {
            return Key;
        }
    };

    class TableEntryData
    {
        private static Encoding _enc1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252);
        public string Key { get; set; }
        public byte[] KeyBytes => Encoding.Convert(Encoding.UTF8, _enc1252, Encoding.UTF8.GetBytes(Key));
        public string Value { get; set; }
        public byte[] ValueBytes => Encoding.UTF8.GetBytes(Value);
        public TblHashNode HashNode { get; set; }
        public ushort HashIndex { get; set; }
    }

    public static ushort getCRC(byte[] data)
    {
        ushort[] CRCTable = {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7, 0x8108, 0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF,
        0x1231, 0x0210, 0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6, 0x9339, 0x8318, 0xB37B, 0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE,
        0x2462, 0x3443, 0x0420, 0x1401, 0x64E6, 0x74C7, 0x44A4, 0x5485, 0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE, 0xF5CF, 0xC5AC, 0xD58D,
        0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6, 0x5695, 0x46B4, 0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D, 0xC7BC,
        0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823, 0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B,
        0x5AF5, 0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12, 0xDBFD, 0xCBDC, 0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A,
        0x6CA6, 0x7C87, 0x4CE4, 0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41, 0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD, 0xAD2A, 0xBD0B, 0x8D68, 0x9D49,
        0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13, 0x2E32, 0x1E51, 0x0E70, 0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A, 0x9F59, 0x8F78,
        0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E, 0xE16F, 0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
        0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E, 0x02B1, 0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256,
        0xB5EA, 0xA5CB, 0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D, 0x34E2, 0x24C3, 0x14A0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
        0xA7DB, 0xB7FA, 0x8799, 0x97B8, 0xE75F, 0xF77E, 0xC71D, 0xD73C, 0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657, 0x7676, 0x4615, 0x5634,
        0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9, 0xB98A, 0xA9AB, 0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882, 0x28A3,
        0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A, 0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92,
        0xFD2E, 0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9, 0x7C26, 0x6C07, 0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1,
        0xEF1F, 0xFF3E, 0xCF5D, 0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8, 0x6E17, 0x7E36, 0x4E55, 0x5E74, 0x2E93, 0x3EB2, 0x0ED1, 0x1EF0
        };

        ushort CRCValue = 0xFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            byte charvalue = (byte)(data[i] ^ (CRCValue >> 8));
            ushort temp = (ushort)((CRCValue & 0xFF) << 8);
            CRCValue = (ushort)(CRCTable[charvalue] ^ temp);
        }
        return CRCValue;
    }

    public static uint getHashValue(byte[] key, uint hashTableSize)
    {
        char currentChar;
        //char[] keyCharArray = key.ToCharArray();
        uint hashValue = 0;
        foreach (char c in key)
        {
            currentChar = c;
            hashValue *= 0x10;
            hashValue += currentChar;
            if ((hashValue & 0xF0000000) != 0)
            {
                uint tempValue = hashValue & 0xF0000000;
                tempValue /= 0x01000000;
                hashValue &= 0x0FFFFFFF;
                hashValue ^= tempValue;
            }
        }
        return hashValue % (uint)hashTableSize;
    }

    private static void WriteHeader(BinaryWriter writer, TblHeader header)
    {
        byte[] headerBytes = new byte[TblHeader.size];
        IntPtr ptr = Marshal.AllocHGlobal(TblHeader.size);
        Marshal.StructureToPtr(header, ptr, true);
        Marshal.Copy(ptr, headerBytes, 0, TblHeader.size);
        Marshal.FreeHGlobal(ptr);
        writer.BaseStream.Position = 0;
        writer.Write(headerBytes, 0, TblHeader.size);
        writer.BaseStream.Position = writer.BaseStream.Length;
    }


    public static void WriteTablesFile(string outputPath, string json)
    {
        var jsonList = JsonConvert.DeserializeObject<List<TableList>>(json);
        if (jsonList == null)
        {
            throw new Exception("Unable to read json file to dictionary");
        }

        uint colorHeaderSize = 2;
        uint[] colorBytes = { 0x00FF, 0x0063 };
        string colorHeader = new string(colorBytes.Select(b => (char)b).ToArray(), 0, (int)colorHeaderSize);

        ushort entriesCount = (ushort)jsonList.Count;

        int dataStartOffset = TblHeader.size + entriesCount * sizeof(ushort) + entriesCount * TblHashNode.size;

        var collisionsDetected = new bool[entriesCount];

        var currentOffset = (uint)dataStartOffset;
        var maxCollisionsNumber = 0;

        var nodes = new List<TableEntryData>();

        ushort i = 0;
        foreach (TableList entry in jsonList)
        {
            var tableEntry = new TableEntryData()
            {
                Key = entry.Key,
                Value = entry.Value
            };


            var hashValue = getHashValue(tableEntry.KeyBytes, entriesCount);
            var hashIndex = hashValue;

            var currentCollisionsNumber = 0;
            while (collisionsDetected[hashIndex]) // counting collisions for current hash value
            {
                currentCollisionsNumber++;
                hashIndex++;
                hashIndex %= entriesCount;
            }
            collisionsDetected[hashIndex] = true;
            if (currentCollisionsNumber > maxCollisionsNumber)
            {
                maxCollisionsNumber = currentCollisionsNumber;
            }

            var currentKeyLength = (uint)tableEntry.KeyBytes.Length + 1;
            var currentValLength = (uint)tableEntry.ValueBytes.Length + 1;

            tableEntry.HashNode = new TblHashNode { Active = 1, Index = i, HashValue = hashValue, StringKeyOffset = currentOffset, StringValOffset = currentOffset + currentKeyLength, StringValLength = (ushort)currentValLength };
            tableEntry.HashIndex = (ushort)hashIndex;

            nodes.Add(tableEntry);
            currentOffset += currentKeyLength + currentValLength;
            i++;
        }

        using (var tblFile = new FileStream(outputPath, FileMode.Create))
        {
            using (var writer = new BinaryWriter(tblFile))
            {
                var header = new TblHeader();

                // Write empty header to the beginning
                WriteHeader(writer, header);

                // Write hash indexes
                foreach (var entry in nodes)
                {
                    writer.Write(entry.HashIndex);
                }

                // Write NodeHeaders in the order of the hash indexes
                foreach (var entry in nodes.OrderBy(x => x.HashIndex))
                {
                    writer.Write(entry.HashNode.Active);
                    writer.Write(entry.HashNode.Index);
                    writer.Write(entry.HashNode.HashValue);
                    writer.Write(entry.HashNode.StringKeyOffset);
                    writer.Write(entry.HashNode.StringValOffset);
                    writer.Write(entry.HashNode.StringValLength);
                }

                // Write actual key/val pair and add null termination
                foreach (var entry in nodes)
                {
                    writer.Write(entry.KeyBytes);
                    writer.Write((byte)0);
                    writer.Write(entry.ValueBytes);
                    writer.Write((byte)0);
                }

                var fileSize = writer.BaseStream.Length;

                // Get CRC bytes
                byte[] dataToCRC = new byte[fileSize - dataStartOffset];
                writer.BaseStream.Position = dataStartOffset;
                writer.BaseStream.Read(dataToCRC, 0, dataToCRC.Length);
                writer.BaseStream.Position = 0;

                header = new TblHeader
                {
                    CRC = getCRC(dataToCRC),
                    NodesNumber = entriesCount,
                    HashTableSize = entriesCount,
                    Version = 1,
                    DataStartOffset = (uint)dataStartOffset,
                    HashMaxTries = (uint)maxCollisionsNumber + 1,
                    FileSize = (uint)fileSize
                };
                WriteHeader(writer, header);
            }
        }
    }

    public static List<KeyValuePair<string, string>> ReadTablesFile(string path)
    {
        var result = new List<KeyValuePair<string, string>>();

        using (var fs = new FileStream(path, FileMode.Open))
        {
            using (var br = new BinaryReader(fs, Encoding.UTF8))
            {
                // Read the header
                var header = GetHeader(br);

                // number of bytes to read without header
                var numElem = header.FileSize - TblHeader.size;

                // Check we can read the entire file
                var byteArray = br.ReadBytes((int)numElem);
                if (byteArray.Length == numElem)
                {
                    br.BaseStream.Position = TblHeader.size;

                    // Read the table
                    result = GetStringTable(br, header);
                }
                else
                {
                    throw new Exception($"Table '{path}' seems to be corrupt");
                }
            }
        }

        return result;
    }

    private static List<KeyValuePair<string, string>> GetStringTable(BinaryReader br, TblHeader header)
    {
        var result = new List<KeyValuePair<string, string>>();
        var tableList = new List<TableList>();

        br.BaseStream.Position += header.NodesNumber * sizeof(ushort);
        var hashNodes = new List<TblHashNode>();

        for (uint i = 0; i < header.HashTableSize; i++)
        {
            hashNodes.Add(GetHashNode(br));
        }

        br.BaseStream.Position = 0;

        var byteArray = ReadAllBytes(br);
        foreach (var hashNode in hashNodes)
        {

            if (hashNode.Active == 0)
            {
                continue;
            }
            else if (hashNode.Active != 1)
            {
                continue;
            }

            string val = null;
            string key;

            val = Encoding.UTF8.GetString(byteArray, (int)hashNode.StringValOffset, hashNode.StringValLength).Trim('\0');
            key = Encoding.UTF8.GetString(byteArray, (int)hashNode.StringKeyOffset, (int)hashNode.StringValOffset - (int)hashNode.StringKeyOffset).Trim('\0');

            tableList.Add(new TableList { Key = key, Value = val ?? "", Index = hashNode.Index });
        }

        foreach (var tableValue in tableList.OrderBy(x => x.Index))
        {
            result.Add(new KeyValuePair<string, string>(tableValue.Key, tableValue.Value));
        }

        return result;
    }

    private static TblHeader GetHeader(BinaryReader br)
    {
        byte[] readBuffer = new byte[TblHeader.size];

        readBuffer = br.ReadBytes(TblHeader.size);
        GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
        var header = (TblHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(TblHeader));
        handle.Free();

        return header;
    }

    private static TblHashNode GetHashNode(BinaryReader br)
    {
        byte[] readBuffer = new byte[TblHashNode.size];

        readBuffer = br.ReadBytes(TblHashNode.size);
        GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
        var result = (TblHashNode)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(TblHashNode));
        handle.Free();

        return result;
    }

    private static byte[] ReadAllBytes(BinaryReader reader)
    {
        const int bufferSize = 4096;
        using (var ms = new MemoryStream())
        {
            byte[] buffer = new byte[bufferSize];
            int count;
            while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
            {
                ms.Write(buffer, 0, count);
            }
            return ms.ToArray();
        }
    }
}
