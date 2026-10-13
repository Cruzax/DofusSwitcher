using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Télécharge le symbole d'une classe depuis DofusDB quand l'utilisateur n'a pas mis d'image.</summary>
public sealed class ClassImageDownloader
{
    private static readonly HttpClient Http = CreateClient();

    private readonly string _folder;
    private readonly HashSet<string> _attempted = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string>? _symbolUrls;

    public ClassImageDownloader(string folder) => _folder = folder;

    /// <summary>
    /// Télécharge l'image si elle manque. Une seule tentative par classe et par lancement ;
    /// renvoie true si un fichier a été ajouté.
    /// </summary>
    public async Task<bool> EnsureImageAsync(string className)
    {
        var key = ClassStyle.ToFileKey(className);
        if (key.Length == 0 || !_attempted.Add(key)) return false;
        if (ClassImageProvider.FindFile(_folder, key) is not null) return false;

        try
        {
            _symbolUrls ??= BreedCatalog.ParseSymbolUrls(await Http.GetStringAsync(BreedCatalog.BreedsUrl));
            if (!_symbolUrls.TryGetValue(key, out var url)) return false;

            var bytes = await Http.GetByteArrayAsync(url);
            Directory.CreateDirectory(_folder);
            var path = Path.Combine(_folder, key + ".png");
            var tempPath = path + ".tmp";
            await File.WriteAllBytesAsync(tempPath, bytes);
            // L'utilisateur a pu déposer sa propre image pendant le téléchargement : on ne l'écrase pas.
            if (ClassImageProvider.FindFile(_folder, key) is not null)
            {
                File.Delete(tempPath);
                return false;
            }
            File.Move(tempPath, path);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException
                                       or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            // Hors ligne ou API indisponible : on garde la pastille de couleur, nouvel essai au prochain lancement.
            _symbolUrls = null;
            return false;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DofusSwitcher/1.0");
        return client;
    }
}
