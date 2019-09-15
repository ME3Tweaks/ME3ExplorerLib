﻿namespace Gammtek.Conduit.Data
{
	/// <summary>
	///     The value validator interface
	/// </summary>
	/// <typeparam name="TValue">
	///     The type of the value
	/// </typeparam>
	public interface IValueValidator<in TValue>
	{
		/// <summary>
		///     Determines whether the specified value is valid.
		/// </summary>
		/// <param name="value">
		///     The value.
		/// </param>
		/// <returns>
		///     <c>true</c> if is valid, otherwise <c>false</c>.
		/// </returns>
		bool IsValid(TValue @value);
	}
}
