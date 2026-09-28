using MiningOverlay.Core.Calibration;
using MiningOverlay.Core.Ocr;
using SkiaSharp;

if (args.Length == 0 || args[0] != "calibrate")
{
    PrintUsage();
    return 1;
}

var options = ParseOptions(args.AsSpan(1).ToArray());
if (options is null)
{
    PrintUsage();
    return 1;
}

var imagePaths = options.BatchDir is not null
    ? Directory.EnumerateFiles(options.BatchDir, "*.*")
        .Where(IsImage)
        .OrderBy(p => p, StringComparer.Ordinal)
        .ToList()
    : new List<string> { options.ImagePath! };

if (imagePaths.Count == 0)
{
    Console.Error.WriteLine($"No image files found in {options.BatchDir}");
    return 1;
}

int confident = 0;
foreach (var path in imagePaths)
{
    if (RunOne(path, options))
        confident++;
    Console.WriteLine();
}

if (imagePaths.Count > 1)
    Console.WriteLine($"{confident}/{imagePaths.Count} images read confidently.");

if (options.Manual is { } manual)
    PrintProfileSnippet(options.Ship, imagePaths[0], manual);

return 0;

static bool RunOne(string path, CliOptions o)
{
    using var full = SKBitmap.Decode(path);
    if (full is null)
    {
        Console.WriteLine($"{Path.GetFileName(path)}: could not decode image, skipping.");
        return false;
    }

    var resolution = RegionResolver.Resolve(o.Manual, o.Ship, full.Width, full.Height);
    if (!resolution.Ok)
        Console.WriteLine($"  ! {resolution.Description}");

    var r = resolution.Region;
    var px = new SKRectI(
        (int)(full.Width * r.X),
        (int)(full.Height * r.Y),
        (int)(full.Width * (r.X + r.W)),
        (int)(full.Height * (r.Y + r.H)));

    using var cropped = new SKBitmap(new SKImageInfo(px.Width, px.Height));
    using (var canvas = new SKCanvas(cropped))
        canvas.DrawBitmap(full, px, new SKRect(0, 0, px.Width, px.Height), new SKSamplingOptions());

    if (o.DumpPreprocessedDir is not null)
    {
        Directory.CreateDirectory(o.DumpPreprocessedDir);
        using var preprocessed = ImagePreprocessor.Preprocess(cropped);
        var dumpPath = Path.Combine(o.DumpPreprocessedDir, Path.GetFileNameWithoutExtension(path) + ".png");
        using var fs = File.OpenWrite(dumpPath);
        using var data = preprocessed.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(fs);
    }

    var result = RsOcrReader.Read(cropped, o.Mode);

    Console.WriteLine($"{Path.GetFileName(path)}  [{full.Width}x{full.Height}]");
    Console.WriteLine($"  Region: x={r.X:0.###} y={r.Y:0.###} w={r.W:0.###} h={r.H:0.###}  ({resolution.Description})");
    Console.WriteLine($"  Digits: '{result.Digits}'");

    if (result.Rs is int rs)
    {
        Console.WriteLine($"  RS: {rs:N0}");
        foreach (var m in result.Matches.Take(3))
            Console.WriteLine($"    [{m.Resource.Tier}] {m.Resource.Name,-15} {m.Nodes}x  {m.Label,-6}  {m.Confidence}%");
        return true;
    }

    Console.WriteLine("  No confident match — adjust --x/--y/--w/--h and try again.");
    return false;
}

static void PrintProfileSnippet(string ship, string sampleImagePath, Region region)
{
    using var img = SKBitmap.Decode(sampleImagePath);
    var resolution = img is null ? "?x?" : $"{img.Width}x{img.Height}";
    Console.WriteLine("Profile entry for ShipProfiles.cs:");
    Console.WriteLine($"    [(\"{ship}\", \"{resolution}\")] = new Region({region.X}, {region.Y}, {region.W}, {region.H}),");
}

static bool IsImage(string path) =>
    Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp";

static CliOptions? ParseOptions(string[] args)
{
    string? image = null, batch = null, ship = null, mode = "ship", dumpDir = null;
    double? x = null, y = null, w = null, h = null;

    for (int i = 0; i < args.Length; i++)
    {
        string Next() => i + 1 < args.Length ? args[++i] : throw new IndexOutOfRangeException();
        try
        {
            switch (args[i])
            {
                case "--image": image = Next(); break;
                case "--batch": batch = Next(); break;
                case "--ship": ship = Next(); break;
                case "--mode": mode = Next(); break;
                case "--x": x = double.Parse(Next()); break;
                case "--y": y = double.Parse(Next()); break;
                case "--w": w = double.Parse(Next()); break;
                case "--h": h = double.Parse(Next()); break;
                case "--dump-preprocessed": dumpDir = Next(); break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return null;
            }
        }
        catch (IndexOutOfRangeException)
        {
            Console.Error.WriteLine($"Missing value for {args[i]}");
            return null;
        }
    }

    if (image is null && batch is null)
    {
        Console.Error.WriteLine("Need --image <file> or --batch <dir>.");
        return null;
    }

    var provided = new[] { x, y, w, h };
    if (provided.Any(v => v is not null) && provided.Any(v => v is null))
    {
        Console.Error.WriteLine("--x/--y/--w/--h must all be given together, or none at all.");
        return null;
    }

    Region? manual = x is not null ? new Region(x.Value, y!.Value, w!.Value, h!.Value) : null;
    return new CliOptions(image, batch, ship ?? "golem", manual, mode ?? "ship", dumpDir);
}

static void PrintUsage()
{
    Console.Error.WriteLine("""
        miningoverlay-cli calibrate --image <path> [--ship <name>] [--x F --y F --w F --h F] [--mode ship|fps|ground] [--dump-preprocessed <dir>]
        miningoverlay-cli calibrate --batch <dir>  [--ship <name>] [--x F --y F --w F --h F] [--mode ship|fps|ground] [--dump-preprocessed <dir>]

        Runs a screenshot (or every image in a directory) through the exact same OCR
        pipeline the overlay uses, so you can find a working capture region without
        needing the game running. x/y/w/h are fractions (0.0-1.0) of the screenshot's
        own dimensions. Once it reads confidently, a ready-to-paste ShipProfiles.cs
        entry is printed.

        --dump-preprocessed saves what Tesseract actually sees (post crop/threshold/
        upscale) as a PNG per image — useful for spotting misread digits by eye.
        """);
}

internal sealed record CliOptions(string? ImagePath, string? BatchDir, string Ship, Region? Manual, string Mode, string? DumpPreprocessedDir);
