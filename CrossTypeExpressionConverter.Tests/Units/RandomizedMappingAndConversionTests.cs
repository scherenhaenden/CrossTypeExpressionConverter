using System.Linq.Expressions;
using CrossTypeExpressionConverter.Tests.Helpers.Models.Randomized;

namespace CrossTypeExpressionConverter.Tests.Units;

[TestFixture]
public class RandomizedMappingAndConversionTests
{
    private static readonly Random Rng = new Random();
    private static readonly string[] PossibleNames = { "Alice", "Bob", "Charlie", "David", "Eve", "Fiona", "George", "Hannah" };

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
            NullableInt = Rng.Next(0,3) == 0 ? (int?)null : Rng.Next(1,100)
        };
    }

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
    
    private bool Evaluate<T>(Expression<Func<T, bool>> predicate, T item)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        if (item == null) throw new ArgumentNullException(nameof(item));
        try
        {
            return predicate.Compile()(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error compiling/evaluating predicate: {ex.Message}");
            Console.WriteLine($"Predicate: {predicate.ToString()}");
            Console.WriteLine($"Item: {item.ToString()}");
            throw;
        }
    }


    [Test]
    [Repeat(10)] // Run this test 10 times with different random data
    public void Convert_WithRandomDataAndFullMapping_ComplexPredicate_ShouldEvaluateCorrectly()
    {
        // 1. Generate a "target" random source model
        var targetSource = GenerateRandomSourceModel();
        Console.WriteLine($"Target Source: {targetSource}");

        // 2. Define the LINQ mapping expression
        Expression<Func<SourceModelRandom, DestinationModelRandomMapped>> mappingExpression = src =>
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
            };

        // 3. Use MappingUtils to build the member map
        var memberMap = MappingUtils.BuildMemberMap(mappingExpression);

        // Assert MappingUtils correctness (basic check)
        Assert.That(memberMap, Is.Not.Null);
        Assert.That(memberMap.Count, Is.EqualTo(8), "MemberMap should contain mappings for all 8 properties.");
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalId)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedEntityId)));
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalName)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedFullName)));
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalValue)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedNumericData)));
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalDate)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedTimestamp)));
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalFlag)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedIsEnabled)));
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalGuid)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedUniqueId)));
        Assert.That(memberMap[nameof(SourceModelRandom.OriginalAmount)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedTransactionValue)));
        Assert.That(memberMap[nameof(SourceModelRandom.NullableInt)], Is.EqualTo(nameof(DestinationModelRandomMapped.MappedOptionalNumber)));


        // 4. Construct a complex source predicate that is TRUE for targetSource
        // We'll use specific values from targetSource to build the predicate
        Expression<Func<SourceModelRandom, bool>> sourcePredicate = s =>
            s.OriginalId == targetSource.OriginalId &&
            (s.OriginalName == targetSource.OriginalName || s.OriginalValue > targetSource.OriginalValue - 10) &&
            s.OriginalFlag == targetSource.OriginalFlag &&
            s.OriginalDate < targetSource.OriginalDate.AddDays(1) &&
            s.OriginalGuid == targetSource.OriginalGuid &&
            s.OriginalAmount >= targetSource.OriginalAmount &&
            (targetSource.NullableInt.HasValue ? s.NullableInt == targetSource.NullableInt.Value : s.NullableInt == null) &&
            s.OriginalValue < (targetSource.OriginalValue + 100.5); // Add some range

        // Verify the source predicate is true for the targetSource (sanity check)
        Assert.IsTrue(Evaluate(sourcePredicate, targetSource), "Source predicate should be true for targetSource.");

        // 5. Convert the predicate
        var convertedPredicate = ExpressionConverter.Convert<SourceModelRandom, DestinationModelRandomMapped>(sourcePredicate, memberMap);
        Assert.That(convertedPredicate, Is.Not.Null);
        Console.WriteLine($"Source Predicate: {sourcePredicate.ToString()}");
        Console.WriteLine($"Converted Predicate: {convertedPredicate.ToString()}");


        // 6. Create the corresponding "target" destination model instance
        var targetDestination = MapSourceToDestination(targetSource);
        Console.WriteLine($"Target Destination: {targetDestination}");

        // 7. Evaluate the converted predicate against the target destination instance
        bool evaluationResult = Evaluate(convertedPredicate, targetDestination);
        Assert.IsTrue(evaluationResult, "Converted predicate should be true for the targetDestination.");

        // 8. Create and test "non-target" destination instances
        var nonTargetDestination1 = MapSourceToDestination(targetSource);
        nonTargetDestination1.MappedEntityId = targetDestination.MappedEntityId + 1; // Change a critical part of the predicate
        Console.WriteLine($"Non-Target Destination 1: {nonTargetDestination1}");
        Assert.IsFalse(Evaluate(convertedPredicate, nonTargetDestination1), "Converted predicate should be false for nonTargetDestination1 (ID changed).");

        var nonTargetDestination2 = MapSourceToDestination(targetSource);
        nonTargetDestination2.MappedIsEnabled = !targetDestination.MappedIsEnabled; // Change another critical part
        Console.WriteLine($"Non-Target Destination 2: {nonTargetDestination2}");
        Assert.IsFalse(Evaluate(convertedPredicate, nonTargetDestination2), "Converted predicate should be false for nonTargetDestination2 (Flag changed).");
        
        if (targetSource.NullableInt.HasValue)
        {
            var nonTargetDestination3 = MapSourceToDestination(targetSource);
            nonTargetDestination3.MappedOptionalNumber = targetSource.NullableInt.Value + 1;
            Console.WriteLine($"Non-Target Destination 3: {nonTargetDestination3}");
            Assert.IsFalse(Evaluate(convertedPredicate, nonTargetDestination3), "Converted predicate should be false for nonTargetDestination3 (NullableInt changed).");
        }
        else // targetSource.NullableInt was null
        {
            var nonTargetDestination3 = MapSourceToDestination(targetSource);
            nonTargetDestination3.MappedOptionalNumber = 12345; // Make it non-null
             Console.WriteLine($"Non-Target Destination 3: {nonTargetDestination3}");
            Assert.IsFalse(Evaluate(convertedPredicate, nonTargetDestination3), "Converted predicate should be false for nonTargetDestination3 (NullableInt changed from null to value).");
        }
    }
}


