namespace NCoreUtils.Data.Builders;

public delegate void UpdateDelegate<T>(ref T builder) where T : struct;