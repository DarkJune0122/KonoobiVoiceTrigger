// Author: DarkJune (SoG)
// TODO: Publish as GitHub blob.
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace VoiceTrigger;

// Technical attributes:
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class RequestIdentifierAttribute(string name) : Attribute
{
    public string Name { get; init; } = name;
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class ResponseIdentifierAttribute(string name) : Attribute
{
    public string Name { get; init; } = name;
}

/// <summary>
/// Indicates enums that can potentially list 
/// <see cref="RequestIdentifierAttribute"/>s and <see cref="ResponseIdentifierAttribute"/>s.
/// </summary>
/// <remarks>
/// Introduced as an early-fail system for optimization.
/// </remarks>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
public sealed class IdentifierCollectionAttribute() : Attribute;

/// <summary>
/// Skips checks, related to enum having states with the same values.
/// </summary>
/// <remarks>
/// This can be enforced by custom analyzers later, if needed.
/// </remarks>
//[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
//public sealed class SkipDeduplication : Attribute;

// Annotated enum.
[IdentifierCollection]
public enum TargetEnum : ushort
{
    [RequestIdentifier("RequestIdentifier1")]
    [ResponseIdentifier("ResponseIdentifier1")]
    Identifier1,
    [RequestIdentifier("RequestIdentifier2")]
    [ResponseIdentifier("ResponseIdentifier2")]
    Identifier2,
    [RequestIdentifier("RequestIdentifier3")]
    [ResponseIdentifier("ResponseIdentifier3")]
    Identifier3,
}

// Publicly accessible enum extension.
// Note: you can also do a helper class instead, in case in large projects there will be too many extensions.
//  But for convenience it's a lot easier.
// Note: Extensions are not visible unless you import a namespace with the extension.
//  Sometimes IDE helps - find unimported extensions for you. But sometimes it might not found the extension.
//  If it will be an issue - note from above might be better, be it less convenient.
//  IDEs always finds all the classes, so you are guaranteed to find it.
// Note: Those notes might be removed when I publish this.
public static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetRequestIdentifier<T>(this T e) where T : unmanaged, Enum
    {
        return VTSEnumDescriptor<T>.GetRequestIdentifier(e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetResponseIdentifier<T>(this T e) where T : unmanaged, Enum
    {
        return VTSEnumDescriptor<T>.GetResponseIdentifier(e);
    }
}

// I just had no VTSCSharp installed in my IDE - so had to create VTSException class.
public abstract class VTSException(string message) : Exception(message);
public sealed class VTSNonAnnotatedEnumException(string message) : VTSException(message)
{
    // Constructs and throws the actual exception outside of a hot-path.
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw()
    {
        throw new VTSNonAnnotatedEnumException($"Provided attribute is not annotated with {nameof(RequestIdentifierAttribute)} or {nameof(ResponseIdentifierAttribute)} attributes! Please annotate all enum members with said attributes.");
    }
}
public sealed class VTSIncorrectEnumAnnotationException(string message) : VTSException(message)
{
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Throw<T>() where T : unmanaged, Enum => ThrowMissingIdentifiers(typeof(T));

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMissingIdentifiers(Type type)
    {
        // We assume the devs will not provide non-enum type as an input here.
        // Throwing other exceptions here will make debugging harder.
        throw new VTSIncorrectEnumAnnotationException($"Enum ({type.FullName}) was not annotated correctly with {nameof(RequestIdentifierAttribute)} and {nameof(ResponseIdentifierAttribute)}s! Please, make sure to annotate all enum members with those attributes - no exceptions!");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowContainsDuplicateValues<T>() where T : unmanaged, Enum => ThrowContainsDuplicateValues(typeof(T));

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowContainsDuplicateValues(Type type)
    {
        throw new VTSIncorrectEnumAnnotationException($"Enum ({type.FullName}) contains duplicate values!");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMemberMissingAttributes(FieldInfo info)
    {
        throw new VTSIncorrectEnumAnnotationException($"Enum member ({info.Name}) of enum ({info.DeclaringType?.FullName}) is missing required attributes! Please, make sure to annotate all enum members with {nameof(RequestIdentifierAttribute)} and {nameof(ResponseIdentifierAttribute)}s - no exceptions!");
    }



    //[DoesNotReturn]
    //[MethodImpl(MethodImplOptions.NoInlining)]
    //public static void ThrowNonUniformIndexing<T>() where T : unmanaged, Enum => ThrowNonUniformIndexing(typeof(T));

    //[DoesNotReturn]
    //[MethodImpl(MethodImplOptions.NoInlining)]
    //private static void ThrowNonUniformIndexing(Type type)
    //{
    //    throw new VTSIncorrectEnumAnnotationException($"Enum ({type.FullName}) has gaps between field members!");
    //}
}

// Internal caching mechanism.
internal static class VTSEnumDescriptor<T> where T : unmanaged, Enum
{
    const BindingFlags EnumMemberBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
    enum EnumValueKind : byte
    {
        Byte, Short,
        Int, Long,
    }
    enum EnumMappingKind : byte
    {
        Uniform, // Values are sequential, from 0 to a value.
        Lookup, // Values are stored in a lookup table.
    }
    readonly struct Identifiers(string requestId, string responseId)
    {
        public readonly string RequestID = requestId;
        public readonly string ResponseID = responseId;
    }

    public static bool IsAnnotated { get; }

    static readonly EnumValueKind ValueKind;
    static readonly EnumMappingKind MappingKind;
    static readonly Dictionary<T, Identifiers>? Lookup;
    static readonly Identifiers[]? JumpTable;
    static readonly int JumpTableMinimumValue;

    static VTSEnumDescriptor()
    {
        // Iteration should allocate less memory than allocating an entire attribute.
        // Note: Replace with 'GetAttribute' if you care about actual attribute values.
        if (!typeof(T).CustomAttributes.Any(static a => a.GetType() == typeof(IdentifierCollectionAttribute)))
        {
            IsAnnotated = false;
            return;
        }

        // Caching once, whenever this class with this specific T enum type is used.
        // Static constructor execution is thread-safe.
        // Important! Includes duplicates if they are declared!
        var values = Enum.GetValues<T>();
        if (values.LongLength == 0)
        {
            IsAnnotated = false;
            return;
        }

        // Removes any unannotaged members.
        // Such members can be used for spacing-out enum values.
        // However! It's not recommended to do, as it will break the jump-table optimization.
        // Adding a support just in case.
        var members = typeof(T).GetFields(EnumMemberBindingFlags);
        long length = values.LongLength;
        if (values.LongLength != members.LongLength)
        {
            // TODO: Throw exception.
        }

        long count = 0; // Counts as a head, and as a total amount of valid items;
        for (long i = 0; i < length; i++)
        {
            // Removes unannotated entries and compacts remaining together.
            var member = members[i];
            if (HasAllRequiredAttributes(member.CustomAttributes))
            {
                // Compacts the items.
                if (i != count)
                    members[count] = member;
                count++;
            }
            // Don't move the head if the check failed.
        }
        // Skipped - the entire array is later released to GC.
        //for (long i = count + 1; i < length; i++)
        //{
        //    // Nulls all other references.
        //    members[i] = null!;
        //}

        // Since T is unmanaged type - we can safely use SizeOf here.
        // Note: 'enum A : char' - is read here as Short, since this is what System.Char is.
        int size = Unsafe.SizeOf<T>();
        ValueKind = size switch
        {
            // Impossible in C# runtime.
            // Removing it also should make switch jump-table start from 1...
            // But I'm not taking any chances)
            0 => ThrowInvalidValueSizeException(size),

            1 => EnumValueKind.Byte,
            2 => EnumValueKind.Short,
            3 => ThrowInvalidValueSizeException(size),
            4 => EnumValueKind.Int,
            5 => ThrowInvalidValueSizeException(size),
            6 => ThrowInvalidValueSizeException(size),
            7 => ThrowInvalidValueSizeException(size),
            8 => EnumValueKind.Long,
            _ => ThrowInvalidValueSizeException(size),
        };

        // Constants to do Math.Abs(...); using bitwise operations.
        //const int IntAbs = 0x7FFFFFFF;
        //const uint UIntAbs = 0x7FFFFFFFu;
        //const long LongAbs = 0x7FFFFFFF_FFFFFFFFL;
        //const ulong ULongAbs = 0x7FFFFFFF_FFFFFFFFuL;

        // Checking if we can produce a uniform jump-table.
        // Note: let me know if Unsafe code is prohibited completely.
        // Note: also, this part might probably be simplified with some Unsafe usage, and what not.
        bool isUniform = true;
        long jumpTableOrigin;
        long requiredJumpTableSize;
        switch (ValueKind)
        {
            case EnumValueKind.Byte:
                // Braces here scope temporary variables only to the inside of a switch case block.
                // This allows other cases to have variables with similar names.
                // No idea if it influences the performance - maybe make method allocate a bit more bytes on a stack.
                // But it makes development a ton simpler.
                // I might investigate C# IL code later to reconfirm this.
                // But I believe it's not important here - not a hot-path anyway.
                {
                    byte origin = Unsafe.As<T, byte>(ref values[0]);
                    jumpTableOrigin = origin;

                    // No need to cache length - it doesn't matter for arrays.
                    // IIRC, array length is cached by a compiler. Not the case for lists.
                    byte last = origin;
                    for (long i = 1; i < count; i++)
                    {
                        byte now = Unsafe.As<T, byte>(ref values[i]);
                        int delta = unchecked(now - last);
                        // We check for difference between enum values to be exactly 1.
                        // This will produce a very optimized jump-table.
                        if (delta == 0)
                        {
                            // Duplicate found.
                            // Note: Enum.GetValues() sort values by their *unsigned* raw value/number.
                            // The other will be: [0, 1, 2, 3, ..., -5, -4, -3, -2, -1].
                            // More on this: https://learn.microsoft.com/en-us/dotnet/api/system.enum.getvalues?view=net-10.0
                            VTSIncorrectEnumAnnotationException.ThrowContainsDuplicateValues<T>();
                        }

                        // Last value is updated before the loop quits.
                        // This is important for calculating the jump table size more accurately.
                        last = now;
                        if (delta != 1)
                        {
                            isUniform = false;
                            break;
                        }
                    }

                    // Reuses first and last known values without accessing the array again.
                    requiredJumpTableSize = last - origin + 1;
                }
                break;
            case EnumValueKind.Short:
                {
                    ushort origin = Unsafe.As<T, ushort>(ref values[0]);

                    ushort last = origin;
                    for (long i = 1; i < count; i++)
                    {
                        ushort now = Unsafe.As<T, ushort>(ref values[i]);
                        int delta = unchecked(now - last);
                        if (delta == 0)
                            VTSIncorrectEnumAnnotationException.ThrowContainsDuplicateValues<T>();

                        last = now;
                        if (delta != 1)
                        {
                            isUniform = false;
                            break;
                        }
                    }

                    requiredJumpTableSize = last - origin + 1;
                }
                break;
            case EnumValueKind.Int:
                {
                    uint origin = Unsafe.As<T, uint>(ref values[0]);

                    uint last = origin;
                    for (long i = 1; i < count; i++)
                    {
                        uint now = Unsafe.As<T, uint>(ref values[i]);
                        uint delta = unchecked(now - last);
                        if (delta == 0)
                            VTSIncorrectEnumAnnotationException.ThrowContainsDuplicateValues<T>();

                        last = now;
                        if (delta != 1)
                        {
                            isUniform = false;
                            break;
                        }
                    }

                    requiredJumpTableSize = last - origin + 1u;
                }
                break;
            case EnumValueKind.Long:
                {
                    ulong origin = Unsafe.As<T, ulong>(ref values[0]);

                    ulong last = origin;
                    for (long i = 1; i < count; i++)
                    {
                        ulong now = Unsafe.As<T, ulong>(ref values[i]);
                        ulong delta = unchecked(now - last);
                        if (delta == 0)
                            VTSIncorrectEnumAnnotationException.ThrowContainsDuplicateValues<T>();

                        last = now;
                        if (delta != 1)
                        {
                            isUniform = false;
                            break;
                        }
                    }

                    ulong requirement = last - origin + 1uL;
                    // Values above cannot be mapped to an array jump-table.
                    if (requirement > long.MaxValue)
                    {
                        isUniform = false;
                    }
                    requiredJumpTableSize = (long)requirement;
                }
                break;
            default: ThrowSwitchException(ValueKind); return;
        }

        // When we are here, we know:
        // 1. There is no duplicates.
        // 2. Overall
        if (isUniform)
        {
            MappingKind = EnumMappingKind.Uniform;
            for (long i = 0; i < count; i++)
            {
                var member = members[i];
                if (HasAllRequiredAttributes(member.CustomAttributes))
                {

                }
                else
                {
                    VTSIncorrectEnumAnnotationException.ThrowMemberMissingAttributes(member);
                }
            }
        }
        else
        {
            MappingKind = EnumMappingKind.Lookup;

        }

        // Simplifications:
        static bool HasAllRequiredAttributes(IEnumerable<CustomAttributeData> attributes)
        {
            MissingRequirements requirements = MissingRequirements.Both;
            foreach (var attribute in attributes)
            {
                var type = attribute.GetType();
                switch (requirements)
                {
                    case MissingRequirements.Both:
                        if (type == typeof(RequestIdentifierAttribute))
                        {
                            requirements &= ~MissingRequirements.Request;
                        }
                        else if (type == typeof(ResponseIdentifierAttribute))
                        {
                            requirements &= ~MissingRequirements.Response;
                        }
                        break;
                    case MissingRequirements.Request:
                        if (type == typeof(RequestIdentifierAttribute))
                        {
                            return true;
                        }
                        break;
                    case MissingRequirements.Response:
                        if (type == typeof(ResponseIdentifierAttribute))
                        {
                            return true;
                        }
                        break;
                    case MissingRequirements.None:
                        return true;
                }
            }
            return requirements == MissingRequirements.None;
        }
    }

    enum MissingRequirements : byte
    {
        None,
        Request,
        Response,
        Both,
    }

    // Returns EnumValueKind for usage in a switch expression.
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static EnumValueKind ThrowInvalidValueSizeException(int size)
    {
        // Never hits under normal circumstances.
        throw new SystemException($"VTS Bug! Enum type ({typeof(T)}) has a byte size of ({size}), which is unsupported by the system!");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowSwitchException(EnumValueKind kind) => throw new SwitchExpressionException($"Unhandled value kind:");

    public static string GetRequestIdentifier(T e)
    {
        throw new NotImplementedException();

        // Branch prediction on constant values evaluate to ~5 cycles on modern CPUs iirc.
        // But if branch value has a 50/50 change to change - no prediction happen.
        // Thus - CPU are left with just evaluating the branch. This takes ~15 cycles iirc.
        //if (!IsAnnotated)
        //{
        //    VTSNonAnnotatedEnumException.Throw();
        //}

        //if (IsUniform)
        //{

        //}

    }
    public static string GetResponseIdentifier(T e)
    {
        throw new NotImplementedException();
    }
}