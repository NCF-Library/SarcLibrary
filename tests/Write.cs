using SarcLibrary;

namespace Tests
{
    [TestClass]
    public class Write
    {
        [TestMethod]
        [DataRow("D:\\Botw\\Cemu (Stable)\\mlc01\\usr\\title\\0005000e\\101c9500\\content\\Pack\\TitleBG.pack")]
        [DataRow("D:\\Botw\\Files\\01007EF00011E000\\romfs\\Pack\\TitleBG.pack")]
        public void WriteSarc(string sarc)
        {
            SarcFile sarcBase = SarcFile.FromBinary(sarc);

            sarcBase.Endian = Endian.Little;
            sarcBase.ToBinary(sarc + ".sarc");
        }

        [TestMethod]
        [DataRow("D:\\Botw\\Cemu (Stable)\\mlc01\\usr\\title\\0005000e\\101c9500\\content\\Pack\\TitleBG.pack")]
        [DataRow("D:\\Botw\\Files\\01007EF00011E000\\romfs\\Pack\\TitleBG.pack")]
        public void WriteLoadedSarc(string sarc)
        {
            SarcFile sarcBase = SarcFile.LoadFromDirectory(sarc);

            if (sarc.Contains("wiiu")) {
                sarcBase.Endian = Endian.Big;
            }

            sarcBase.ToBinary(sarc + ".sarc");
        }
    }
}
