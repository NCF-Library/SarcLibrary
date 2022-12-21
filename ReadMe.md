# SarcLibrary

Nintendo **S**EAD **Arc**hive Library | Based on [oead/sarc](https://github.com/zeldamods/oead/blob/master/src/sarc.cpp) by [Léo Lam](https://github.com/leoetlino)

SarcLibrary has been tested to parse and re-write ***([almost](#Byte-Perfect-Exceptions))*** every SARC file found in Breath of the Wild byte-perfectly.

## Usage

### Reading a Sarc File

```cs
// Read from File Path
SarcFile sarc = SarcFile.FromBinary("content/Pack/Bootup.pack");
```

```cs
// Read from a byte[]
// Do not use with File.ReadAllBytes(), use
// a Stream instead.
SarcFile sarc = SarcFile.FromBinary(data);
```

```cs
// Read from a Stream
using FileStream fs = File.OpenRead("content/Pack/Bootup.pack");
SarcFile sarc = new(fs);
```

### Writing a Sarc File

```cs
// Write to a File
sarc.ToBinary("content/Pack/Bootup.pack");
```

```cs
// Write to a byte[]
// Not advised unless the byte[] is required, consider
// using a MemoryStream with the next example instead.
byte[] data = sarc.ToBinary();
```

```cs
// Write to a Stream
using MemoryStream ms = new();
sarc.ToBinary(ms);
```

### Sarc Tools

```cs
// Extract to a Directory
await sarc.ExtractTodirectory("path/to/output");
```

```cs
// Load from a Directory
SarcFile sarc = SarcFile.LoadFromDirectory("path/to/input", searchPattern: "*.*", searchOption: SearchOption.AllDirectories)
```

## Benchmarks

| Function                              |  Elapsed  |  Allocated |
|:--------------------------------------|:---------:|:----------:|
| Read TitleBG (75MB, BigEndian)        |  29.16 ms |  75,307 KB |
| Read TitleBG (143MB. LittleEndian)    |  51.02 ms | 143,854 KB |
|                                       |           |            |
| Write TitleBG (75MB, BigEndian)       |  40.90 ms |   110 KB   |
| Write TitleBG (143MB, LittleEndian)   | 127.49 ms |   110 KB   |
|                                       |           |            |
| Extract TitleBG (75MB, BigEndian)     | 133.18 ms |   732 KB   |
| Extract TitleBG (143MB, LittleEndian) | 170.64 ms |   726 KB   |
|                                       |           |            |
| Load from Folder (409 Files, ~73MB)   | 40.61 ms  | 73,365 KB  |
| Load from Folder (409 Files, ~140MB)  | 68.11 ms  | 143,910 KB |

_Benchmarks run on an [`AMD Ryzen 7 3700X`](https://www.amd.com/en/products/cpu/amd-ryzen-7-3700x) CPU, 64GB (4 x 16GB) of [`VENGEANCE® DDR4 DRAM 3200MHz C16`](https://www.corsair.com/us/en/Categories/Products/Memory/Vengeance-PRO-RGB-Black/p/CMW32GX4M2E3200C16) memory, and a [`Kingston KC3000 PCIe 4.0 NVMe M.2`](https://www.kingston.com/en/ssd/kc3000-nvme-m2-solid-state-drive) SSD_

## Byte-Perfect Exceptions

Exceptions to every file being parsed and re-written byte-perfectly are a the sbeventpack archives in `content/Event`, which seems to use a mix of alignments. During tests, I found offsets of 0x100 and 0x8 that had varying reliability.<br>
These odd occurrences are also ignored in `oead/sarc`. Luckily, the game does not seem to care if these files are aligned to 4-bytes, so the error is not detrimental (at least not in BOTW).

## Credit

- **[ArchLeaders](https://github.com/ArchLeaders)**: Optimized C# SARC implementation based on oead by [Léo Lam](https://github.com/leoetlino)
- **[exelix](https://github.com/exelix11)**: Original C# SARC implementation
- **[Léo Lam](https://github.com/leoetlino)**: C++ SARC implementation
