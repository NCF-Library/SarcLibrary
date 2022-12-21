using SarcLibrary;

namespace Tests
{
    [TestClass]
    public class Read
    {
        [TestMethod]
        [DataRow("D:\\Botw\\Cemu (Stable)\\mlc01\\usr\\title\\0005000e\\101c9500\\content\\Pack\\TitleBG.pack")]
        [DataRow("D:\\Botw\\Files\\01007EF00011E000\\romfs\\Pack\\TitleBG.pack")]
        public void ReadSarc(string sarc)
        {
            using FileStream fs = File.OpenRead(sarc);
            SarcFile sarcBase = new(fs);

            foreach ((var file, var _) in sarcBase) {
                Console.WriteLine(file);
            }
        }
    }
}
