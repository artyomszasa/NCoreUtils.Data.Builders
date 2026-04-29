namespace NCoreUtils.Data.Builders;

/// <summary>
/// Represents a method that updates a builder of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the builder, which must be a struct.</typeparam>
/// <param name="builder">A reference to the builder to update.</param>
public delegate void UpdateDelegate<T>(ref T builder) where T : struct;