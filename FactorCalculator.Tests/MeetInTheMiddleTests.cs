using Xunit;
using FactorCalculator;
using static FactorCalculator.FactorCalculationService;

namespace FactorCalculator.Tests;

/// <summary>
/// Tests specifically for the Meet-in-the-Middle algorithm implementation.
/// </summary>
public class MeetInTheMiddleTests {
        [Fact]
        public void MeetInTheMiddle_ProducesSimilarResultsAsBruteForce() {
                // Arrange - Use a scenario where both algorithms should find results
                var prices = new[] { 10.0, 10.0, 10.0 };
                var bruteForceConfig = new CalculationConfig(1.0, 2.0, 0.2, 36.0, 2.0, CalculationAlgorithm.BruteForce);
                var meetInMiddleConfig = new CalculationConfig(1.0, 2.0, 0.2, 36.0, 2.0, CalculationAlgorithm.MeetInTheMiddle);

                // Act
                var bruteForceResults = FindBestResults(prices, bruteForceConfig);
                var meetInMiddleResults = FindBestResults(prices, meetInMiddleConfig);

                // Assert - Both should find results
                Assert.NotEmpty(bruteForceResults);
                Assert.NotEmpty(meetInMiddleResults);

                // The best result (last one) should have similar quality
                var bfBest = bruteForceResults.Last();
                var mimBest = meetInMiddleResults.Last();
                Assert.True(Math.Abs(bfBest.Error) < 1.0);
                Assert.True(Math.Abs(mimBest.Error) < 1.0);
        }

        [Fact]
        public void MeetInTheMiddle_WithSinglePart_FallsBackToBruteForce() {
                // Arrange - Single part should use brute force internally
                var prices = new[] { 10.0 };
                var config = new CalculationConfig(1.0, 3.0, 0.1, 20.0, 0.5, CalculationAlgorithm.MeetInTheMiddle);

                // Act
                var results = FindBestResults(prices, config);

                // Assert
                Assert.NotEmpty(results);
                var bestResult = results.Last();
                Assert.Equal(20.0, bestResult.Total, 1);
        }

        [Fact]
        public void MeetInTheMiddle_WithTwoParts_FallsBackToBruteForce() {
                // Arrange - Two parts should use brute force internally
                var prices = new[] { 10.0, 10.0 };
                var config = new CalculationConfig(1.0, 2.0, 0.1, 25.0, 1.0, CalculationAlgorithm.MeetInTheMiddle);

                // Act
                var results = FindBestResults(prices, config);

                // Assert
                Assert.NotEmpty(results);
        }

        [Fact]
        public void MeetInTheMiddle_WithFourParts_FindsValidResults() {
                // Arrange - Four parts is where meet-in-the-middle shines
                var prices = new[] { 5.0, 10.0, 15.0, 20.0 };
                var config = new CalculationConfig(0.5, 1.5, 0.1, 50.0, 2.0, CalculationAlgorithm.MeetInTheMiddle);

                // Act
                var results = FindBestResults(prices, config);

                // Assert
                Assert.NotEmpty(results);
                var bestResult = results.Last();
                Assert.Equal(4, bestResult.Factors.Length);
                Assert.True(Math.Abs(bestResult.Error) < 2.0);
        }

        [Fact]
        public void MeetInTheMiddle_PreservesDataIntegrity() {
                // Arrange
                var prices = new[] { 5.0, 10.0, 15.0 };
                var config = new CalculationConfig(1.0, 2.0, 0.2, 40.0, 3.0, CalculationAlgorithm.MeetInTheMiddle);

                // Act
                var results = FindBestResults(prices, config);

                // Assert
                foreach (var result in results) {
                        Assert.Equal(3, result.Factors.Length);
                        Assert.Equal(3, result.AdjustedPrices.Length);

                        // Verify adjusted prices match factors * prices
                        for (int i = 0; i < 3; i++) {
                                var expected = Math.Round(prices[i] * result.Factors[i], 2);
                                Assert.Equal(expected, result.AdjustedPrices[i]);
                        }

                        // Verify total matches sum
                        Assert.Equal(result.AdjustedPrices.Sum(), result.Total);
                }
        }

        [Fact]
        public void AlgorithmSelection_DefaultsToBruteForce() {
                // Arrange - Config without explicit algorithm
                var config = new CalculationConfig(1.0, 2.0, 0.1, 20.0, 1.0);

                // Assert
                Assert.Equal(CalculationAlgorithm.BruteForce, config.Algorithm);
        }

        [Fact]
        public void MeetInTheMiddle_ReportsProgressViaCallback() {
                // Arrange
                var prices = new[] { 10.0, 10.0, 10.0 };
                var config = new CalculationConfig(1.0, 2.0, 0.2, 30.0, 2.0, CalculationAlgorithm.MeetInTheMiddle);
                var progressReported = false;
                Action<double> onProgress = p => { progressReported = true; };

                // Act
                FindBestResults(prices, config, onProgress);

                // Assert
                Assert.True(progressReported);
        }

        [Fact]
        public void MeetInTheMiddle_WithExactMatch_FindsIt() {
                // Arrange - With exact match possible
                var prices = new[] { 10.0, 10.0, 10.0 };
                var config = new CalculationConfig(1.0, 2.0, 0.1, 30.0, 0.01, CalculationAlgorithm.MeetInTheMiddle);

                // Act
                var results = FindBestResults(prices, config);

                // Assert - Should find the exact match (all factors = 1.0)
                Assert.NotEmpty(results);
                Assert.Contains(results, r => Math.Abs(r.Error) < 0.01);
        }
}
