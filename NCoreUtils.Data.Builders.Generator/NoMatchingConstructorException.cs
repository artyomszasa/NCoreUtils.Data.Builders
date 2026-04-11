using System;
using System.Collections.Generic;

namespace NCoreUtils.Data;

internal class NoMatchingConstructorException(string target, IReadOnlyList<string> reasons)
    : InvalidOperationException($"No matching constructor for {target}: {string.Join("; ", reasons)}.")
{
    public string Target { get; } = target;

    public IReadOnlyList<string> Reasons { get; } = reasons;
}