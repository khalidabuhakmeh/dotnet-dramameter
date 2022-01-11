using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static System.StringComparer;
using static System.TimeSpan;
using static Microsoft.AspNetCore.Http.Results;
using static SixLabors.ImageSharp.Color;
using static SixLabors.ImageSharp.Image;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var background = Load("./assets/bg.png");
var foreground = Load("./assets/foreground.png");
var needle = Load("./assets/needle.png");
var levels = new Dictionary<string, (Image label, Color color)>(OrdinalIgnoreCase)
{
    {"low", (Load("./assets/low.png"), LightGreen)},
    {"moderate", (Load("./assets/moderate.png"), Yellow)},
    {"high", (Load("./assets/high.png"), DarkOrange)},
    {"extreme", (Load("./assets/extreme.png"), Red)}
};

app.MapGet("/", async (HttpContext http, int? severity) =>
{
    var rotation = Math.Clamp(severity ?? 0, 0, 180);

    var rotatedNeedle = needle.CloneAs<Rgba32>();
    rotatedNeedle.Mutate(ctx =>
    {
        ctx.Transform(new AffineTransformBuilder()
            .AppendRotationDegrees(rotation)
        );
        ctx.Pad(background.Width, background.Height, Transparent);
    });

    // find the severity based on ranges
    var result = rotation switch
    {
        <= 180 and > 135 => levels["extreme"],
        <= 135 and > 90 => levels["high"],
        <= 90 and > 45 => levels["moderate"],
        _ => levels["low"]
    };

    var image = background.CloneAs<Rgba32>();
    image.Mutate(ctx =>
    {
        ctx.Vignette(result.color); // match the background to the intensity
        ctx.DrawImage(foreground, new Point(0, 0), 1f);
        ctx.DrawImage(rotatedNeedle, new Point(5, 170), opacity: 1f);
        ctx.DrawImage(result.label, new Point(0, 20), opacity: 1f);
    });

    var memoryStream = new MemoryStream();
    await image.SaveAsync(memoryStream, PngFormat.Instance);
    await memoryStream.FlushAsync();
    memoryStream.Position = 0;

    http.Response.Headers.CacheControl = $"public,max-age={FromHours(24).TotalSeconds}";
    return Stream(memoryStream, "image/png");
});

app.Run();