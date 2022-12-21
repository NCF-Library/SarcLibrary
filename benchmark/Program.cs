#if DEBUG

using SarcLibrary;

SarcBenchmark sb = new();
Dictionary<string, BenchmarkData> benchmarkData = new();

await sb.Setup();

foreach (var func in sb.GetType().GetMethods().Where(x => x.GetCustomAttributes<BenchmarkAttribute>().Any())) {
    benchmarkData.Add(func.Name, new BenchmarkData(new(), new()));
    for (int i = 0; i < 5; i++) {
        Stopwatch watch = Stopwatch.StartNew();
        func.Invoke(sb, Array.Empty<object>());
        watch.Stop();
        benchmarkData[func.Name].Ticks.Add(watch.ElapsedTicks);
        benchmarkData[func.Name].Milliseconds.Add(watch.ElapsedMilliseconds);
    }
}

foreach ((var name, var data) in benchmarkData) {
    Console.WriteLine($"{name}:");
    Console.WriteLine($"Ticks: {data.Ticks.Min()}:{data.Ticks.Max()} | {string.Join(", ", data.Ticks)}");
    Console.WriteLine($"Milliseconds: {data.Milliseconds.Min()}:{data.Milliseconds.Max()} | {string.Join(", ", data.Milliseconds)}");
    Console.WriteLine();
}

#else

BenchmarkRunner.Run<SarcBenchmark>();

#endif