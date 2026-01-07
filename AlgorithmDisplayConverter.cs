using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace FactorCalculator;

/// <summary>
/// Converts CalculationAlgorithm enum values to user-friendly German display names.
/// </summary>
public class AlgorithmDisplayConverter : IValueConverter {
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
		if (value is CalculationAlgorithm algorithm) {
			return algorithm switch {
				CalculationAlgorithm.BruteForce => "Genau",
				CalculationAlgorithm.MeetInTheMiddle => "Schnell",
				_ => algorithm.ToString()
			};
		}
		return value?.ToString();
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
		// Not needed for display purposes
		throw new NotImplementedException();
	}
}
