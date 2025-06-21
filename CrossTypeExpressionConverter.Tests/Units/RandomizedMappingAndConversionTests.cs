using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers;
using CrossTypeExpressionConverter.Tests.Helpers.Models.Randomized;

namespace CrossTypeExpressionConverter.Tests.Units;

/// <summary>
/// Contains randomized, property-based tests to ensure robust conversion across a variety of data scenarios.
/// </summary>
[TestFixture]
public class RandomizedMappingAndConversionTests
{
    private static readonly Random Rng = new();
    private static readonly string[] PossibleNames = { "Alice", "Bob", "Charlie", "David", "Eve", "Fiona", "George", "Hannah" };

    /// <summary>
    /// Generates a random SourceModelRandom instance with randomized property values.
    /// </summary>
    /// <returns>A new <see cref="SourceModelRandom"/> instance.</returns>
    private SourceModelRandom GenerateRandomSourceModel()
    {
        return new SourceModelRandom
        {
            OriginalId = Rng.Next(1, 10000),
            OriginalName = PossibleNames[Rng.Next(PossibleNames.Length)],
            OriginalValue = Rng.NextDouble() * 1000,
            OriginalDate = DateTime.UtcNow.AddDays(-Rng.Next(0, 365)).AddSeconds(Rng.Next(-100000, 100000)),
            OriginalFlag = Rng.Next(0, 2) == 0,
            OriginalGuid = Guid.NewGuid(),
            OriginalAmount = (decimal)(Rng.NextDouble() * 5000),
            NullableInt = Rng.Next(0, 3) == 0 ? null : Rng.Next(1, 100)
        };
    }

    /// <summary>
    /// Maps a SourceModelRandom instance to a corresponding DestinationModelRandomMapped instance.
    /// </summary>
    /// <param name="source">The source model to map.</param>
    /// <returns>A new <see cref="DestinationModelRandomMapped"/> instance with mapped properties.</returns>
    private DestinationModelRandomMapped MapSourceToDestination(SourceModelRandom source)
    {
        return new DestinationModelRandomMapped
        {
            MappedEntityId = source.OriginalId,
            MappedFullName = source.OriginalName,
            MappedNumericData = source.OriginalValue,
            MappedTimestamp = source.OriginalDate,
            MappedIsEnabled = source.OriginalFlag,
            MappedUniqueId = source.OriginalGuid,
            MappedTransactionValue = source.OriginalAmount,
            MappedOptionalNumber = source.NullableInt
        };
    }
    
    /// <summary>
    /// Compiles and executes a predicate against an item, returning the boolean result.
    /// </summary>


    /// <summary>
    /// Tests the conversion of a complex predicate with fully mapped properties using randomized data.
    /// This test is repeated multiple times to cover a wide range of input values.
    /// </summary>
    [Test]
    [Repeat(10)] // Run this test 10 times with different random data
    public void Convert_WithRandomDataAndFullMapping_ComplexPredicate_ShouldEvaluateCorrectly()
    {
        // 1. Arrange: Generate a "target" random source model and its corresponding destination model.
        var targetSource = GenerateRandomSourceModel();
        var targetDestination = MapSourceToDestination(targetSource);

        // 2. Arrange: Define the LINQ mapping expression and build the member map from it.
        var memberMap = MappingUtils.BuildMemberMap<SourceModelRandom, DestinationModelRandomMapped>(src =>
            new DestinationModelRandomMapped
            {
                MappedEntityId = src.OriginalId,
                MappedFullName = src.OriginalName,
                MappedNumericData = src.OriginalValue,
                MappedTimestamp = src.OriginalDate,
                MappedIsEnabled = src.OriginalFlag,
                MappedUniqueId = src.OriginalGuid,
                MappedTransactionValue = src.OriginalAmount,
                MappedOptionalNumber = src.NullableInt
            });
        
        var options = new ExpressionConverterOptions().WithMemberMap(memberMap);

        // 3. Arrange: Construct a complex source predicate that is guaranteed to be TRUE for the targetSource.
        Expression<Func<SourceModelRandom, bool>> sourcePredicate = s =>
            s.OriginalId == targetSource.OriginalId &&
            (s.OriginalName == targetSource.OriginalName || s.OriginalValue > targetSource.OriginalValue - 10) &&
            s.OriginalFlag == targetSource.OriginalFlag &&
            s.OriginalDate < targetSource.OriginalDate.AddDays(1) &&
            s.OriginalGuid == targetSource.OriginalGuid &&
            s.OriginalAmount >= targetSource.OriginalAmount &&
            s.NullableInt == targetSource.NullableInt &&
            s.OriginalValue < (targetSource.OriginalValue + 100.5);

        // Sanity check: Ensure the original predicate is true for the source object.
        Assert.That(TestUtils.Evaluate(sourcePredicate, targetSource), Is.True, "Source predicate should be true for targetSource.");

        // 4. Act: Convert the predicate.
        var convertedPredicate = ExpressionConverterFacade.Convert<SourceModelRandom, DestinationModelRandomMapped>(sourcePredicate, options);

        // 5. Assert: The converted predicate must be true for the target destination object.
        Assert.That(TestUtils.Evaluate(convertedPredicate, targetDestination), Is.True, "Converted predicate should be true for the targetDestination.");

        // 6. Assert: The converted predicate must be false for non-target objects with altered properties.
        var nonTargetId = MapSourceToDestination(targetSource);
        nonTargetId.MappedEntityId++;
        Assert.That(TestUtils.Evaluate(convertedPredicate, nonTargetId), Is.False, "Predicate should be false when a critical mapped property (ID) is changed.");

        var nonTargetFlag = MapSourceToDestination(targetSource);
        nonTargetFlag.MappedIsEnabled = !targetSource.OriginalFlag;
        Assert.That(TestUtils.Evaluate(convertedPredicate, nonTargetFlag), Is.False, "Predicate should be false when another critical mapped property (Flag) is changed.");
    }
}
