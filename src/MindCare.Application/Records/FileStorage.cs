namespace MindCare.Application.Records;

public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Pasta onde os documentos dos pacientes são gravados (volume persistente no Docker).</summary>
    public string Path { get; set; } = "App_Data/storage";

    /// <summary>Tamanho máximo por arquivo, em megabytes.</summary>
    public int MaxUploadMegabytes { get; set; } = 10;
}

/// <summary>Armazenamento dos arquivos anexados. A implementação padrão usa o disco local.</summary>
public interface IFileStorage
{
    Task SaveAsync(string relativePath, Stream content, CancellationToken ct = default);
    Stream OpenRead(string relativePath);
    void Delete(string relativePath);
}

internal sealed class LocalFileStorage(Microsoft.Extensions.Options.IOptions<StorageOptions> options) : IFileStorage
{
    public async Task SaveAsync(string relativePath, Stream content, CancellationToken ct = default)
    {
        var path = Resolve(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, ct);
    }

    public Stream OpenRead(string relativePath) =>
        new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

    public void Delete(string relativePath)
    {
        var path = Resolve(relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>Impede caminhos fora da pasta de armazenamento (ex.: "../").</summary>
    private string Resolve(string relativePath)
    {
        var root = System.IO.Path.GetFullPath(options.Value.Path);
        var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relativePath));
        if (!full.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Caminho de arquivo inválido.");
        }

        return full;
    }
}
