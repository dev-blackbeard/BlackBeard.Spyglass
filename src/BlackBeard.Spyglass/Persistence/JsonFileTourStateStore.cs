using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text.Json;
using BlackBeard.Spyglass.Diagnostics;

namespace BlackBeard.Spyglass.Persistence;

/// <summary>
/// The default <see cref="ITourStateStore"/>: persists tour preferences as JSON under
/// <c>%LOCALAPPDATA%\{Company}\{Product}\spyglass.tours.json</c>, resolved from the entry assembly
/// unless an explicit path is supplied. Writes are atomic (temp file + move) and never throw; any I/O
/// failure is traced as a warning and the store degrades to holding its state in memory only.
/// </summary>
public sealed class JsonFileTourStateStore : ITourStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly Dictionary<string, bool> _values = new();
    private bool _loaded;
    private bool _dirty;

    /// <summary>
    /// Creates a store at the default location, or at <paramref name="filePath"/> if supplied.
    /// </summary>
    /// <param name="filePath">
    /// An explicit file path to use instead of the default <c>%LOCALAPPDATA%</c> location.
    /// </param>
    public JsonFileTourStateStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath) ? ResolveDefaultFilePath() : filePath!;
    }

    /// <inheritdoc />
    public bool? GetShowEveryTime(string tourKey)
    {
        EnsureLoaded();
        return _values.TryGetValue(tourKey, out var value) ? value : null;
    }

    /// <inheritdoc />
    public void SetShowEveryTime(string tourKey, bool value)
    {
        EnsureLoaded();
        _values[tourKey] = value;
        _dirty = true;
    }

    /// <inheritdoc />
    public void Flush()
    {
        if (!_dirty)
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(new StoredState(_values), SerializerOptions);
            var tempFilePath = _filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tempFilePath, json);
            File.Move(tempFilePath, _filePath, overwrite: true);
            _dirty = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            SpyglassTrace.Source.TraceEvent(
                TraceEventType.Warning,
                0,
                "Spyglass could not persist tour state to '{0}': {1}",
                _filePath,
                ex.Message);
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            var json = File.ReadAllText(_filePath);
            var stored = JsonSerializer.Deserialize<StoredState>(json, SerializerOptions);
            if (stored?.Tours is null)
            {
                return;
            }

            foreach (var pair in stored.Tours)
            {
                _values[pair.Key] = pair.Value;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            SpyglassTrace.Source.TraceEvent(
                TraceEventType.Warning,
                0,
                "Spyglass could not load tour state from '{0}'; starting fresh: {1}",
                _filePath,
                ex.Message);
            _values.Clear();
        }
    }

    private static string ResolveDefaultFilePath()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var company = entryAssembly?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        var product = entryAssembly?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

        if (string.IsNullOrWhiteSpace(company))
        {
            company = "BlackBeard";
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            product = entryAssembly?.GetName().Name ?? "Spyglass";
        }

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, company!, product!, "spyglass.tours.json");
    }

    private sealed class StoredState
    {
        public StoredState()
        {
        }

        public StoredState(Dictionary<string, bool> tours) => Tours = tours;

        public Dictionary<string, bool> Tours { get; set; } = new();
    }
}
