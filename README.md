## Builder fields

### Special types

By default builder field has special type when source type:
* is an `IReadOnlyList` when element type is `int` → `List<int>`
* is an `IReadOnlyList` when element type is `string` → `List<string>`
* is an `IReadOnlyList` when element type is a reference type → `RefList<TBuilder>` where `TBuilder` is a builder
  of the element type.

### Simple types

For simple types (`string` and value types including `Nullable<T>`) by default builder field has samee type as source.
In case of string the builder field is always nullable (i.e. `string?`).

