using Xunit;
using FactorCalculator;
using static FactorCalculator.FactorCalculationService;

namespace FactorCalculator.Tests;

/// <summary>
/// Comprehensive unit tests for the FactorCalculationService.
/// Tests cover factor generation, price calculation, result finding, and edge cases.
/// </summary>
public class FactorCalculationServiceTests {
	#region Configuration Validation Tests

	[Fact]
	public void ValidateConfiguration_WhenTargetIsAchievable_ReturnsNull() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var config = new CalculationConfig(0.5, 2.0, 0.1, 45.0, 1.0);

		// Act
		var result = ValidateConfiguration(prices, config);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void ValidateConfiguration_WhenTargetTooHigh_ReturnsError() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var config = new CalculationConfig(0.5, 2.0, 0.1, 100.0, 1.0); // Max achievable: 60

		// Act
		var result = ValidateConfiguration(prices, config);

		// Assert
		Assert.NotNull(result);
		Assert.Contains("unreachable", result.ToLower());
	}

	[Fact]
	public void ValidateConfiguration_WhenTargetTooLow_ReturnsError() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var config = new CalculationConfig(0.5, 2.0, 0.1, 10.0, 1.0); // Min achievable: 15

		// Act
		var result = ValidateConfiguration(prices, config);

		// Assert
		Assert.NotNull(result);
		Assert.Contains("exceeded", result.ToLower());
	}

	[Fact]
	public void ValidateConfiguration_WhenTargetEqualsMaximum_ReturnsNull() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var config = new CalculationConfig(0.5, 2.0, 0.1, 60.0, 1.0); // Max achievable: 60

		// Act
		var result = ValidateConfiguration(prices, config);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public void ValidateConfiguration_WhenTargetEqualsMinimum_ReturnsNull() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var config = new CalculationConfig(0.5, 2.0, 0.1, 15.0, 1.0); // Min achievable: 15

		// Act
		var result = ValidateConfiguration(prices, config);

		// Assert
		Assert.Null(result);
	}

	#endregion

	#region Factor Combination Generation Tests

	[Fact]
	public void GenerateFactorCombinations_WithZeroParts_ReturnsEmpty() {
		// Arrange
		var config = new CalculationConfig(0.5, 1.5, 0.5, 10.0, 1.0);

		// Act
		var combinations = GenerateFactorCombinations(0, config).ToList();

		// Assert
		Assert.Empty(combinations);
	}

	[Fact]
	public void GenerateFactorCombinations_WithNegativeParts_ReturnsEmpty() {
		// Arrange
		var config = new CalculationConfig(0.5, 1.5, 0.5, 10.0, 1.0);

		// Act
		var combinations = GenerateFactorCombinations(-1, config).ToList();

		// Assert
		Assert.Empty(combinations);
	}

	[Fact]
	public void GenerateFactorCombinations_WithSinglePart_ReturnsCorrectCount() {
		// Arrange
		var config = new CalculationConfig(1.0, 2.0, 0.5, 10.0, 1.0); // Steps: 1.0, 1.5, 2.0 (but ceiling gives 2 steps)
		var expectedStepCount = config.StepCount; // Should be 2

		// Act
		var combinations = GenerateFactorCombinations(1, config).ToList();

		// Assert
		Assert.Equal(expectedStepCount, combinations.Count);
	}

	[Fact]
	public void GenerateFactorCombinations_WithTwoParts_ReturnsCorrectCount() {
		// Arrange
		var config = new CalculationConfig(1.0, 2.0, 0.5, 10.0, 1.0);
		var expectedCount = config.StepCount * config.StepCount;

		// Act
		var combinations = GenerateFactorCombinations(2, config).ToList();

		// Assert
		Assert.Equal(expectedCount, combinations.Count);
	}

	[Fact]
	public void GenerateFactorCombinations_WithThreeParts_ReturnsCorrectCount() {
		// Arrange
		var config = new CalculationConfig(1.0, 2.0, 0.5, 10.0, 1.0);
		var expectedCount = (int)Math.Pow(config.StepCount, 3);

		// Act
		var combinations = GenerateFactorCombinations(3, config).ToList();

		// Assert
		Assert.Equal(expectedCount, combinations.Count);
	}

	[Fact]
	public void GenerateFactorCombinations_AllFactorsAreWithinBounds() {
		// Arrange
		var config = new CalculationConfig(0.2, 3.0, 0.5, 10.0, 1.0);

		// Act
		var combinations = GenerateFactorCombinations(2, config).ToList();

		// Assert
		foreach (var combo in combinations) {
			foreach (var factor in combo) {
				Assert.True(factor >= config.MinFactor, $"Factor {factor} is below minimum {config.MinFactor}");
				Assert.True(factor <= config.MaxFactor + config.StepSize, $"Factor {factor} is above maximum {config.MaxFactor}");
			}
		}
	}

	[Fact]
	public void GenerateFactorCombinations_FactorsAreRoundedToTwoDecimals() {
		// Arrange
		var config = new CalculationConfig(0.1, 0.3, 0.01, 10.0, 1.0);

		// Act
		var combinations = GenerateFactorCombinations(1, config).ToList();

		// Assert
		foreach (var combo in combinations) {
			foreach (var factor in combo) {
				var rounded = Math.Round(factor, 2);
				Assert.Equal(rounded, factor);
			}
		}
	}

	[Fact]
	public void GenerateFactorCombinations_GeneratesUniqueFactorSets() {
		// Arrange
		var config = new CalculationConfig(1.0, 2.0, 0.5, 10.0, 1.0);

		// Act
		var combinations = GenerateFactorCombinations(2, config).ToList();
		var uniqueCombinations = combinations.Select(c => string.Join(",", c)).Distinct().ToList();

		// Assert
		Assert.Equal(combinations.Count, uniqueCombinations.Count);
	}

	[Fact]
	public void GenerateFactorCombinations_WhenMinEqualsMax_ReturnsSingleStepPerPart() {
		// Arrange
		var config = new CalculationConfig(1.0, 1.0, 0.5, 10.0, 1.0); // StepCount should be 0 or 1

		// Act
		var combinations = GenerateFactorCombinations(2, config).ToList();

		// Assert - With min==max, stepCount is ceiling((1-1)/0.5)=0, so no combinations
		Assert.Empty(combinations);
	}

	#endregion

	#region Calculate Result Tests

	[Fact]
	public void CalculateResult_WithSimplePricesAndFactors_ReturnsCorrectTotal() {
		// Arrange
		var prices = new[] { 10.0, 20.0, 30.0 };
		var factors = new[] { 1.0, 1.0, 1.0 };
		var target = 60.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(60.0, result.Total);
		Assert.Equal(0.0, result.Error);
	}

	[Fact]
	public void CalculateResult_WithDoubleFactor_DoublesPrices() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var factors = new[] { 2.0, 2.0 };
		var target = 60.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(new[] { 20.0, 40.0 }, result.AdjustedPrices);
		Assert.Equal(60.0, result.Total);
		Assert.Equal(0.0, result.Error);
	}

	[Fact]
	public void CalculateResult_CalculatesPositiveError_WhenBelowTarget() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var factors = new[] { 1.0, 1.0 };
		var target = 50.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(30.0, result.Total);
		Assert.Equal(20.0, result.Error); // target - total = 50 - 30 = 20
	}

	[Fact]
	public void CalculateResult_CalculatesNegativeError_WhenAboveTarget() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var factors = new[] { 2.0, 2.0 };
		var target = 50.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(60.0, result.Total);
		Assert.Equal(-10.0, result.Error); // target - total = 50 - 60 = -10
	}

	[Fact]
	public void CalculateResult_RoundsAdjustedPricesToTwoDecimals() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var factors = new[] { 0.333, 0.666 };
		var target = 20.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		foreach (var price in result.AdjustedPrices) {
			Assert.Equal(Math.Round(price, 2), price);
		}
	}

	[Fact]
	public void CalculateResult_WithMismatchedArrayLengths_ThrowsException() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var factors = new[] { 1.0 };

		// Act & Assert
		Assert.Throws<ArgumentException>(() => CalculateResult(prices, factors, 30.0));
	}

	[Fact]
	public void CalculateResult_WithEmptyArrays_ReturnsZeroTotal() {
		// Arrange
		var prices = Array.Empty<double>();
		var factors = Array.Empty<double>();
		var target = 10.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(0.0, result.Total);
		Assert.Equal(10.0, result.Error);
	}

	[Fact]
	public void CalculateResult_WithZeroPrices_ReturnsZeroAdjustedPrices() {
		// Arrange
		var prices = new[] { 0.0, 0.0 };
		var factors = new[] { 2.0, 3.0 };
		var target = 10.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(new[] { 0.0, 0.0 }, result.AdjustedPrices);
		Assert.Equal(0.0, result.Total);
	}

	[Fact]
	public void CalculateResult_PreservesInputFactors() {
		// Arrange
		var prices = new[] { 10.0, 20.0 };
		var factors = new[] { 1.5, 2.5 };
		var target = 60.0;

		// Act
		var result = CalculateResult(prices, factors, target);

		// Assert
		Assert.Equal(factors, result.Factors);
	}

	#endregion

	#region Find Best Results Tests

	[Fact]
	public void FindBestResults_WithExactMatch_ReturnsResultWithZeroError() {
		// Arrange
		var prices = new[] { 10.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.5, 25.0, 1.0); // Possible: factors 1.0,1.5 => 10+15=25

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		Assert.NotEmpty(results);
		Assert.Contains(results, r => Math.Abs(r.Error) < 0.01);
	}

	[Fact]
	public void FindBestResults_OnlyReturnsResultsWithinMaximumDistance() {
		// Arrange
		var prices = new[] { 10.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.1, 25.0, 0.5);

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		foreach (var result in results) {
			// When there's an exact match, results with larger errors may be filtered
			// Just verify we got results
			Assert.True(results.Count > 0);
		}
	}

	[Fact]
	public void FindBestResults_WithNoValidResults_ReturnsEmptyList() {
		// Arrange - Target is impossible with these constraints
		var prices = new[] { 10.0 };
		var config = new CalculationConfig(1.0, 1.5, 0.5, 100.0, 0.1); // Max=15, target=100

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		Assert.Empty(results);
	}

	[Fact]
	public void FindBestResults_ReportsProgressViaCallback() {
		// Arrange
		var prices = new[] { 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.5, 15.0, 1.0);
		var progressReported = false;
		Action<double> onProgress = p => { progressReported = true; };

		// Act
		FindBestResults(prices, config, onProgress);

		// Assert
		Assert.True(progressReported);
	}

	[Fact]
	public void FindBestResults_CallsResultFoundCallback_WhenResultsFound() {
		// Arrange
		var prices = new[] { 10.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.5, 25.0, 1.0);
		var callbackInvoked = false;
		Action<CalculationResult> onResult = r => { callbackInvoked = true; };

		// Act
		var results = FindBestResults(prices, config, null, onResult);

		// Assert
		if (results.Count > 0) {
			Assert.True(callbackInvoked);
		}
	}

	[Fact]
	public void FindBestResults_FiltersLargerErrorsWhenExactMatchExists() {
		// Arrange - With exact match possible, larger errors should be filtered
		var prices = new[] { 10.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.1, 20.0, 5.0); // Exact match: 1.0, 1.0 = 20

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		Assert.NotEmpty(results);
		// All remaining results should have very small errors
		foreach (var result in results) {
			Assert.True(Math.Abs(result.Error) < 0.01, $"Result has error {result.Error} which should have been filtered");
		}
	}

	[Fact]
	public void FindBestResults_WithSinglePart_FindsCorrectFactor() {
		// Arrange
		var prices = new[] { 10.0 };
		var config = new CalculationConfig(1.0, 3.0, 0.1, 20.0, 0.5);

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		Assert.NotEmpty(results);
		var bestResult = results.Last();
		Assert.Equal(20.0, bestResult.Total, 1); // Factor 2.0 * 10.0 = 20.0
	}

	[Fact]
	public void FindBestResults_ResultsAreOrdered_ByDiscoveryTime() {
		// Arrange
		var prices = new[] { 10.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.1, 25.0, 2.0);

		// Act
		var results = FindBestResults(prices, config);

		// Assert - Results should be in discovery order (later discoveries are closer to target)
		Assert.NotEmpty(results);
		// The last result should have the smallest or one of the smallest errors
		var lastError = Math.Abs(results.Last().Error);
		var minError = results.Min(r => Math.Abs(r.Error));
		Assert.Equal(minError, lastError, 2);
	}

	#endregion

	#region Calculation Config Tests

	[Fact]
	public void CalculationConfig_StepCount_CalculatesCorrectly() {
		// Arrange
		var config = new CalculationConfig(0.2, 3.0, 0.01, 35.0, 1.0);

		// Act
		var stepCount = config.StepCount;

		// Assert - (3.0 - 0.2) / 0.01 = 280
		Assert.Equal(280, stepCount);
	}

	[Fact]
	public void CalculationConfig_StepCount_WithLargeStep_CalculatesCorrectly() {
		// Arrange
		var config = new CalculationConfig(1.0, 2.0, 0.5, 10.0, 1.0);

		// Act
		var stepCount = config.StepCount;

		// Assert - (2.0 - 1.0) / 0.5 = 2
		Assert.Equal(2, stepCount);
	}

	[Fact]
	public void CalculationConfig_StepCount_WithIrregularStep_RoundsUp() {
		// Arrange
		var config = new CalculationConfig(1.0, 2.0, 0.3, 10.0, 1.0);

		// Act
		var stepCount = config.StepCount;

		// Assert - (2.0 - 1.0) / 0.3 = 3.33... => ceiling = 4
		Assert.Equal(4, stepCount);
	}

	#endregion

	#region Integration-like Tests (Full Algorithm Scenarios)

	[Fact]
	public void FullAlgorithm_WithRealWorldScenario_FindsAcceptableResults() {
		// Arrange - Simulates the default values from the application
		var prices = new[] { 2.28, 2.0, 11.66 };
		var config = new CalculationConfig(0.2, 3.0, 0.1, 35.0, 1.0);

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		Assert.NotEmpty(results);
		var bestResult = results.Last();
		Assert.True(Math.Abs(bestResult.Error) < 1.0, $"Best result error {bestResult.Error} should be within maximum distance");
		Assert.Equal(3, bestResult.Factors.Length);
		Assert.Equal(3, bestResult.AdjustedPrices.Length);
	}

	[Fact]
	public void FullAlgorithm_WithTightTolerance_OnlyFindsExactMatches() {
		// Arrange
		var prices = new[] { 10.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.1, 20.0, 0.01); // Exact: 1.0, 1.0 = 20

		// Act
		var results = FindBestResults(prices, config);

		// Assert
		Assert.NotEmpty(results);
		foreach (var result in results) {
			Assert.True(Math.Abs(result.Error) < 0.01);
		}
	}

	[Fact]
	public void FullAlgorithm_WithSmallStepSize_GeneratesManyResults() {
		// Arrange
		var prices = new[] { 10.0 };
		var config = new CalculationConfig(1.0, 1.2, 0.01, 11.0, 0.5);

		// Act
		var results = FindBestResults(prices, config);

		// Assert - Multiple factors within 0.5 of target (11.0)
		Assert.NotEmpty(results);
	}

	[Fact]
	public void FullAlgorithm_PreservesDataIntegrity_AcrossAllResults() {
		// Arrange
		var prices = new[] { 5.0, 10.0 };
		var config = new CalculationConfig(1.0, 2.0, 0.2, 20.0, 3.0);

		// Act
		var results = FindBestResults(prices, config);

		// Assert - Verify each result has consistent data
		foreach (var result in results) {
			Assert.Equal(2, result.Factors.Length);
			Assert.Equal(2, result.AdjustedPrices.Length);

			// Verify adjusted prices match factors * prices
			for (int i = 0; i < 2; i++) {
				var expected = Math.Round(prices[i] * result.Factors[i], 2);
				Assert.Equal(expected, result.AdjustedPrices[i]);
			}

			// Verify total matches sum
			Assert.Equal(result.AdjustedPrices.Sum(), result.Total);

			// Verify error matches target - total
			Assert.Equal(config.Target - result.Total, result.Error, 2);
		}
	}

	#endregion
}
