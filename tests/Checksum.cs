using Nintendo.Yaz0;
using SarcLibrary;

namespace Tests
{
    [TestClass]
    public class Checksum
    {

        [TestMethod]
        [DataRow("nx_base", "D:\\Botw\\Files\\01007EF00011E000\\romfs")]
        [DataRow("nx_dlc", "D:\\Botw\\Files\\01007EF00011F001\\romfs")]
        [DataRow("wiiu_base", "D:\\Botw\\Cemu (Stable)\\mlc01\\usr\\title\\00050000\\101c9500\\content")]
        [DataRow("wiiu_update", "D:\\Botw\\Cemu (Stable)\\mlc01\\usr\\title\\0005000e\\101c9500\\content")]
        [DataRow("wiiu_dlc", "D:\\Botw\\Cemu (Stable)\\mlc01\\usr\\title\\0005000c\\101c9500\\content\\0010")]
        public async Task CheckAllFiles(string key, string root)
        {
            List<string> failed = new();
            string resDir = Directory.CreateDirectory($"D:\\Bin\\SarcLibrary\\{key}\\").FullName;

            await Parallel.ForEachAsync(Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories), async (file, cancellationToken) => {
                byte[] data = await File.ReadAllBytesAsync(file, cancellationToken);

                // Skip eventpacks because they are a known to
                // have unknwon file (bfevfl/bfevtm) alignments
                // See ../ReadMe.md#Byte-Perfect-Exceptions for more info
                if (data.Length > 22 && Path.GetExtension(file) != ".sbeventpack") {
                    if (data.AsSpan()[0x00..0x04].SequenceEqual("Yaz0"u8) && data.AsSpan()[0x11..0x15].SequenceEqual("SARC"u8)) {
                        data = Yaz0.Decompress(data);
                    }
                    else if (!data.AsSpan()[0x00..0x04].SequenceEqual("SARC"u8)) {
                        return;
                    }

                    SarcFile sarc = SarcFile.FromBinary(data);
                    byte[] sarcData = sarc.ToBinary();
                    if (!Enumerable.SequenceEqual(data, sarcData)) {
                        failed.Add($"{file}: {data.Length} did not match {sarcData.Length}.");
                        if (failed.Count <= 25) {
                            File.WriteAllBytes($"{Path.Combine(resDir, Path.GetFileNameWithoutExtension(file))}.failed{Path.GetExtension(file)}", sarcData);
                            File.WriteAllBytes(Path.Combine(resDir, Path.GetFileName(file)), data);
                        }
                        else {
                            throw new Exception();
                        }
                    }
                }
            });

            if (failed.Any()) {
                File.WriteAllLines($"{resDir}\\_fail_list.txt", failed);
                throw new InternalTestFailureException();
            }
        }
    }
}
