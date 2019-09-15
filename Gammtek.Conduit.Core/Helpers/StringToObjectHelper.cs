﻿using System;
using System.Globalization;
using System.Text;
using Gammtek.Conduit.Extensions.Reflection;

namespace Gammtek.Conduit.Helpers
{
	/// <summary>
	///     String to object helper class that converts a string to the right object if possible.
	/// </summary>
	public static class StringToObjectHelper
	{
		//private static ILog Log { get; } = LogManager.GetCurrentClassLogger();
		
		/// <summary>
		///     Gets or sets the default culture to use for parsing.
		/// </summary>
		/// <value>The default culture.</value>
		public static CultureInfo DefaultCulture { get; set; } = CultureInfo.InvariantCulture;

		/// <summary>
		///     Converts a string to a boolean.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The boolean value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static bool ToBool(string value)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			if (string.Equals("0", value, StringComparison.Ordinal))
			{
				return false;
			}

			return string.Equals("1", value, StringComparison.Ordinal)
				   || bool.Parse(value);
		}

		/// <summary>
		///     Converts a string to a byte array.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The byte array value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static byte[] ToByteArray(string value)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			var encoding = new UTF8Encoding();

			return encoding.GetBytes(value);
		}

		/// <summary>
		///     Converts a string to a date/time.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The date/time value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static DateTime ToDateTime(string value)
		{
			return ToDateTime(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to a date/time.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The date/time value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static DateTime ToDateTime(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return DateTime.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to a decimal.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The decimal value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static decimal ToDecimal(string value)
		{
			return ToDecimal(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to a decimal.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The decimal value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static decimal ToDecimal(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return decimal.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to a double.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The double value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static double ToDouble(string value)
		{
			return ToDouble(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to a double.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The double value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static double ToDouble(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return double.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to an enum value. If the value cannot be converted for any reason, the <paramref name="defaultValue" />
		///     will be returned.
		/// </summary>
		/// <typeparam name="TEnumValue">The type of the enum.</typeparam>
		/// <param name="value">The value.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>The enum value representing the string.</returns>
		public static TEnumValue ToEnum<TEnumValue>(string value, TEnumValue defaultValue)
			where TEnumValue : struct, IComparable, IFormattable
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return defaultValue;
			}

			TEnumValue enumValue;

			if (!Enum<TEnumValue>.TryParse(value, out enumValue))
			{
				enumValue = defaultValue;
			}

			return enumValue;
		}

		/// <summary>
		///     Converts a string to a float.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The float value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static float ToFloat(string value)
		{
			return ToFloat(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to a float.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The float value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static float ToFloat(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return float.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to a guid.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The guid value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static Guid ToGuid(string value)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return new Guid(value);
		}

		/// <summary>
		///     Converts a string to an integer.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The integer value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static int ToInt(string value)
		{
			return ToInt(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to an integer.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The integer value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static int ToInt(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return int.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to a long.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The long value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static long ToLong(string value)
		{
			return ToLong(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to a long.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The long value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static long ToLong(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return long.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to the right target type, such as <see cref="string" />, <see cref="bool" /> and <see cref="DateTime" />.
		/// </summary>
		/// <param name="targetType">The target type to convert to.</param>
		/// <param name="value">The value to convert to the specified target type.</param>
		/// <returns>The converted value. If the <paramref name="value" /> is <c>null</c>, this method will return <c>null</c>.</returns>
		/// <exception cref="NotSupportedException">The specified <paramref name="targetType" /> is not supported.</exception>
		public static object ToRightType(Type targetType, string value)
		{
			return ToRightType(targetType, value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to the right target type, such as <see cref="string" />, <see cref="bool" /> and <see cref="DateTime" />.
		/// </summary>
		/// <param name="targetType">The target type to convert to.</param>
		/// <param name="value">The value to convert to the specified target type.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The converted value. If the <paramref name="value" /> is <c>null</c>, this method will return <c>null</c>.</returns>
		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="NotSupportedException">The specified <paramref name="targetType" /> is not supported.</exception>
		public static object ToRightType(Type targetType, string value, CultureInfo cultureInfo)
		{
			if (value == null)
			{
				return null;
			}

			if (targetType == typeof (string))
			{
				return value;
			}

			if (targetType == typeof (bool))
			{
				return ToBool(value);
			}

			if (targetType == typeof (bool?))
			{
				return ToBool(value);
			}

			if (targetType == typeof (byte[]))
			{
				return ToByteArray(value);
			}

			if (targetType == typeof (DateTime))
			{
				return ToDateTime(value, cultureInfo);
			}

			if (targetType == typeof (DateTime?))
			{
				return ToDateTime(value, cultureInfo);
			}

			if (targetType == typeof (TimeSpan))
			{
				return ToTimeSpan(value, cultureInfo);
			}

			if (targetType == typeof (TimeSpan?))
			{
				return ToTimeSpan(value, cultureInfo);
			}

			if (targetType == typeof (decimal))
			{
				return ToDecimal(value, cultureInfo);
			}

			if (targetType == typeof (decimal?))
			{
				return ToDecimal(value, cultureInfo);
			}

			if (targetType == typeof (double))
			{
				return ToDouble(value, cultureInfo);
			}

			if (targetType == typeof (double?))
			{
				return ToDouble(value, cultureInfo);
			}

			if (targetType == typeof (float))
			{
				return ToFloat(value, cultureInfo);
			}

			if (targetType == typeof (float?))
			{
				return ToFloat(value, cultureInfo);
			}

			if (targetType == typeof (Guid))
			{
				return ToGuid(value);
			}

			if (targetType == typeof (Guid?))
			{
				return ToGuid(value);
			}

			if (targetType == typeof (int))
			{
				return ToInt(value, cultureInfo);
			}

			if (targetType == typeof (int?))
			{
				return ToInt(value, cultureInfo);
			}

			if (targetType == typeof (long))
			{
				return ToLong(value, cultureInfo);
			}

			if (targetType == typeof (long?))
			{
				return ToLong(value, cultureInfo);
			}

			if (targetType == typeof (Type))
			{
				return ToType(value);
			}

			if (targetType.IsEnumEx())
			{
				return Enum.Parse(targetType, value, false);
			}

			//throw Log.ErrorAndCreateException<NotSupportedException>("Type '{0}' is not yet supported", targetType.FullName);
			throw new NotSupportedException($"Type '{targetType.FullName}' is not yet supported");
		}

		/// <summary>
		///     Converts a string to a string.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The string value of the string.</returns>
		public static string ToString(string value)
		{
			return value;
		}

		/// <summary>
		///     Converts a string to a timespan.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The timespan value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static TimeSpan ToTimeSpan(string value)
		{
			return ToTimeSpan(value, DefaultCulture);
		}

		/// <summary>
		///     Converts a string to a timespan.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="cultureInfo">The culture information.</param>
		/// <returns>The timespan value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static TimeSpan ToTimeSpan(string value, CultureInfo cultureInfo)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return TimeSpan.Parse(value, cultureInfo);
		}

		/// <summary>
		///     Converts a string to a Type.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The Type value of the string.</returns>
		/// <exception cref="ArgumentException">The <paramref name="value" /> is <c>null</c> or whitespace.</exception>
		public static Type ToType(string value)
		{
			Argument.IsNotNullOrWhitespace(nameof(value), value);

			value = CleanString(value);

			return Type.GetType(value);
		}

		/// <summary>
		///     Cleans up the string, for example by removing the braces.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns>The cleaned up string.</returns>
		private static string CleanString(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			while (value.StartsWith("(") && value.EndsWith(")"))
			{
				value = value.Substring(1, value.Length - 2);
			}

			return value.Trim();
		}
	}
}
