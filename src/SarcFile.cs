﻿using System.Text;
using static SarcLibrary.SarcHelper;

namespace SarcLibrary
{
    public enum Endian : ushort { Big = 0xFFFE, Little = 0xFEFF }

    public class SarcFile : Dictionary<string, byte[]>
    {
        public Endian Endian { get; set; }
        public bool HashOnly { get; set; } = false;
        public bool Legacy { get; set; } = false;

        public SarcFile()
        {
            Endian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
        }

        public SarcFile(Stream stream)
        {
            using BinaryReader reader = new(stream);

            Span<byte> sarc = stackalloc byte[4];
            stream.Read(sarc);
            if (!sarc.SequenceEqual(SARC)) {
                throw new InvalidDataException("Invalid SARC magic");
            }

            stream.Seek(2, SeekOrigin.Current);
            Endian = (Endian)reader.ReadUInt16();

            int fileSize = reader.ReadInt32();
            int dataOffset = reader.ReadInt32();
            stream.Seek(10, SeekOrigin.Current);

            ushort count = reader.ReadUInt16();
            stream.Seek(4, SeekOrigin.Current);

            if (Endian == Endian.Big) {
                fileSize.SwapEndian();
                dataOffset.SwapEndian();
                count.SwapEndian();
            }

            Span<(uint, int, int, int)> nodes = stackalloc (uint, int, int, int)[count];

            for (int i = 0; i < count; i++) {
                uint hash = reader.ReadUInt32();
                int attributes = reader.ReadInt32();
                int dataStart = reader.ReadInt32();
                int dataEnd = reader.ReadInt32();

                if (Endian == Endian.Big) {
                    hash.SwapEndian();
                    attributes.SwapEndian();
                    dataStart.SwapEndian();
                    dataEnd.SwapEndian();
                }

                HashOnly = (byte)(attributes >> 24) != 1;
                int strOffset = (attributes & 0xFFFF) * 4;

                nodes[i] = (hash, strOffset, dataStart, dataEnd);
            }

            stream.Seek(8, SeekOrigin.Current);

            if (!HashOnly) {
                Span<byte> stringTableBuffer = reader.ReadBytes((int)(dataOffset - stream.Position)).AsSpan();
                for (int i = 0; i < count; i++) {
                    if (i == count - 1) {
                        Add(Encoding.UTF8.GetString(stringTableBuffer[nodes[i].Item2..]).Replace(Null, Empty),
                           reader.ReadBytes(nodes[i].Item4 - nodes[i].Item3));
                    }
                    else {
                        Add(Encoding.UTF8.GetString(stringTableBuffer[nodes[i].Item2..nodes[i + 1].Item2]).Replace(Null, Empty),
                           reader.ReadBytes(nodes[i].Item4 - nodes[i].Item3));
                        stream.Seek(nodes[i + 1].Item3 - nodes[i].Item4, SeekOrigin.Current);
                    }
                }
            }
            else {
                stream.Seek(dataOffset, SeekOrigin.Begin);
                for (int i = 0; i < count; i++) {
                    byte[] buffer = reader.ReadBytes(nodes[i].Item4 - nodes[i].Item3);
                    Add($"{nodes[i].Item1:x8}.{GuessFileExtension(buffer)}", buffer);

                    if (i != count - 1) {
                        stream.Seek(nodes[i + 1].Item3 - nodes[i].Item4, SeekOrigin.Current);
                    }
                }
            }
        }

        public static SarcFile FromBinary(byte[] data)
        {
            using MemoryStream ms = new(data);
            return new(ms);
        }

        public static SarcFile FromBinary(string sarcFile)
        {
            using FileStream fs = File.OpenRead(sarcFile);
            return new(fs);
        }

        public static SarcFile FromBinary(Stream stream)
        {
            return new(stream);
        }

        public byte[] ToBinary()
        {
            using MemoryStream ms = new();
            ToBinary(ms);
            return ms.ToArray();
        }

        public void ToBinary(string outputFile)
        {
            using FileStream fs = File.Create(outputFile);
            ToBinary(fs);
        }

        public void ToBinary(Stream stream)
        {
            using BinaryWriter writer = new(stream);

            // Allocate sorted keys/values
            string[] fileNames = Keys.ToArray();
            uint[] keys = new uint[Count];
            (string, int, int, int, int)[] nodes = new (string, int, int, int, int)[Count];

            int relStringOffset = 0;
            int relDataOffset = 0;
            int fileAlignment = 1;
            for (int i = 0; i < Count; i++) {
                string fileName = fileNames[i];
                keys[i] = GetHash(fileName);

                // Calculate data and string offsets
                Span<byte> buffer = this[fileName].AsSpan();
                int alignment = GetFileAlignment(fileName, buffer, this);
                int dataStart = relDataOffset.Align(alignment);
                int dataEnd = dataStart + buffer.Length;
                nodes[i] = (fileName, dataStart, dataEnd, relStringOffset / 4, alignment);

                relDataOffset = dataEnd;
                relStringOffset += fileName.Length + 4 & -4;
                fileAlignment = LCM(fileAlignment, alignment);
            }

            Array.Sort(keys, nodes);

            stream.Seek(0x14, SeekOrigin.Begin);

            // Write data nodes (SFAT)
            writer.Write(SFAT);
            writer.Write(0x0C.AsInt16(Endian));
            writer.Write(Count.AsInt16(Endian));
            writer.Write(HashKey.AsUInt32(Endian));

            for (int i = 0; i < Count; i++) {
                writer.Write(keys[i].AsUInt32(Endian));
                writer.Write(HashOnly ? 0x00 : (0x01000000 | nodes[i].Item4).AsInt32(Endian));
                writer.Write(nodes[i].Item2.AsInt32(Endian));
                writer.Write(nodes[i].Item3.AsInt32(Endian));
            }

            // Write string table (SFNT)
            writer.Write(SFNT);
            writer.Write(0x08.AsInt16(Endian));
            writer.Write((ushort)0x00);

            for (int i = 0; i < Count; i++) {
                byte[] buffer = Encoding.UTF8.GetBytes(nodes[i].Item1);
                byte[] aligned = new byte[buffer.Length + 4 & -4];
                Array.Copy(buffer, aligned, buffer.Length);
                writer.Write(aligned);
            }

            // Console.WriteLine(string.Join(", ", nodes.Select(x => x.Item4)));
            stream.Align(fileAlignment);
            int dataOffset = (int)stream.Position;

            // Write data
            for (int i = 0; i < Count; i++) {
                stream.Align(nodes[i].Item5);
                writer.Write(this[nodes[i].Item1]);
            }

            int fileSize = (int)stream.Position;

            stream.Seek(0, SeekOrigin.Begin);
            writer.Write(SARC);
            writer.Write(0x14.AsInt16(Endian));
            writer.Write((ushort)Endian);
            writer.Write(fileSize.AsInt32(Endian));
            writer.Write(dataOffset.AsInt32(Endian)); // Data offset (0x0C)
            writer.Write(0x0100.AsInt16(Endian)); // SARC Version
            writer.Write((ushort)0x00); // Reserved
        }

        public async Task ExtractToDirectory(string outputDirectory)
        {
            if (HashOnly) {
                Directory.CreateDirectory(outputDirectory);
            }

            await Parallel.ForEachAsync(this, async (sarcFile, cancellationToken) => {
                if (!HashOnly) {
                    string file = Path.Combine(outputDirectory, sarcFile.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(file) ?? "");
                    await File.WriteAllBytesAsync(file, sarcFile.Value, cancellationToken);
                }
                else {
                    await File.WriteAllBytesAsync(Path.Combine(outputDirectory, sarcFile.Key), sarcFile.Value, cancellationToken);
                }
            });
        }

        public static SarcFile LoadFromDirectory(string directory, string searchPattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            SarcFile sarc = new();
            foreach (var file in Directory.EnumerateFiles(directory, searchPattern, searchOption)) {
                sarc.Add(Path.GetRelativePath(directory, file), File.ReadAllBytes(file));
            }

            return sarc;
        }
    }
}