#if NET10_0_OR_GREATER
// Sentis 2.6.1 uses the Burst attribute on its CPU jobs. .NET 10 also defines it.
global using SkipLocalsInitAttribute = global::Unity.Burst.CompilerServices.SkipLocalsInitAttribute;
#endif
