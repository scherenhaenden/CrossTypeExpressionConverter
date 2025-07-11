namespace CrossTypeExpressionConverter.Tests.Helpers.Models.Randomized
{
    public class SourceModelRandom
    {
        public int OriginalId { get; set; }
        public string? OriginalName { get; set; }
        public double OriginalValue { get; set; }
        public DateTime OriginalDate { get; set; }
        public bool OriginalFlag { get; set; }
        public Guid OriginalGuid { get; set; }
        public decimal OriginalAmount { get; set; }
        public int? NullableInt { get; set; }

        /// <summary>
        /// Returns a formatted string summarizing all property values of the <c>SourceModelRandom</c> instance for debugging purposes.
        /// </summary>
        /// <returns>A string representation of the object's property values, including nullability information.</returns>
        public override string ToString()
        {
            return $"SourceModelRandom(Id={OriginalId}, Name='{OriginalName}', Value={OriginalValue}, Date={OriginalDate:yyyy-MM-dd HH:mm:ss}, Flag={OriginalFlag}, Guid={OriginalGuid}, Amount={OriginalAmount}, NullableInt={NullableInt?.ToString() ?? "null"})";
        }
    }
}

// File: CrossTypeExpressionConverter.Tests/Helpers/Models/Randomized/DestinationModelRandomMapped.cs
namespace CrossTypeExpressionConverter.Tests.Helpers.Models.Randomized
{
    public class DestinationModelRandomMapped
    {
        public int MappedEntityId { get; set; }       // from OriginalId
        public string? MappedFullName { get; set; }   // from OriginalName
        public double MappedNumericData { get; set; } // from OriginalValue
        public DateTime MappedTimestamp { get; set; } // from OriginalDate
        public bool MappedIsEnabled { get; set; }     // from OriginalFlag
        public Guid MappedUniqueId { get; set; }      // from OriginalGuid
        public decimal MappedTransactionValue { get; set; } // from OriginalAmount
        public int? MappedOptionalNumber { get; set; } // from NullableInt

        /// <summary>
        /// Returns a formatted string summarizing the property values of the destination model for debugging purposes.
        /// </summary>
        /// <returns>A string representation of the <c>DestinationModelRandomMapped</c> instance, including all property values.</returns>
        public override string ToString()
        {
            return $"DestinationModelRandomMapped(EntityId={MappedEntityId}, FullName='{MappedFullName}', NumericData={MappedNumericData}, Timestamp={MappedTimestamp:yyyy-MM-dd HH:mm:ss}, IsEnabled={MappedIsEnabled}, UniqueId={MappedUniqueId}, TransactionValue={MappedTransactionValue}, OptionalNumber={MappedOptionalNumber?.ToString() ?? "null"})";
        }
    }
}