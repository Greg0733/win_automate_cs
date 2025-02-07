using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Data.Json;

namespace launcher.ArknightsRecruit
{
    internal class ValueName<T>
    {
        internal readonly T value;
        internal readonly string name;

        public ValueName(T value, string name)
        {
            this.value = value;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (value == null) return false;

            if (obj is ValueName<T> other && value.Equals(other.value)) return true;

            return obj is T otherAsT && value.Equals(otherAsT);
        }

        public override int GetHashCode()
        {
            if (value == null) return 0;

            return value.GetHashCode();
        }
    }

    public readonly struct PriorityRule
    {
        public int Type { get; init; }
        public dynamic Value { get; init; }
        public int Action { get; init; }
        public bool Guaranteed { get; init; }
    }
    public readonly struct Preferences
    {
        [JsonPropertyName("window title contains")]
        public string? WindowTitleContains { get; init; }
        [JsonPropertyName("ask before proceeding")]
        public bool? AskBeforeProceeding { get; init; }
        [JsonPropertyName("do refresh")]
        public bool? DoRefresh { get; init; }
        [JsonPropertyName("do recruit")]
        public bool? DoRecruit { get; init; }
        [JsonPropertyName("do expedite")]
        public bool? DoExpedite { get; init; }
        [JsonPropertyName("do hire")]
        public bool? DoHire { get; init; }
        [JsonPropertyName("priority rules")]
        public List<PriorityRule>? PriorityRules { get; init; }
    }

    internal class JsonArknightsRecruitAndIIRC
    {
        internal readonly int actionInform;
        internal readonly int actionRecruit;
        internal readonly int actionRefresh;

        internal readonly int ruleTypeRarity;
        internal readonly int ruleTypeOperator;

        internal readonly ValueName<int>[] actions;
        internal readonly ValueName<int>[] ruleTypes;
        internal readonly short[] rarities;
        internal readonly string[] operators;

        internal readonly Preferences preferences;

        private readonly static JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly string configFilename = "config.json";
        private readonly string operatorsFilename = "operators_list.json";
        private readonly string preferencesPath;

        internal JsonArknightsRecruitAndIIRC(string configDir)
        {
            string configPath = Path.Join(configDir, configFilename);
            // TODO handle exception
            JsonObject configRoot = JsonObject.Parse(File.ReadAllText(configPath));
            JsonObject configConstants = configRoot.GetNamedObject("constants");
            preferencesPath = Path.Join(Path.GetDirectoryName(configPath), configRoot.GetNamedString("preferences filename")); 
            JsonObject configGame = configRoot.GetNamedObject("game constraints");

            actions = new ValueName<int>[3];
            ruleTypes = new ValueName<int>[2];

            actionInform = ParseConstant(configConstants, "action inform", ref actions, 0);
            actionRecruit = ParseConstant(configConstants, "action recruit", ref actions, 1);
            actionRefresh = ParseConstant(configConstants, "action refresh", ref actions, 2);

            ruleTypeRarity = ParseConstant(configConstants, "rule type rarity", ref ruleTypes, 0);
            ruleTypeOperator = ParseConstant(configConstants, "rule type operator", ref ruleTypes, 1);

            JsonArray raritiesJson = configGame.GetNamedArray("available rarities");
            rarities = new short[raritiesJson.Count];
            for (uint i = 0; i < raritiesJson.Count; i++)
            {
                rarities[i] = (short)raritiesJson.GetNumberAt(i);
            }

            string operatorsPath = Path.Join(configDir, operatorsFilename);
            JsonArray operatorsJson = JsonArray.Parse(File.ReadAllText(operatorsPath));
            operators = new string[operatorsJson.Count];
            for (uint i = 0; i < operatorsJson.Count; i++)
            {
                operators[i] = operatorsJson.GetObjectAt(i).GetNamedString("title");
            }

            preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(preferencesPath), jsonSerializerOptions);
        }

        internal void SavePreferences(Preferences newPreferences)
        {
            JsonSerializerOptions jsonOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            File.WriteAllText(preferencesPath, JsonSerializer.Serialize(newPreferences, jsonOptions));
        }

        internal static short? GetSelectedRarity(PriorityRule priorityRule)
        {
            try {
                return priorityRule.Value.GetInt16();
            } catch (InvalidOperationException)
            {
                return null;
            } catch (FormatException)
            {
                return null;
            }
            
        }

        internal static string? GetSelectedOperator(PriorityRule priorityRule)
        {
            try
            {
                return priorityRule.Value.GetString();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static int ParseConstant(JsonObject configConstants, string key, ref ValueName<int>[] subConstantsArray, int index)
        {
            JsonObject configInform = configConstants.GetNamedObject(key);
            int value = (int)configInform.GetNamedNumber("value");
            subConstantsArray[index] = new(value, configInform.GetNamedString("display name"));

            return value;
        }
    }
}
