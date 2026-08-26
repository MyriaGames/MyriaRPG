using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Myria.Lib.Core.Systems.Mods;

namespace Myria.Wpf.Services.Mods
{
    public static class ModManifestStore
    {
        private static readonly JsonSerializerOptions _writeOpts = new() { WriteIndented = true };

        // ── mod.json (general properties: enabled, loadOrder) ─────────────────

        public static void WriteProperty(LoadedMod mod, string propertyName, JsonNode? value)
        {
            var manifest = ReadJson(Path.Combine(mod.Directory, "mod.json"));
            manifest[FindKey(manifest, propertyName)] = value;
            WriteJson(Path.Combine(mod.Directory, "mod.json"), manifest);
        }

        // ── setting_values.json (user-chosen setting values) ──────────────────

        public static void WriteSettingValue(LoadedMod mod, string settingKey, string value)
        {
            var path   = Path.Combine(mod.Directory, "setting_values.json");
            var values = ReadJson(path);

            string valuesKey = FindKey(values, settingKey);
            values[valuesKey] = value;
            WriteJson(path, values);

            // Keep the in-memory copy in sync so the UI reflects the change immediately.
            mod.SettingValues[settingKey] = value;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static JsonObject ReadJson(string path)
        {
            try
            {
                if (File.Exists(path))
                    return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
            }
            catch { }
            return new JsonObject();
        }

        private static void WriteJson(string path, JsonObject obj)
        {
            File.WriteAllText(path, obj.ToJsonString(_writeOpts));
        }

        private static string FindKey(JsonObject obj, string propertyName)
            => obj.Select(p => p.Key)
                  .FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase))
               ?? propertyName;
    }
}
