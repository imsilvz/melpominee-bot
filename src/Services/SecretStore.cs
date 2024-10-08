﻿using System.Text.Json;
using System.Collections.Concurrent;
namespace Melpominee.Services;
public class SecretStore
{
    private static Lazy<SecretStore> _instance = new Lazy<SecretStore>(() => new SecretStore());
    public static SecretStore Instance => _instance.Value;

    private ConcurrentDictionary<string, string> _store;
    public SecretStore()
    {
        _store = new ConcurrentDictionary<string, string>();

        string[]? secretFiles = null;
        if(Directory.Exists($"{Directory.GetCurrentDirectory()}/secrets/"))
        {
            secretFiles = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/secrets/");
        }

        if (secretFiles is not null)
        {
            foreach (var fileName in secretFiles)
            {
                LoadSecret(Path.GetFileName(fileName));
            }
        }
    }

    public void LoadSecret(string filename)
    {
        // check docker directory
        string filePath = $"{Directory.GetCurrentDirectory()}/secrets/{filename}";
        if (File.Exists(filePath))
        {
            // read file
            string secretText = File.ReadAllText(filePath);
            // load to store
            var secretDict = JsonSerializer.Deserialize<Dictionary<string, string>>(secretText);
            if (secretDict is not null)
            {
                foreach (var item in secretDict)
                {
                    _store.AddOrUpdate(item.Key.ToUpper(), item.Value, (key, oldValue) => item.Value);
                }
            }
        }
    }

    public string GetSecret(string key, bool opt = false)
    {
        string? val;
        if (_store.TryGetValue(key, out val))
        {
            return val;
        }

        if (!opt)
        {
            throw new KeyNotFoundException($"Secret '{key}' does not exist in SecretStore!");
        }
        return "";
    }
}