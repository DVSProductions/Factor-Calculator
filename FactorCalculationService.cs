using System;
using System.Collections.Generic;
using System.Linq;

namespace FactorCalculator;

/// <summary>
/// Service class that encapsulates the factor calculation algorithm.
/// Extracted from MainViewModel for testability.
/// </summary>
public class FactorCalculationService {
	/// <summary>
	/// Configuration for the calculation algorithm.
	/// </summary>
	public record CalculationConfig(
		double MinFactor,
		double MaxFactor,
		double StepSize,
		double Target,
		double MaximumDistance
	) {
		public int StepCount => (int)Math.Ceiling((MaxFactor - MinFactor) / StepSize);
	}

	/// <summary>
	/// Represents the result of a factor calculation.
	/// </summary>
	public record CalculationResult(
		double[] Factors,
		double[] AdjustedPrices,
		double Total,
		double Error
	);

	/// <summary>
	/// Validates that the target is achievable with the given configuration.
	/// </summary>
	/// <returns>null if valid, or an error message if invalid</returns>
	public static string? ValidateConfiguration(double[] prices, CalculationConfig config) {
		var sumPrices = prices.Sum();
		
		if (sumPrices * config.MaxFactor < config.Target) {
			return $"Target price unreachable. Maximum achievable: {sumPrices * config.MaxFactor:F2}";
		}
		
		if (sumPrices * config.MinFactor > config.Target) {
			return $"Target price always exceeded. Minimum achievable: {sumPrices * config.MinFactor:F2}";
		}
		
		return null;
	}

	/// <summary>
	/// Generates all possible factor combinations based on the configuration.
	/// </summary>
	public static IEnumerable<double[]> GenerateFactorCombinations(int partCount, CalculationConfig config) {
		if (partCount <= 0) {
			yield break;
		}

		var stepCount = config.StepCount;
		
		IEnumerable<IEnumerable<double>> GetSub(int remainingParts) {
			if (remainingParts == 1) {
				for (var n = 0; n < stepCount; n++) {
					yield return new[] { config.MinFactor + config.StepSize * n };
				}
			} else {
				for (var n = 0; n < stepCount; n++) {
					var current = config.MinFactor + config.StepSize * n;
					foreach (var value in GetSub(remainingParts - 1)) {
						yield return value.Prepend(current);
					}
				}
			}
		}

		foreach (var combo in GetSub(partCount)) {
			yield return combo.Select(x => Math.Round(x, 2)).ToArray();
		}
	}

	/// <summary>
	/// Calculates the result for a single factor combination applied to prices.
	/// </summary>
	public static CalculationResult CalculateResult(double[] prices, double[] factors, double target) {
		if (prices.Length != factors.Length) {
			throw new ArgumentException("Prices and factors arrays must have the same length");
		}

		var adjustedPrices = factors
			.Select((factor, i) => Math.Round(prices[i] * factor, 2))
			.ToArray();
		
		var total = adjustedPrices.Sum();
		var error = target - total;

		return new CalculationResult(factors, adjustedPrices, total, error);
	}

	/// <summary>
	/// Finds all factor combinations that produce results within the maximum distance from target.
	/// </summary>
	/// <param name="prices">Array of original prices for each part</param>
	/// <param name="config">Calculation configuration</param>
	/// <param name="onProgress">Optional callback for progress updates (0.0 to 1.0)</param>
	/// <param name="onResultFound">Optional callback when a result is found</param>
	/// <returns>List of all valid results</returns>
	public static List<CalculationResult> FindBestResults(
		double[] prices,
		CalculationConfig config,
		Action<double>? onProgress = null,
		Action<CalculationResult>? onResultFound = null
	) {
		var results = new List<CalculationResult>();
		var closestDistance = double.PositiveInfinity;
		
		var totalCombinations = (long)Math.Pow(config.StepCount, prices.Length);
		var currentCombination = 0L;

		foreach (var factors in GenerateFactorCombinations(prices.Length, config)) {
			currentCombination++;
			
			var result = CalculateResult(prices, factors, config.Target);
			var absDistance = Math.Abs(result.Error);

			if ((absDistance < closestDistance && absDistance < config.MaximumDistance) || absDistance < 0.001) {
				closestDistance = absDistance;
				results.Add(result);
				onResultFound?.Invoke(result);
			}

			// Report progress periodically (every 10000 iterations)
			if (currentCombination % 10000 == 0) {
				onProgress?.Invoke((double)currentCombination / totalCombinations);
			}
		}

		// Final progress update
		onProgress?.Invoke(1.0);

		// If we found a very close match, filter out results with larger errors
		if (closestDistance < 0.01) {
			results = results.Where(x => Math.Abs(x.Error) < 0.001).ToList();
		}

		return results;
	}
}
