using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PhotoReducer;

public class ResizeImageFunction
{
    private readonly ILogger<ResizeImageFunction> _logger;

    public ResizeImageFunction(ILogger<ResizeImageFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ResizeImageFunction))]
    [BlobOutput("thumbnails/{name}", Connection = "PhotoStorage")]
    public async Task<byte[]> Run(
     [BlobTrigger("photos/{name}", Connection = "PhotoStorage")] byte[] blob,
     string name)
    {
        try
        {
            _logger.LogInformation("Processing blob: {name}", name);

            using var input = new MemoryStream(blob);
            using var output = new MemoryStream();

            await ResizeAsync(input, output);

            var result = output.ToArray();

            _logger.LogInformation(
                "Resize completed for {name}. Input size: {input} bytes, Output size: {output} bytes",
                name, blob.Length, result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing blob: {name}", name);
            throw;
        }
    }

    private async Task ResizeAsync(Stream inputStream, Stream outputStream)
    {
        using Image image = await Image.LoadAsync(inputStream);
        image.Mutate(x => x.Resize(30, 0));
        await image.SaveAsync(outputStream, JpegFormat.Instance);
    }
}