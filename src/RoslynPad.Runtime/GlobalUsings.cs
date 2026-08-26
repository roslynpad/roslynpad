#if !NET9_0_OR_GREATER
// System.Threading.Lock was introduced in .NET 9; alias it to object on older targets
// so lock statements can use a single field type across all target frameworks.
global using Lock = System.Object;
#endif
