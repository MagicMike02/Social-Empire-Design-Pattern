// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Marker class for C# 9 init-only setters.
	/// Required because Unity targets netstandard2.1, which does not include this type.
	/// This is a standard polyfill — the compiler only checks for the type's existence.
	/// </summary>
	internal static class IsExternalInit
	{
	}
}