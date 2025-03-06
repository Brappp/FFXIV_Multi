using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FFXIVClientManager.Utils
{
    /// <summary>
    /// Helper class for JSON serialization and deserialization
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new StringEnumConverter() }
        };

        /// <summary>
        /// Serializes an object to a JSON string
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, DefaultSettings);
        }

        /// <summary>
        /// Deserializes a JSON string to an object
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
        }

        /// <summary>
        /// Saves an object to a JSON file
        /// </summary>
        public static void SaveToFile<T>(T obj, string filePath)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Serialize and save
                string json = Serialize(obj);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error saving to file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads an object from a JSON file
        /// </summary>
        public static T LoadFromFile<T>(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            try
            {
                string json = File.ReadAllText(filePath);
                return Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error loading from file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves an object to a JSON file asynchronously
        /// </summary>
        public static async Task SaveToFileAsync<T>(T obj, string filePath)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Serialize and save
                string json = Serialize(obj);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error saving to file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads an object from a JSON file asynchronously
        /// </summary>
        public static async Task<T> LoadFromFileAsync<T>(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                return Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error loading from file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a file contains valid JSON
        /// </summary>
        public static bool IsValidJson(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            try
            {
                string json = File.ReadAllText(filePath);
                JsonConvert.DeserializeObject(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a deep clone of an object using JSON serialization
        /// </summary>
        public static T Clone<T>(T obj)
        {
            if (obj == null)
                return default;

            string json = Serialize(obj);
            return Deserialize<T>(json);
        }
    }
}