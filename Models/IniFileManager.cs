using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Windows11Settings.Models
{
    /// <summary>
    /// Simple INI file manager for reading and writing configuration files
    /// </summary>
    public class IniFileManager
    {
        private readonly string _filePath;
        private readonly Dictionary<string, Dictionary<string, string>> _sections;

        public IniFileManager(string filePath)
        {
            _filePath = filePath;
            _sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            LoadFile();
        }

        /// <summary>
        /// Load the INI file into memory
        /// </summary>
        private void LoadFile()
        {
            _sections.Clear();

            if (!File.Exists(_filePath))
            {
                // Create default sections for new file
                _sections["UI"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            try
            {
                string currentSection = null;
                var sectionData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in File.ReadAllLines(_filePath))
                {
                    var trimmedLine = line.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    // Check if this is a section header
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        // Save previous section if it exists
                        if (currentSection != null && sectionData.Any())
                        {
                            _sections[currentSection] = sectionData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        }

                        // Start new section
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        sectionData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    // Check if this is a key-value pair
                    else if (currentSection != null && trimmedLine.Contains("="))
                    {
                        var equalsIndex = trimmedLine.IndexOf('=');
                        var key = trimmedLine.Substring(0, equalsIndex).Trim();
                        var value = trimmedLine.Substring(equalsIndex + 1).Trim();

                        // Remove quotes if present
                        if ((value.StartsWith("\"") && value.EndsWith("\"")) || 
                            (value.StartsWith("'") && value.EndsWith("'")))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        sectionData[key] = value;
                    }
                }

                // Save the last section
                if (currentSection != null && sectionData.Any())
                {
                    _sections[currentSection] = sectionData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading INI file: {ex.Message}");
                // Initialize with default sections on error
                _sections["UI"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Save the current data to the INI file
        /// </summary>
        private void SaveFile()
        {
            try
            {
                var sb = new StringBuilder();

                foreach (var section in _sections)
                {
                    if (!section.Value.Any())
                        continue;

                    sb.AppendLine($"[{section.Key}]");

                    foreach (var kvp in section.Value)
                    {
                        var value = kvp.Value;
                        // Add quotes if value contains spaces, equals, or other special characters
                        if (value.Contains(" ") || value.Contains("=") || value.Contains("[") || value.Contains("]") || 
                            value.StartsWith("\"") || value.StartsWith("'"))
                        {
                            value = $"\"{value}\"";
                        }
                        sb.AppendLine($"{kvp.Key}={value}");
                    }

                    sb.AppendLine();
                }

                // Write to file
                File.WriteAllText(_filePath, sb.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving INI file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a value from the INI file
        /// </summary>
        /// <typeparam name="T">The type to convert the value to</typeparam>
        /// <param name="section">The section name</param>
        /// <param name="key">The key name</param>
        /// <param name="defaultValue">The default value if the key doesn't exist</param>
        /// <returns>The value from the INI file or the default value</returns>
        public T GetValue<T>(string section, string key, T defaultValue)
        {
            try
            {
                if (!_sections.TryGetValue(section, out var sectionData) || 
                    !sectionData.TryGetValue(key, out var valueString))
                {
                    return defaultValue;
                }

                // Convert string to requested type
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)valueString;
                }
                else if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    if (int.TryParse(valueString, out var intValue))
                        return (T)(object)intValue;
                }
                else if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                {
                    if (bool.TryParse(valueString, out var boolValue))
                        return (T)(object)boolValue;
                    // Handle common string representations of boolean
                    if (valueString.Equals("1", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)true;
                    if (valueString.Equals("0", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)false;
                    if (valueString.Equals("true", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)true;
                    if (valueString.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)false;
                }
                else if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
                {
                    if (double.TryParse(valueString, out var doubleValue))
                        return (T)(object)doubleValue;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting value from INI file: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Set a value in the INI file
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="key">The key name</param>
        /// <param name="value">The value to set</param>
        public void SetValue(string section, string key, string value)
        {
            try
            {
                if (!_sections.TryGetValue(section, out var sectionData))
                {
                    sectionData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _sections[section] = sectionData;
                }

                sectionData[key] = value;
                SaveFile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting value in INI file: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a key from a section
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="key">The key name</param>
        public void RemoveKey(string section, string key)
        {
            try
            {
                if (_sections.TryGetValue(section, out var sectionData))
                {
                    sectionData.Remove(key);
                    SaveFile();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing key from INI file: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove an entire section
        /// </summary>
        /// <param name="section">The section name</param>
        public void RemoveSection(string section)
        {
            try
            {
                _sections.Remove(section);
                SaveFile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing section from INI file: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a section exists
        /// </summary>
        /// <param name="section">The section name</param>
        /// <returns>True if the section exists</returns>
        public bool SectionExists(string section)
        {
            return _sections.ContainsKey(section);
        }

        /// <summary>
        /// Check if a key exists in a section
        /// </summary>
        /// <param name="section">The section name</param>
        /// <param name="key">The key name</param>
        /// <returns>True if the key exists</returns>
        public bool KeyExists(string section, string key)
        {
            return _sections.TryGetValue(section, out var sectionData) && sectionData.ContainsKey(key);
        }

        /// <summary>
        /// Get all section names
        /// </summary>
        /// <returns>An array of section names</returns>
        public string[] GetSectionNames()
        {
            return _sections.Keys.ToArray();
        }

        /// <summary>
        /// Get all keys in a section
        /// </summary>
        /// <param name="section">The section name</param>
        /// <returns>An array of key names</returns>
        public string[] GetKeyNames(string section)
        {
            if (_sections.TryGetValue(section, out var sectionData))
            {
                return sectionData.Keys.ToArray();
            }
            return Array.Empty<string>();
        }
    }
}