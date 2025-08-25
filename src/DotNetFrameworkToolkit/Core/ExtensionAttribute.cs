using System;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Enables the use of extension methods in projects targeting .NET Framework 2.0 by providing
/// the <c>ExtensionAttribute</c> in the <see cref="System.Runtime.CompilerServices"/> namespace.
/// This allows the C# compiler to recognize methods marked with the <c>this</c> keyword
/// in the first parameter as extension methods, even though this attribute is not present in .NET 2.0 by default.
/// </summary>
/// <remarks>
/// Read more about this technique at <see href="http://www.danielmoth.com/Blog/Using-Extension-Methods-In-Fx-20-Projects.aspx">this blog</see> by Daniel Moth.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class ExtensionAttribute : Attribute { }

//TODO: This works but for parjects that target a version of .NET Framework higher than 2.0, this causes a compiler warning:
// The predefined type 'ExtensionAttribute' is defined in multiple assemblies in the global alias; using definition from 'DotNetFrameworkToolkit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
