using LiteDB;
using LiteDB.AOT;
using System.Diagnostics;

if (args.Contains("--benchmark", StringComparer.Ordinal))
{
    return RunBenchmark(args.Contains("--reflection", StringComparer.Ordinal));
}

var mapper = new BsonMapper()
    .UseGeneratedMappers(new LiteDbGeneratedMapperProvider());

var path = Path.Combine(Path.GetTempPath(), "litedb-aot-smoke-" + Guid.NewGuid().ToString("N") + ".db");

try
{
    using (var database = new LiteDatabase(path, mapper))
    {
        var collection = database.GetCollection<AotPerson>("people");

        collection.Insert(new AotPerson
        {
            Id = 7,
            Name = "Ada",
            Address = new AotAddress
            {
                City = "London"
            },
            Notes = new List<AotNote>
            {
                new AotNote { Text = "first" },
                new AotNote { Text = "second" }
            },
            NoteArray = new[]
            {
                new AotNote { Text = "first" },
                new AotNote { Text = "second" }
            },
            NoteSet = new HashSet<AotNote>
            {
                new AotNote { Text = "first" },
                new AotNote { Text = "second" }
            },
            NoteDict = new Dictionary<string, AotNote>
            {
                ["first"] = new AotNote { Text = "first" },
                ["second"] = new AotNote { Text = "second" }
            },
            ReadOnlyNotes = new List<AotNote>
            {
                new AotNote { Text = "first" },
                new AotNote { Text = "second" }
            }
        });
    }

    using (var database = new LiteDatabase(path, mapper))
    {
        var restored = database.GetCollection<AotPerson>("people").FindAll().FirstOrDefault();
        if (restored == null)
        {
            Console.Error.WriteLine("Native AOT smoke test failed: missing person.");
            return 1;
        }

        if (restored.Id != 7 || restored.Name != "Ada")
        {
            Console.Error.WriteLine("Native AOT smoke test failed: flat fields.");
            return 1;
        }

        if (restored.Address == null || restored.Address.City != "London")
        {
            Console.Error.WriteLine("Native AOT smoke test failed: nested address.");
            return 1;
        }

        if (restored.Notes == null || restored.Notes.Count != 2 || restored.Notes[1].Text != "second")
        {
            Console.Error.WriteLine("Native AOT smoke test failed: list materialization.");
            return 1;
        }

        if (restored.NoteArray == null || restored.NoteArray.Length != 2 || restored.NoteArray[1].Text != "second")
        {
            Console.Error.WriteLine("Native AOT smoke test failed: array materialization.");
            return 1;
        }

        if (restored.NoteSet == null || restored.NoteSet.Count != 2)
        {
            Console.Error.WriteLine("Native AOT smoke test failed: hashset materialization.");
            return 1;
        }

        if (restored.NoteDict == null || restored.NoteDict.Count != 2 || restored.NoteDict["second"].Text != "second")
        {
            Console.Error.WriteLine("Native AOT smoke test failed: dictionary materialization.");
            return 1;
        }

        if (restored.ReadOnlyNotes == null || restored.ReadOnlyNotes.Count != 2 || restored.ReadOnlyNotes[1].Text != "second")
        {
            Console.Error.WriteLine("Native AOT smoke test failed: read-only list materialization.");
            return 1;
        }
    }

    Console.WriteLine("Native AOT smoke test passed.");
    return 0;
}
finally
{
    if (File.Exists(path))
    {
        File.Delete(path);
    }
}

static int RunBenchmark(bool includeReflection)
{
    const int iterations = 10_000;
    var sample = new AotPerson
    {
        Id = 7,
        Name = "Ada",
        Address = new AotAddress { City = "London" },
        Notes = new List<AotNote>
        {
            new AotNote { Text = "first" },
            new AotNote { Text = "second" }
        }
    };

    var generatedMapper = new BsonMapper()
        .UseGeneratedMappers(new LiteDbGeneratedMapperProvider());
    RunMappingBenchmark("generated", generatedMapper, sample, iterations);

    if (includeReflection)
    {
        RunMappingBenchmark("reflection", new BsonMapper(), sample, iterations);
    }
    else
    {
        Console.WriteLine("reflection: skipped (pass --reflection in a managed process)");
    }

    return 0;
}

static void RunMappingBenchmark(string name, BsonMapper mapper, AotPerson sample, int iterations)
{
    var warmup = mapper.ToDocument(sample);
    _ = mapper.Deserialize<AotPerson>(warmup);

    long checksum = 0;
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var stopwatch = Stopwatch.StartNew();

    for (var i = 0; i < iterations; i++)
    {
        var document = mapper.ToDocument(sample);
        var restored = mapper.Deserialize<AotPerson>(document);
        checksum += restored.Id + restored.Notes.Count + document.Count;
    }

    stopwatch.Stop();
    var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    var nanosecondsPerOperation = stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / iterations;

    Console.WriteLine($"{name}: {iterations:N0} round-trips, {stopwatch.Elapsed.TotalMilliseconds:N1} ms, {nanosecondsPerOperation:N0} ns/op, {allocated:N0} bytes, checksum {checksum}");
}

[LiteEntity]
public sealed class AotPerson
{
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public AotAddress Address { get; set; } = new AotAddress();

    public List<AotNote> Notes { get; set; } = new List<AotNote>();

    public AotNote[] NoteArray { get; set; } = Array.Empty<AotNote>();

    public HashSet<AotNote> NoteSet { get; set; } = new HashSet<AotNote>();

    public Dictionary<string, AotNote> NoteDict { get; set; } = new Dictionary<string, AotNote>();

    public IReadOnlyList<AotNote> ReadOnlyNotes { get; set; } = new List<AotNote>();
}

[LiteEntity]
public sealed class AotAddress
{
    public string City { get; set; } = string.Empty;
}

[LiteEntity]
public sealed class AotNote
{
    public string Text { get; set; } = string.Empty;
}
