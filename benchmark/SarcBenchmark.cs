#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using BenchmarkDotNet.Attributes;
using SarcLibrary;

namespace Benchmark
{
    public record BenchmarkData(List<long> Ticks, List<long> Milliseconds);

    [MemoryDiagnoser]
    public class SarcBenchmark
    {
        private readonly string _sarcBePath = "F:\\Bin\\wiiu\\TitleBG.pack";
        private readonly string _sarcLePath = "F:\\Bin\\nx\\TitleBG.pack";

        private SarcFile _sarcBe;
        private SarcFile _sarcLe;

        [GlobalSetup]
        public async Task Setup()
        {
            using (FileStream fs = File.OpenRead(_sarcBePath)) {
                _sarcBe = new(fs);
                await _sarcBe.ExtractToDirectory($"{_sarcBePath}.setup.ex");
            }

            using (FileStream fs = File.OpenRead(_sarcLePath)) {
                _sarcLe = new(fs);
                await _sarcLe.ExtractToDirectory($"{_sarcLePath}.setup.ex");
            }
        }

        [Benchmark]
        public void SarcReadBE()
        {
            SarcFile sarc = SarcFile.FromBinary(_sarcBePath);
        }

        [Benchmark]
        public void SarcWriteBE()
        {
            _sarcBe.ToBinary($"{_sarcBePath}.pack");
        }

        [Benchmark]
        public async Task SarcExtractBE()
        {
            await _sarcBe.ExtractToDirectory($"{_sarcBePath}.ex");
        }

        [Benchmark]
        public void SarcLoadBE()
        {
            SarcFile sarc = SarcFile.LoadFromDirectory($"{_sarcBePath}.setup.ex");
        }

        [Benchmark]
        public void SarcReadLE()
        {
            SarcFile sarc = SarcFile.FromBinary(_sarcLePath);
        }

        [Benchmark]
        public void SarcWriteLE()
        {
            _sarcLe.ToBinary($"{_sarcLePath}.pack");
        }

        [Benchmark]
        public async Task SarcExtractLE()
        {
            await _sarcLe.ExtractToDirectory($"{_sarcLePath}.ex");
        }

        [Benchmark]
        public void SarcLoadLE()
        {
            SarcFile sarc = SarcFile.LoadFromDirectory($"{_sarcLePath}.setup.ex");
        }
    }
}
