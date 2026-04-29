namespace NCoreUtils.Data.Builders;

/// <summary>
/// Represents a method that defines a set of criteria and determines whether the specified item meets those criteria.
/// </summary>
/// <typeparam name="T">The type of the item to compare, which must be a struct.</typeparam>
/// <param name="item">The item to compare.</param>
/// <returns><c>true</c> if <paramref name="item"/> meets the criteria; otherwise, <c>false</c>.</returns>
public delegate bool RefListFindDelegate<T>(in T item) where T : struct;