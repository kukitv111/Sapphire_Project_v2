namespace Sapphire.Shared.Kernel.ValueObjects;

/// <summary>
/// Base class for value objects in DDD.
/// Value objects are immutable and compared by their properties, not identity.
/// </summary>
public abstract record ValueObject
{
    protected ValueObject() { }

    /// <summary>
    /// Validates the value object state.
    /// Override to implement custom validation logic.
    /// </summary>
    protected virtual void Validate()
    {
    }

    /// <summary>
    /// Creates a validated value object.
    /// Calls Validate() to ensure invariants are maintained.
    /// </summary>
    protected static T Create<T>(Func<T> factory) where T : ValueObject
    {
        var valueObject = factory();
        valueObject.Validate();
        return valueObject;
    }
}
