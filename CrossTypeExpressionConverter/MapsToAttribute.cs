namespace CrossTypeExpressionConverter;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MapsToAttribute : Attribute
{
    public string DestinationMemberName { get; }

    public MapsToAttribute(string destinationMemberName)
    {
        DestinationMemberName = destinationMemberName ?? throw new ArgumentNullException(nameof(destinationMemberName));
    }
}
