using System;
using System.Collections.Generic;
using System.Linq;

namespace FactorCalculator;

/// <summary>
/// Specifies which algorithm to use for factor calculations.
/// </summary>
public enum CalculationAlgorithm {
        /// <summary>
        /// Brute force algorithm that tries all possible factor combinations.
        /// Time complexity: O(n^k) where n = step count, k = number of parts.
        /// </summary>
        BruteForce,

        /// <summary>
        /// Meet-in-the-middle algorithm that splits parts into two halves and uses
        /// binary search to find matching combinations.
        /// Time complexity: O(n^(k/2) * log(n^(k/2))) - much faster for 4+ parts.
        /// </summary>
        MeetInTheMiddle
}

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
                double MaximumDistance,
                CalculationAlgorithm Algorithm = CalculationAlgorithm.BruteForce
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
        /// Represents a partial sum for the meet-in-the-middle algorithm.
        /// </summary>
        private record PartialSum(double Sum, double[] Factors);

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
        /// Dispatches to the appropriate algorithm based on config.Algorithm.
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
                return config.Algorithm switch {
                        CalculationAlgorithm.MeetInTheMiddle => FindBestResultsMeetInTheMiddle(prices, config, onProgress, onResultFound),
                        _ => FindBestResultsBruteForce(prices, config, onProgress, onResultFound)
                };
        }

        /// <summary>
        /// Brute force implementation - tries all possible factor combinations.
        /// </summary>
        private static List<CalculationResult> FindBestResultsBruteForce(
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

        /// <summary>
        /// Meet-in-the-middle implementation - splits parts into two halves,
        /// precomputes partial sums for the first half, then searches for
        /// complements in the second half using binary search.
        /// </summary>
        private static List<CalculationResult> FindBestResultsMeetInTheMiddle(
                double[] prices,
                CalculationConfig config,
                Action<double>? onProgress = null,
                Action<CalculationResult>? onResultFound = null
        ) {
                // For very small part counts, brute force is more efficient
                if (prices.Length <= 2) {
                        return FindBestResultsBruteForce(prices, config, onProgress, onResultFound);
                }

                var results = new List<CalculationResult>();
                var closestDistance = double.PositiveInfinity;

                // Split prices into two halves
                var midPoint = prices.Length / 2;
                var firstHalfPrices = prices.Take(midPoint).ToArray();
                var secondHalfPrices = prices.Skip(midPoint).ToArray();

                // Phase 1: Precompute all partial sums for the first half
                onProgress?.Invoke(0.0);
                var firstHalfPartials = new List<PartialSum>();

                foreach (var factors in GenerateFactorCombinations(firstHalfPrices.Length, config)) {
                        var sum = factors.Select((f, i) => Math.Round(firstHalfPrices[i] * f, 2)).Sum();
                        firstHalfPartials.Add(new PartialSum(sum, factors));
                }

                // Sort by sum for binary search
                firstHalfPartials = firstHalfPartials.OrderBy(p => p.Sum).ToList();
                var firstHalfSums = firstHalfPartials.Select(p => p.Sum).ToArray();

                onProgress?.Invoke(0.3);

                // Phase 2: For each second-half combination, find matching first-half combinations
                var secondHalfCombos = GenerateFactorCombinations(secondHalfPrices.Length, config).ToList();
                var totalSecondHalf = secondHalfCombos.Count;
                var processedSecondHalf = 0;

                foreach (var secondFactors in secondHalfCombos) {
                        processedSecondHalf++;

                        var secondHalfSum = secondFactors.Select((f, i) => Math.Round(secondHalfPrices[i] * f, 2)).Sum();

                        // We need firstHalfSum + secondHalfSum ≈ target
                        // So firstHalfSum ≈ target - secondHalfSum
                        var targetFirstHalfSum = config.Target - secondHalfSum;

                        // Binary search for matching first-half sums within tolerance
                        var lowTarget = targetFirstHalfSum - config.MaximumDistance;
                        var highTarget = targetFirstHalfSum + config.MaximumDistance;

                        var lowIndex = BinarySearchLowerBound(firstHalfSums, lowTarget);
                        var highIndex = BinarySearchUpperBound(firstHalfSums, highTarget);

                        for (var i = lowIndex; i < highIndex && i < firstHalfPartials.Count; i++) {
                                var firstHalfPartial = firstHalfPartials[i];
                                var combinedFactors = firstHalfPartial.Factors.Concat(secondFactors).ToArray();
                                var result = CalculateResult(prices, combinedFactors, config.Target);
                                var absDistance = Math.Abs(result.Error);

                                if ((absDistance < closestDistance && absDistance < config.MaximumDistance) || absDistance < 0.001) {
                                        closestDistance = absDistance;
                                        results.Add(result);
                                        onResultFound?.Invoke(result);
                                }
                        }

                        // Report progress
                        if (processedSecondHalf % 1000 == 0) {
                                onProgress?.Invoke(0.3 + 0.7 * processedSecondHalf / totalSecondHalf);
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

        /// <summary>
        /// Binary search to find the first index where array[index] >= value.
        /// </summary>
        private static int BinarySearchLowerBound(double[] array, double value) {
                var low = 0;
                var high = array.Length;

                while (low < high) {
                        var mid = (low + high) / 2;
                        if (array[mid] < value) {
                                low = mid + 1;
                        } else {
                                high = mid;
                        }
                }

                return low;
        }

        /// <summary>
        /// Binary search to find the first index where array[index] > value.
        /// </summary>
        private static int BinarySearchUpperBound(double[] array, double value) {
                var low = 0;
                var high = array.Length;

                while (low < high) {
                        var mid = (low + high) / 2;
                        if (array[mid] <= value) {
                                low = mid + 1;
                        } else {
                                high = mid;
                        }
                }

                return low;
        }
}
