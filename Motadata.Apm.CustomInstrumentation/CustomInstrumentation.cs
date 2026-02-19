/*
 *   Copyright (c) Motadata 2026. All rights reserved.
 *
 *   This source code is the property of Motadata and constitutes
 *   proprietary and confidential information. Unauthorized copying, distribution,
 *   modification, or use of this file, via any medium, is strictly prohibited
 *   unless prior written permission is obtained from Motadata.
 *
 *   Unauthorized access or use of this software may result in legal action
 *   and/or prosecution to the fullest extent of the law.
 *
 *   This software is provided "AS IS," without warranties of any kind, express
 *   or implied, including but not limited to implied warranties of
 *   merchantability or fitness for a particular purpose. In no event shall
 *   Motadata be held liable for any damages arising from the use
 *   of this software.
 *
 *   For inquiries, contact: engg@motadata.com
 *
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Motadata.Apm.CustomInstrumentation
{
    /// <summary>
    /// Utility class for setting custom instrumentation attributes on OpenTelemetry spans.
    /// <list type="bullet">
    /// <item><description>Setting scalar attributes (bool, double, float, int, long, string) on the current span</description></item>
    /// <item><description>Setting array attributes (arrays of bool, double, float, int, long, string) on the current span</description></item>
    /// <item><description>Validation of attribute keys and values with descriptive error messages</description></item>
    /// </list>
    /// <para>
    /// All attribute keys are automatically prefixed with "apm." unless already present.
    /// This ensures consistent namespacing for APM-related attributes across the application.
    /// </para>
    /// <para>
    /// Thread-safe: All methods operate on the current span context using Activity.Current,
    /// which flows across async/await boundaries via AsyncLocal&lt;T&gt; in .NET.
    /// The class depends only on the Activity.Current property for span access.
    /// </para>
    /// </summary>
    public static class CustomInstrumentation
    {
        private const string DefaultPrefix = "apm.";

        private static readonly Regex KeyValidationRegex = new Regex("^[a-zA-Z0-9.]+$", RegexOptions.Compiled);

        /// <summary>
        /// Prepares and validates the attribute key for use with OpenTelemetry spans.
        /// <para>
        /// This method performs the following operations:
        /// <list type="number">
        /// <item><description>Validates that the key is not null</description></item>
        /// <item><description>Trims leading and trailing whitespace from the key</description></item>
        /// <item><description>Validates that the key is not empty after trimming</description></item>
        /// <item><description>Validates that the key contains only valid characters (alphabets, numbers, and dots - no spaces or special characters)</description></item>
        /// <item><description>Converts the key to lowercase for consistency</description></item>
        /// <item><description>Adds the "apm." prefix if not already present</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="key">The original attribute key</param>
        /// <returns>The prepared key with "apm." prefix in lowercase</returns>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters</exception>
        private static string PrepareKey(string key)
        {
            if (key == null)
            {
                throw new Exception("Attribute key cannot be null");
            }

            key = key.Trim();

            if (key.Length == 0)
            {
                throw new Exception("Attribute key cannot be empty or whitespace only");
            }

            if (!KeyValidationRegex.IsMatch(key))
            {
                throw new Exception($"Attribute key contains invalid characters. Only alphabets, numbers, and dots are allowed: '{key}'");
            }

            key = key.ToLowerInvariant();

            return key.StartsWith(DefaultPrefix, StringComparison.Ordinal) ? key : DefaultPrefix + key;
        }

        /// <summary>
        /// Validates that an array is not null and not empty.
        /// <list type="bullet">
        /// <item><description>This method is used to validate array attribute values before setting them on a span.</description></item>
        /// <item><description>The type name is automatically determined from the generic type parameter.</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">The type of elements in the array</typeparam>
        /// <param name="array">The array to validate</param>
        /// <param name="key">The attribute key (used in error messages)</param>
        /// <exception cref="Exception">Thrown if the array is null or empty</exception>
        private static void ValidateList<T>(T[] array, string key)
        {
            if (array == null)
            {
                throw new Exception($"{typeof(T).Name} array cannot be null for key: {key}");
            }

            if (array.Length == 0)
            {
                throw new Exception($"{typeof(T).Name} array cannot be empty for key: {key}");
            }
        }

        /// <summary>
        /// Filters out null values from an array and returns a new array containing only non-null values.
        /// <para>
        /// This method is thread-safe as it:
        /// <list type="bullet">
        /// <item><description>Does not modify the input array</description></item>
        /// <item><description>Uses LINQ for efficient filtering</description></item>
        /// <item><description>Uses only local variables (no shared state)</description></item>
        /// <item><description>Is stateless and can be safely called from multiple threads</description></item>
        /// </list>
        /// The type name is automatically determined from the generic type parameter.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of elements in the array</typeparam>
        /// <param name="array">The input array to filter (must not be null)</param>
        /// <param name="key">The attribute key (used in error messages)</param>
        /// <returns>A new array containing only non-null values from the input array</returns>
        /// <exception cref="Exception">Thrown if the filtered array is empty (all values were null)</exception>
        private static T[] FilterNullValues<T>(T[] array, string key)
        {
            var filtered = array.Where(v => v != null).ToArray();

            if (filtered.Length == 0)
            {
                throw new Exception($"{typeof(T).Name} array contains only null values for key: {key}");
            }

            return filtered;
        }

        /// <summary>
        /// Filters out NaN and Infinite values from a Double array.
        /// <list type="bullet">
        /// <item><description>This method is thread-safe and uses LINQ for efficient filtering.</description></item>
        /// <item><description>Note: Since double is a value type in .NET, the array elements cannot be null.</description></item>
        /// </list>
        /// </summary>
        /// <param name="array">The input array to filter (must not be null)</param>
        /// <param name="key">The attribute key (used in error messages)</param>
        /// <returns>A new array containing only valid Double values</returns>
        /// <exception cref="Exception">Thrown if the filtered array is empty</exception>
        private static double[] FilterDoubles(double[] array, string key)
        {
            var filtered = array.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();

            if (filtered.Length == 0)
            {
                throw new Exception($"Double arr ay contains only invalid values for key: {key}");
            }

            return filtered;
        }

        /// <summary>
        /// Filters out NaN and Infinite values from a Float array and converts to Double.
        /// <list type="bullet">
        /// <item><description>This method is thread-safe and uses LINQ for efficient filtering and conversion.</description></item>
        /// <item><description>Float values are converted to Double for OpenTelemetry compatibility.</description></item>
        /// <item><description>Note: Since float is a value type in .NET, the array elements cannot be null.</description></item>
        /// </list>
        /// </summary>
        /// <param name="array">The input array to filter (must not be null)</param>
        /// <param name="key">The attribute key (used in error messages)</param>
        /// <returns>A new array containing valid Double values converted from Floats</returns>
        /// <exception cref="Exception">Thrown if the filtered array is empty</exception>
        private static double[] ConvertFloats(float[] array, string key)
        {
            var filtered = array.Where(v => !float.IsNaN(v) && !float.IsInfinity(v)).Select(v => (double)v).ToArray();

            if (filtered.Length == 0)
            {
                throw new Exception($"Float array contains only invalid values for key: {key}");
            }

            return filtered;
        }

        /// <summary>
        /// Converts an Integer array to Long array.
        /// <list type="bullet">
        /// <item><description>Integer values are converted to Long for OpenTelemetry compatibility.</description></item>
        /// <item><description>Note: Since int is a value type in .NET, the array cannot contain null values, so no filtering is required - only type conversion.</description></item>
        /// <item><description>Uses Array.ConvertAll for optimal performance with direct array allocation.</description></item>
        /// </list>
        /// </summary>
        /// <param name="array">The input array to convert (must not be null)</param>
        /// <returns>A new array containing Long values converted from Integers</returns>
        private static long[] ConvertIntegers(int[] array)
        {
            return Array.ConvertAll(array, v => (long)v);
        }

        /// <summary>
        /// Retrieves the current active span from the OpenTelemetry context.
        /// </summary>
        /// <returns>The current active Activity (span)</returns>
        /// <exception cref="Exception">Thrown if no active span is available</exception>
        private static Activity GetCurrentSpan()
        {
            var span = Activity.Current;

            if (span == null)
            {
                throw new Exception("No active span available in current context");
            }

            return span;
        }

        /// <summary>
        /// Sets a boolean attribute on the current span.
        /// <para>
        /// The attribute key will be automatically prefixed with "apm." if not already present.
        /// </para>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="value">The boolean value to set</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if an error occurs while setting the attribute</exception>
        public static void Set(string key, bool value)
        {
            key = PrepareKey(key);

            GetCurrentSpan().SetTag(key, value);
        }

        /// <summary>
        /// Sets a double attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>The value must be a valid finite number (not NaN or Infinite).</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="value">The double value to set (cannot be NaN or Infinite)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the value is NaN or Infinite; or if an error occurs while setting the attribute</exception>
        public static void Set(string key, double value)
        {
            key = PrepareKey(key);

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new Exception($"Invalid Double value for key: {key}");
            }

            GetCurrentSpan().SetTag(key, value);
        }

        /// <summary>
        /// Sets a float attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>The value must be a valid finite number (not NaN or Infinite).</description></item>
        /// <item><description>The float value is internally converted to a double for OpenTelemetry compatibility.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="value">The float value to set (cannot be NaN or Infinite)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the value is NaN or Infinite; or if an error occurs while setting the attribute</exception>
        public static void Set(string key, float value)
        {
            key = PrepareKey(key);

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new Exception($"Invalid Float value for key: {key}");
            }

            GetCurrentSpan().SetTag(key, (double)value);
        }

        /// <summary>
        /// Sets an integer attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>The integer value is internally converted to a long for OpenTelemetry compatibility.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="value">The integer value to set</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if an error occurs while setting the attribute</exception>
        public static void Set(string key, int value)
        {
            key = PrepareKey(key);

            GetCurrentSpan().SetTag(key, (long)value);
        }

        /// <summary>
        /// Sets a long attribute on the current span.
        /// <para>
        /// The attribute key will be automatically prefixed with "apm." if not already present.
        /// </para>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="value">The long value to set</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if an error occurs while setting the attribute</exception>
        public static void Set(string key, long value)
        {
            key = PrepareKey(key);

            GetCurrentSpan().SetTag(key, value);
        }

        /// <summary>
        /// Sets a string attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>The value cannot be null, empty, or contain only whitespace characters.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="value">The string value to set (cannot be null, empty, or whitespace-only)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the value is null, empty, or whitespace-only; or if an error occurs while setting the attribute</exception>
        public static void Set(string key, string value)
        {
            key = PrepareKey(key);

            if (value == null || value.Trim().Length == 0)
            {
                throw new Exception($"String value cannot be null or empty for key: {key}");
            }

            GetCurrentSpan().SetTag(key, value);
        }

        /// <summary>
        /// Sets a boolean array attribute on the current span.
        /// <para>
        /// The attribute key will be automatically prefixed with "apm." if not already present.
        /// Note: Since bool is a value type in .NET, the array cannot contain null elements.
        /// </para>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="values">The array of boolean values (cannot be null or empty)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the array is null or empty; or if an error occurs while setting the attribute</exception>
        public static void SetBooleanList(string key, bool[] values)
        {
            key = PrepareKey(key);

            ValidateList(values, key);

            GetCurrentSpan().SetTag(key, values);
        }

        /// <summary>
        /// Sets a double array attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>NaN and Infinite values in the array are automatically filtered out.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="values">The array of double values (cannot be null or empty, must contain at least one valid value)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the array is null, empty, or contains only invalid values; or if an error occurs while setting the attribute</exception>
        public static void SetDoubleList(string key, double[] values)
        {
            key = PrepareKey(key);

            ValidateList(values, key);

            var filtered = FilterDoubles(values, key);

            GetCurrentSpan().SetTag(key, filtered);
        }

        /// <summary>
        /// Sets a float array attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>NaN and Infinite values in the array are automatically filtered out.</description></item>
        /// <item><description>Float values are internally converted to double for OpenTelemetry compatibility.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="values">The array of float values (cannot be null or empty, must contain at least one valid value)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the array is null, empty, or contains only invalid values; or if an error occurs while setting the attribute</exception>
        public static void SetFloatList(string key, float[] values)
        {
            key = PrepareKey(key);

            ValidateList(values, key);

            var filtered = ConvertFloats(values, key);

            GetCurrentSpan().SetTag(key, filtered);
        }

        /// <summary>
        /// Sets an integer array attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>Integer values are internally converted to long for OpenTelemetry compatibility.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="values">The array of integer values (cannot be null or empty)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the array is null or empty; or if an error occurs while setting the attribute</exception>
        public static void SetIntegerList(string key, int[] values)
        {
            key = PrepareKey(key);

            ValidateList(values, key);

            var converted = ConvertIntegers(values);

            GetCurrentSpan().SetTag(key, converted);
        }

        /// <summary>
        /// Sets a long array attribute on the current span.
        /// <para>
        /// The attribute key will be automatically prefixed with "apm." if not already present.
        /// </para>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="values">The array of long values (cannot be null or empty)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the array is null or empty; or if an error occurs while setting the attribute</exception>
        public static void SetLongList(string key, long[] values)
        {
            key = PrepareKey(key);

            ValidateList(values, key);

            GetCurrentSpan().SetTag(key, values);
        }

        /// <summary>
        /// Sets a string array attribute on the current span.
        /// <list type="bullet">
        /// <item><description>The attribute key will be automatically prefixed with "apm." if not already present.</description></item>
        /// <item><description>Null elements in the array are automatically filtered out.</description></item>
        /// </list>
        /// </summary>
        /// <param name="key">The attribute key (will be prefixed with "apm." if needed)</param>
        /// <param name="values">The array of string values (cannot be null or empty, must contain at least one non-null value)</param>
        /// <exception cref="Exception">Thrown if the key is null, empty, or contains invalid characters; or if the array is null, empty, or contains only null values; or if an error occurs while setting the attribute</exception>
        public static void SetStringList(string key, string[] values)
        {
            key = PrepareKey(key);

            ValidateList(values, key);

            var filtered = FilterNullValues(values, key);

            GetCurrentSpan().SetTag(key, filtered);
        }
    }
}
