using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Collections.Immutable;
using System.Globalization;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Provides functionality for generating and comparing public API listings.
/// </summary>
internal sealed class PublicApiListing : IPublicApiListing
{
    /// <summary>
    /// Gets the path to the checked-in public API listing file.
    /// </summary>
    public string ListingPath => Path.Combine(PackageArtifacts.RepositoryRoot, "src", "WeatherParser.Parsing", "PublicApi.txt");

    /// <summary>
    /// Generates a public API listing for the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly to analyze.</param>
    /// <param name="namespacePrefix">If not null, only types whose namespace equals this value or starts with <paramref name="namespacePrefix"/> + "." are included.</param>
    /// <returns>A sorted list of public API entries.</returns>
    public IReadOnlyList<string> Generate(string assemblyPath, string? namespacePrefix = null)
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
            result.Add(FormatType(metadataReader, typeDef, fullName));
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

    private static string FormatType(MetadataReader reader, TypeDefinition typeDef, string fullName)
    {
        var sb = new StringBuilder();
        sb.Append("T:");
        sb.Append(fullName);

        var genericParams = typeDef.GetGenericParameters();
        if (genericParams.Count > 0)
        {
            sb.Append('<');
            for (int i = 0; i < genericParams.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatGenericParameter(reader, genericParams[i]));
            }
            sb.Append('>');
        }

        var typeKind = "class";
        if (typeDef.Attributes.HasFlag(TypeAttributes.Interface))
        {
            typeKind = "interface";
        }
        else
        {
            var baseType = typeDef.BaseType;
            if (!baseType.IsNil)
            {
                var baseTypeName = FormatTypeReference(reader, baseType);
                if (baseTypeName == "System.Enum")
                {
                    typeKind = "enum";
                }
                else if (baseTypeName == "System.ValueType")
                {
                    typeKind = "struct";
                }
                else if (baseTypeName == "System.MulticastDelegate")
                {
                    typeKind = "delegate";
                }
            }
        }

        sb.Append(" (");
        sb.Append(typeKind);

        var isAbstract = typeDef.Attributes.HasFlag(TypeAttributes.Abstract);
        var isSealed = typeDef.Attributes.HasFlag(TypeAttributes.Sealed);
        if (typeKind == "class")
        {
            if (isAbstract && isSealed)
            {
                sb.Append(" static");
            }
            else if (isAbstract)
            {
                sb.Append(" abstract");
            }
            else if (isSealed)
            {
                sb.Append(" sealed");
            }
        }

        sb.Append(')');

        var baseList = new List<string>();
        if (!typeDef.BaseType.IsNil)
        {
            baseList.Add(FormatTypeReference(reader, typeDef.BaseType));
        }

        var interfaceNames = new List<string>();
        foreach (var impl in typeDef.GetInterfaceImplementations())
        {
            interfaceNames.Add(FormatTypeReference(reader, reader.GetInterfaceImplementation(impl).Interface));
        }

        baseList.AddRange(interfaceNames.OrderBy(s => s, StringComparer.Ordinal));
        if (baseList.Count > 0)
        {
            sb.Append(" : ");
            sb.Append(string.Join(", ", baseList));
        }

        return sb.ToString();
    }

    private static string FormatTypeReference(MetadataReader reader, EntityHandle typeHandle)
    {
        if (typeHandle.Kind == HandleKind.TypeDefinition)
        {
            var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)typeHandle);
            return GetFullName(reader, typeDef);
        }
        else if (typeHandle.Kind == HandleKind.TypeReference)
        {
            var typeRef = reader.GetTypeReference((TypeReferenceHandle)typeHandle);
            var name = reader.GetString(typeRef.Name);
            var namespaceName = reader.GetString(typeRef.Namespace);
            if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                return FormatTypeReference(reader, (EntityHandle)typeRef.ResolutionScope) + "." + name;
            }

            return string.IsNullOrEmpty(namespaceName) ? name : namespaceName + "." + name;
        }
        else
        {
            // For other types, fall back to the default provider logic
            var typeNameProvider = new TypeNameProvider(reader);
            return reader.GetTypeSpecification((TypeSpecificationHandle)typeHandle).DecodeSignature(typeNameProvider, null);
        }
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
                result.Add(FormatMethod(reader, methodDef, typeDef));
            }
        }
    }

    private static string FormatMethod(MetadataReader reader, MethodDefinition methodDef, TypeDefinition typeDef)
    {
        var sb = new StringBuilder();
        sb.Append("M:");

        var typeName = GetFullName(reader, typeDef);
        sb.Append(typeName);
        sb.Append('.');

        var methodName = reader.GetString(methodDef.Name);
        sb.Append(methodName);

        var genericParams = methodDef.GetGenericParameters();
        if (genericParams.Count > 0)
        {
            sb.Append("``");
            sb.Append(genericParams.Count);
            sb.Append('<');
            for (int i = 0; i < genericParams.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatGenericParameter(reader, genericParams[i]));
            }
            sb.Append('>');
        }

        var signature = methodDef.DecodeSignature(new TypeNameProvider(reader), null);
        sb.Append('(');
        var parameters = methodDef.GetParameters();
        var paramList = new List<string>();
        foreach (var param in parameters)
        {
            var paramDef = reader.GetParameter(param);
            if (paramDef.SequenceNumber == 0) continue; // Skip return value
            var paramType = signature.ParameterTypes[paramDef.SequenceNumber - 1];
            var paramName = reader.GetString(paramDef.Name);
            var sbParam = new StringBuilder();
            sbParam.Append(paramType);
            sbParam.Append(' ');
            sbParam.Append(paramName);
            if (paramDef.Attributes.HasFlag(ParameterAttributes.HasDefault))
            {
                var defaultValue = paramDef.GetDefaultValue();
                if (!defaultValue.IsNil)
                {
                    sbParam.Append(" = ");
                    sbParam.Append(FormatConstant(reader, defaultValue));
                }
            }
            paramList.Add(sbParam.ToString());
        }

        sb.Append(string.Join(", ", paramList));
        sb.Append(") : ");
        sb.Append(signature.ReturnType);

        var modifiers = new List<string>();
        var access = methodDef.Attributes & MethodAttributes.MemberAccessMask;
        if (access == MethodAttributes.Public)
        {
            modifiers.Add("public");
        }
        else if (access == MethodAttributes.Family)
        {
            modifiers.Add("protected");
        }
        else if (access == MethodAttributes.FamORAssem)
        {
            modifiers.Add("protected internal");
        }

        if (methodDef.Attributes.HasFlag(MethodAttributes.Static))
        {
            modifiers.Add("static");
        }

        var isAbstract = methodDef.Attributes.HasFlag(MethodAttributes.Abstract);
        var isVirtual = methodDef.Attributes.HasFlag(MethodAttributes.Virtual);
        var isNewSlot = methodDef.Attributes.HasFlag(MethodAttributes.NewSlot);
        var isFinal = methodDef.Attributes.HasFlag(MethodAttributes.Final);

        if (isAbstract)
        {
            modifiers.Add("abstract");
        }
        else if (isVirtual)
        {
            if (isNewSlot && !isFinal)
            {
                modifiers.Add("virtual");
            }
            else if (!isNewSlot && !isFinal)
            {
                modifiers.Add("override");
            }
            else if (isFinal && !isNewSlot)
            {
                modifiers.Add("sealed override");
            }
        }

        sb.Append(" [");
        sb.Append(string.Join(" ", modifiers));
        sb.Append(']');

        return sb.ToString();
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
                result.Add(FormatField(reader, fieldDef, typeDef));
            }
        }
    }

    private static string FormatField(MetadataReader reader, FieldDefinition fieldDef, TypeDefinition typeDef)
    {
        var sb = new StringBuilder();
        sb.Append("F:");

        var typeName = GetFullName(reader, typeDef);
        sb.Append(typeName);
        sb.Append('.');

        var fieldName = reader.GetString(fieldDef.Name);
        sb.Append(fieldName);

        var fieldType = fieldDef.DecodeSignature(new TypeNameProvider(reader), null);
        sb.Append(" : ");
        sb.Append(fieldType);

        var modifiers = new List<string>();
        var access = fieldDef.Attributes & FieldAttributes.FieldAccessMask;
        if (access == FieldAttributes.Public)
        {
            modifiers.Add("public");
        }
        else if (access == FieldAttributes.Family)
        {
            modifiers.Add("protected");
        }
        else if (access == FieldAttributes.FamORAssem)
        {
            modifiers.Add("protected internal");
        }

        if (fieldDef.Attributes.HasFlag(FieldAttributes.Literal))
        {
            modifiers.Add("const");
        }
        else
        {
            if (fieldDef.Attributes.HasFlag(FieldAttributes.Static))
            {
                modifiers.Add("static");
            }

            if (fieldDef.Attributes.HasFlag(FieldAttributes.InitOnly))
            {
                modifiers.Add("readonly");
            }
        }

        sb.Append(" [");
        sb.Append(string.Join(" ", modifiers));
        sb.Append(']');

        if (fieldDef.Attributes.HasFlag(FieldAttributes.Literal))
        {
            var defaultValue = fieldDef.GetDefaultValue();
            if (!defaultValue.IsNil)
            {
                sb.Append(" = ");
                sb.Append(FormatConstant(reader, defaultValue));
            }
        }

        return sb.ToString();
    }

    private static string FormatGenericParameter(MetadataReader reader, GenericParameterHandle handle)
    {
        // Name, then the constraints in the order class, struct, type constraints, new() (struct implies System.ValueType and new()).
        var parameter = reader.GetGenericParameter(handle);
        var isStruct = (parameter.Attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
        var constraints = new List<string>();
        if ((parameter.Attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
        {
            constraints.Add("class");
        }

        if (isStruct)
        {
            constraints.Add("struct");
        }

        foreach (var constraintHandle in parameter.GetConstraints())
        {
            var constraintType = FormatTypeReference(reader, reader.GetGenericParameterConstraint(constraintHandle).Type);
            if (!(isStruct && constraintType == "System.ValueType"))
            {
                constraints.Add(constraintType);
            }
        }

        if ((parameter.Attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !isStruct)
        {
            constraints.Add("new()");
        }

        var name = reader.GetString(parameter.Name);
        return constraints.Count == 0 ? name : name + ": " + string.Join(", ", constraints);
    }

    private static string FormatConstant(MetadataReader reader, ConstantHandle handle)
    {
        var constant = reader.GetConstant(handle);
        var typeCode = constant.TypeCode;
        var blobReader = reader.GetBlobReader(constant.Value);

        return typeCode switch
        {
            ConstantTypeCode.Boolean => blobReader.ReadBoolean() ? "true" : "false",
            ConstantTypeCode.Char => $"'{blobReader.ReadChar()}'",
            ConstantTypeCode.SByte => blobReader.ReadSByte().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Byte => blobReader.ReadByte().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int16 => blobReader.ReadInt16().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt16 => blobReader.ReadUInt16().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int32 => blobReader.ReadInt32().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt32 => blobReader.ReadUInt32().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int64 => blobReader.ReadInt64().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt64 => blobReader.ReadUInt64().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Single => blobReader.ReadSingle().ToString("R", CultureInfo.InvariantCulture),
            ConstantTypeCode.Double => blobReader.ReadDouble().ToString("R", CultureInfo.InvariantCulture),
            ConstantTypeCode.String => "\"" + blobReader.ReadUTF16(blobReader.Length) + "\"",
            ConstantTypeCode.NullReference => "null",
            _ => throw new NotSupportedException($"Unsupported constant type code: {typeCode}")
        };
    }

    /// <inheritdoc/>
    public (IReadOnlyList<string> Added, IReadOnlyList<string> Removed) Compare(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var expectedSet = new HashSet<string>(expected);
        var actualSet = new HashSet<string>(actual);

        var added = actualSet.Except(expectedSet).OrderBy(s => s, StringComparer.Ordinal).ToList();
        var removed = expectedSet.Except(actualSet).OrderBy(s => s, StringComparer.Ordinal).ToList();

        return (added, removed);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ReadListing(string path)
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
            return isRequired ? unmodifiedType + " modreq(" + modifierType + ")" : unmodifiedType;
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
