using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Collections.Immutable;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Provides functionality for generating and comparing public API listings.
/// </summary>
internal static class PublicApiListing
{
    /// <summary>
    /// Gets the path to the checked-in public API listing file.
    /// </summary>
    public static string ListingPath => Path.Combine(PackageArtifacts.RepositoryRoot, "src", "WeatherParser.Parsing", "PublicApi.txt");

    /// <summary>
    /// Generates a public API listing for the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly to analyze.</param>
    /// <param name="namespacePrefix">If not null, only types whose namespace equals this value or starts with <paramref name="namespacePrefix"/> + "." are included.</param>
    /// <returns>A sorted list of public API entries.</returns>
    public static IReadOnlyList<string> Generate(string assemblyPath, string? namespacePrefix = null)
    {
        var result = new List<string>();
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadataReader = peReader.GetMetadataReader();

        var typeDefinitions = metadataReader.TypeDefinitions;
        foreach (var typeDefHandle in typeDefinitions)
        {
            var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
            if (!IsExported(metadataReader, typeDef)) continue;

            var ns = GetNamespace(metadataReader, typeDef);
            if (namespacePrefix is not null && ns != namespacePrefix && !ns.StartsWith(namespacePrefix + ".", StringComparison.Ordinal)) continue;

            var fullName = GetFullName(metadataReader, typeDef);
            result.Add($"T:{fullName}");
            AddMethods(result, metadataReader, typeDef);
            AddFields(result, metadataReader, typeDef);
        }

        return result.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    private static bool IsExported(MetadataReader reader, TypeDefinition typeDef)
    {
        var visibility = typeDef.Attributes & TypeAttributes.VisibilityMask;
        var declaring = typeDef.GetDeclaringType();
        if (declaring.IsNil)
        {
            return visibility == TypeAttributes.Public;
        }

        var nestedVisible = visibility == TypeAttributes.NestedPublic
            || visibility == TypeAttributes.NestedFamily
            || visibility == TypeAttributes.NestedFamORAssem;
        return nestedVisible && IsExported(reader, reader.GetTypeDefinition(declaring));
    }

    private static string GetNamespace(MetadataReader reader, TypeDefinition typeDef)
    {
        var declaring = typeDef.GetDeclaringType();
        return declaring.IsNil ? reader.GetString(typeDef.Namespace) : GetNamespace(reader, reader.GetTypeDefinition(declaring));
    }

    private static string GetFullName(MetadataReader reader, TypeDefinition typeDef)
    {
        var name = reader.GetString(typeDef.Name);
        var namespaceName = reader.GetString(typeDef.Namespace);
        var declaringType = typeDef.GetDeclaringType();
        if (declaringType.IsNil)
        {
            return string.IsNullOrEmpty(namespaceName) ? name : $"{namespaceName}.{name}";
        }

        var declaringTypeDef = reader.GetTypeDefinition(declaringType);
        var declaringFullName = GetFullName(reader, declaringTypeDef);
        return $"{declaringFullName}.{name}";
    }

    private static void AddMethods(List<string> result, MetadataReader reader, TypeDefinition typeDef)
    {
        foreach (var methodHandle in typeDef.GetMethods())
        {
            var methodDef = reader.GetMethodDefinition(methodHandle);
            var access = methodDef.Attributes & MethodAttributes.MemberAccessMask;
            if (access == MethodAttributes.Public ||
                access == MethodAttributes.Family ||
                access == MethodAttributes.FamORAssem)
            {
                var typeName = GetFullName(reader, typeDef);
                var methodName = reader.GetString(methodDef.Name);
                var signature = methodDef.DecodeSignature(new TypeNameProvider(reader), null);
                var parameters = string.Join(", ", signature.ParameterTypes.Select(p => p));
                var returnType = signature.ReturnType;
                result.Add($"M:{typeName}.{methodName}({parameters}) : {returnType}");
            }
        }
    }

    private static void AddFields(List<string> result, MetadataReader reader, TypeDefinition typeDef)
    {
        foreach (var fieldHandle in typeDef.GetFields())
        {
            var fieldDef = reader.GetFieldDefinition(fieldHandle);
            var access = fieldDef.Attributes & FieldAttributes.FieldAccessMask;
            if (access == FieldAttributes.Public ||
                access == FieldAttributes.Family ||
                access == FieldAttributes.FamORAssem)
            {
                var typeName = GetFullName(reader, typeDef);
                var fieldName = reader.GetString(fieldDef.Name);
                var fieldType = fieldDef.DecodeSignature(new TypeNameProvider(reader), null);
                result.Add($"F:{typeName}.{fieldName} : {fieldType}");
            }
        }
    }

    /// <summary>
    /// Compares two public API listings.
    /// </summary>
    /// <param name="expected">The expected listing.</param>
    /// <param name="actual">The actual listing.</param>
    /// <returns>A tuple of added and removed entries.</returns>
    public static (IReadOnlyList<string> Added, IReadOnlyList<string> Removed) Compare(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var expectedSet = new HashSet<string>(expected);
        var actualSet = new HashSet<string>(actual);

        var added = actualSet.Except(expectedSet).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var removed = expectedSet.Except(actualSet).OrderBy(s => s, StringComparer.Ordinal).ToList();

        return (added, removed);
    }

    /// <summary>
    /// Reads the public API listing from a file.
    /// </summary>
    /// <param name="path">The path to the listing file.</param>
    /// <returns>The lines of the file, trimmed and without empty lines.</returns>
    public static IReadOnlyList<string> ReadListing(string path)
    {
        if (!File.Exists(path))
            return Array.Empty<string>();

        return File.ReadAllLines(path)
            .Select(l => l.TrimEnd())
            .Where(l => !string.IsNullOrEmpty(l))
            .ToList();
    }

    private sealed class TypeNameProvider : ISignatureTypeProvider<string, object?>
    {
        private readonly MetadataReader _reader;

        public TypeNameProvider(MetadataReader reader)
        {
            _reader = reader;
        }

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            return elementType + "[" + new string(',', shape.Rank - 1) + "]";
        }

        public string GetByReferenceType(string elementType)
        {
            return elementType + '&';
        }

        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            return "fnptr";
        }

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            return genericType + "<" + string.Join(", ", typeArguments) + ">";
        }

        public string GetGenericMethodParameter(object? genericContext, int index)
        {
            return "!!" + index;
        }

        public string GetGenericTypeParameter(object? genericContext, int index)
        {
            return "!" + index;
        }

        public string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired)
        {
            return unmodifiedType;
        }

        public string GetPinnedType(string elementType)
        {
            return elementType;
        }

        public string GetPointerType(string elementType)
        {
            return elementType + '*';
        }

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return typeCode switch
            {
                PrimitiveTypeCode.Boolean => "bool",
                PrimitiveTypeCode.Byte => "byte",
                PrimitiveTypeCode.SByte => "sbyte",
                PrimitiveTypeCode.Char => "char",
                PrimitiveTypeCode.Double => "double",
                PrimitiveTypeCode.Int16 => "short",
                PrimitiveTypeCode.UInt16 => "ushort",
                PrimitiveTypeCode.Int32 => "int",
                PrimitiveTypeCode.UInt32 => "uint",
                PrimitiveTypeCode.Int64 => "long",
                PrimitiveTypeCode.UInt64 => "ulong",
                PrimitiveTypeCode.Single => "float",
                PrimitiveTypeCode.String => "string",
                PrimitiveTypeCode.Object => "object",
                PrimitiveTypeCode.Void => "void",
                PrimitiveTypeCode.IntPtr => "nint",
                PrimitiveTypeCode.UIntPtr => "nuint",
                PrimitiveTypeCode.TypedReference => "System.TypedReference",
                _ => throw new NotSupportedException($"Unsupported primitive type code: {typeCode}")
            };
        }

        public string GetSZArrayType(string elementType)
        {
            return elementType + "[]";
        }

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return GetFullName(reader, reader.GetTypeDefinition(handle));
        }

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var typeRef = reader.GetTypeReference(handle);
            var name = reader.GetString(typeRef.Name);
            if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                return GetTypeFromReference(reader, (TypeReferenceHandle)typeRef.ResolutionScope, rawTypeKind) + "." + name;
            }

            var namespaceName = reader.GetString(typeRef.Namespace);
            return string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
        }

        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }
    }
}
