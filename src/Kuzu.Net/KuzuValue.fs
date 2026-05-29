namespace Kuzu.Net

open System
open System.Runtime.InteropServices
open Kuzu.Net.Native

/// The kind of a result cell, surfaced as a plain enum at the C#-ergonomic
/// boundary (<see cref="KuzuValue.Kind"/>). It is a projection of the
/// <see cref="KuzuValueData"/> discriminated union — the DU is the source of
/// truth; this enum exists so C# callers can branch without pattern-matching.
///
/// Scalar kinds (Bool, Int64, Double, String, Null) are fully decoded into
/// typed <see cref="KuzuValueData"/> cases. Composite and temporal kinds (Node,
/// Rel, List, Struct, …) are not yet structurally decoded: their cells carry
/// Kùzu's string rendering via <see cref="KuzuValue.AsString"/> and report
/// their kind here. See README "Roadmap".
type KuzuValueKind =
    | Null = 0
    | Bool = 1
    | Int64 = 2
    | Double = 3
    | String = 4
    | Node = 5
    | Rel = 6
    | RecursiveRel = 7
    | List = 8
    | Array = 9
    | Struct = 10
    | Map = 11
    | Union = 12
    | Date = 13
    | Timestamp = 14
    | Interval = 15
    | Decimal = 16
    | Blob = 17
    | Uuid = 18
    | InternalId = 19
    | Int128 = 20
    | UInt64 = 21
    | Other = 99

/// The decoded payload of a result cell — the single source of truth for the
/// value layer. F# consumers `match` on this for exhaustive, compiler-checked
/// handling; the C#-ergonomic accessors on <see cref="KuzuValue"/> are a thin
/// projection over it.
///
/// Signed and small unsigned integers (Int8/16/32/64, Serial, UInt8/16/32) are
/// widened losslessly into <c>KInt64</c>; Float and Double into <c>KDouble</c>.
/// Kinds that are not yet structurally decoded (composite, temporal, UInt64,
/// Int128, …) surface as <c>KOther</c> carrying their <see cref="KuzuValueKind"/>;
/// their textual form is available via <see cref="KuzuValue.AsString"/>.
type KuzuValueData =
    | KNull
    | KBool of bool
    | KInt64 of int64
    | KDouble of float
    | KString of string
    | KOther of KuzuValueKind

/// A single cell of a query result row.
///
/// Thin facade over <see cref="KuzuValueData"/>: <see cref="Value"/> exposes the
/// discriminated union for F# pattern-matching, while <see cref="Kind"/> and the
/// <c>AsXxx</c> accessors give C# an idiomatic, non-union surface. A C# caller
/// writes <c>row[0].AsString()</c> / <c>row[0].AsInt64()</c>; an F# caller can
/// instead <c>match row[0].Value with …</c>.
///
/// The typed accessors throw <see cref="InvalidOperationException"/> on a kind
/// mismatch; <see cref="AsString"/> is total and returns Kùzu's own string
/// rendering for any non-null cell (or <c>null</c> when the cell is NULL).
[<Sealed>]
type KuzuValue internal (data: KuzuValueData, rendered: string) as this =

    /// The decoded payload as a discriminated union — for F# pattern-matching.
    member _.Value = data

    /// The kind of this value, projected from <see cref="Value"/>.
    member _.Kind =
        match data with
        | KNull -> KuzuValueKind.Null
        | KBool _ -> KuzuValueKind.Bool
        | KInt64 _ -> KuzuValueKind.Int64
        | KDouble _ -> KuzuValueKind.Double
        | KString _ -> KuzuValueKind.String
        | KOther kind -> kind

    /// True when the underlying Kùzu value is NULL.
    member _.IsNull =
        match data with
        | KNull -> true
        | _ -> false

    /// The value rendered as a string (Kùzu's `kuzu_value_to_string`), or null
    /// when the cell is NULL. Total across every kind.
    member _.AsString() =
        match data with
        | KNull -> null
        | _ -> rendered

    /// The value as a 64-bit integer. Throws when the cell is not an integer
    /// kind (use <see cref="Value"/> to branch without throwing).
    member _.AsInt64() =
        match data with
        | KInt64 v -> v
        | _ -> raise (InvalidOperationException(sprintf "KuzuValue is %A, not Int64." this.Kind))

    /// The value as a double-precision float. Throws when the cell is not a
    /// floating-point kind.
    member _.AsDouble() =
        match data with
        | KDouble v -> v
        | _ -> raise (InvalidOperationException(sprintf "KuzuValue is %A, not Double." this.Kind))

    /// The value as a boolean. Throws when the cell is not a boolean.
    member _.AsBoolean() =
        match data with
        | KBool v -> v
        | _ -> raise (InvalidOperationException(sprintf "KuzuValue is %A, not Bool." this.Kind))

    override _.ToString() =
        match data with
        | KNull -> "null"
        | _ -> rendered

/// Decodes a native `kuzu_value` into a <see cref="KuzuValue"/> facade. Reads
/// from the struct without taking ownership — the caller remains responsible for
/// `kuzu_value_destroy`.
[<RequireQualifiedAccess>]
module internal KuzuValueDecoder =

    /// Maps a native data-type id to the public kind enum. Used for cells that
    /// are not (yet) structurally decoded into a typed `KuzuValueData` case.
    let private kindOfTypeId (id: KuzuDataTypeId) : KuzuValueKind =
        match id with
        | KuzuDataTypeId.Bool -> KuzuValueKind.Bool
        | KuzuDataTypeId.Int8
        | KuzuDataTypeId.Int16
        | KuzuDataTypeId.Int32
        | KuzuDataTypeId.Int64
        | KuzuDataTypeId.Serial
        | KuzuDataTypeId.UInt8
        | KuzuDataTypeId.UInt16
        | KuzuDataTypeId.UInt32 -> KuzuValueKind.Int64
        | KuzuDataTypeId.UInt64 -> KuzuValueKind.UInt64
        | KuzuDataTypeId.Int128 -> KuzuValueKind.Int128
        | KuzuDataTypeId.Double
        | KuzuDataTypeId.Float -> KuzuValueKind.Double
        | KuzuDataTypeId.String -> KuzuValueKind.String
        | KuzuDataTypeId.Node -> KuzuValueKind.Node
        | KuzuDataTypeId.Rel -> KuzuValueKind.Rel
        | KuzuDataTypeId.RecursiveRel -> KuzuValueKind.RecursiveRel
        | KuzuDataTypeId.List -> KuzuValueKind.List
        | KuzuDataTypeId.Array -> KuzuValueKind.Array
        | KuzuDataTypeId.Struct -> KuzuValueKind.Struct
        | KuzuDataTypeId.Map -> KuzuValueKind.Map
        | KuzuDataTypeId.Union -> KuzuValueKind.Union
        | KuzuDataTypeId.Date -> KuzuValueKind.Date
        | KuzuDataTypeId.Timestamp
        | KuzuDataTypeId.TimestampSec
        | KuzuDataTypeId.TimestampMs
        | KuzuDataTypeId.TimestampNs
        | KuzuDataTypeId.TimestampTz -> KuzuValueKind.Timestamp
        | KuzuDataTypeId.Interval -> KuzuValueKind.Interval
        | KuzuDataTypeId.Decimal -> KuzuValueKind.Decimal
        | KuzuDataTypeId.Blob -> KuzuValueKind.Blob
        | KuzuDataTypeId.Uuid -> KuzuValueKind.Uuid
        | KuzuDataTypeId.InternalId -> KuzuValueKind.InternalId
        | _ -> KuzuValueKind.Other

    /// Reads the owned UTF-8 string at `ptr`, freeing it with `kuzu_destroy_string`.
    let private readOwnedString (ptr: nativeint) : string =
        if ptr = IntPtr.Zero then
            null
        else
            let s = Marshal.PtrToStringUTF8 ptr
            NativeMethods.kuzu_destroy_string ptr
            s

    /// Decodes the typed payload of a non-null value of the given kind.
    let private decodeTyped (value: byref<KuzuValueStruct>) (id: KuzuDataTypeId) : KuzuValueData =
        match id with
        | KuzuDataTypeId.Bool ->
            let mutable b = false
            NativeMethods.kuzu_value_get_bool (&value, &b) |> ignore
            KBool b
        | KuzuDataTypeId.Int64
        | KuzuDataTypeId.Serial ->
            let mutable v = 0L
            NativeMethods.kuzu_value_get_int64 (&value, &v) |> ignore
            KInt64 v
        | KuzuDataTypeId.Int32 ->
            let mutable v = 0
            NativeMethods.kuzu_value_get_int32 (&value, &v) |> ignore
            KInt64(int64 v)
        | KuzuDataTypeId.Int16 ->
            let mutable v = 0s
            NativeMethods.kuzu_value_get_int16 (&value, &v) |> ignore
            KInt64(int64 v)
        | KuzuDataTypeId.Int8 ->
            let mutable v = 0y
            NativeMethods.kuzu_value_get_int8 (&value, &v) |> ignore
            KInt64(int64 v)
        | KuzuDataTypeId.UInt32 ->
            let mutable v = 0u
            NativeMethods.kuzu_value_get_uint32 (&value, &v) |> ignore
            KInt64(int64 v)
        | KuzuDataTypeId.UInt16 ->
            let mutable v = 0us
            NativeMethods.kuzu_value_get_uint16 (&value, &v) |> ignore
            KInt64(int64 v)
        | KuzuDataTypeId.UInt8 ->
            let mutable v = 0uy
            NativeMethods.kuzu_value_get_uint8 (&value, &v) |> ignore
            KInt64(int64 v)
        | KuzuDataTypeId.Double ->
            let mutable v = 0.0
            NativeMethods.kuzu_value_get_double (&value, &v) |> ignore
            KDouble v
        | KuzuDataTypeId.Float ->
            let mutable v = 0.0f
            NativeMethods.kuzu_value_get_float (&value, &v) |> ignore
            KDouble(float v)
        | KuzuDataTypeId.String ->
            let mutable ptr = IntPtr.Zero
            NativeMethods.kuzu_value_get_string (&value, &ptr) |> ignore
            KString(readOwnedString ptr)
        | other -> KOther(kindOfTypeId other)

    /// Decodes a native value into a facade cell, capturing Kùzu's own string
    /// rendering alongside the typed payload.
    let decode (value: byref<KuzuValueStruct>) : KuzuValue =
        let rendered = readOwnedString (NativeMethods.kuzu_value_to_string &value)

        if NativeMethods.kuzu_value_is_null &value then
            KuzuValue(KNull, rendered)
        else
            let mutable logicalType = KuzuLogicalType()
            NativeMethods.kuzu_value_get_data_type (&value, &logicalType) |> ignore
            let id = NativeMethods.kuzu_data_type_get_id &logicalType
            NativeMethods.kuzu_data_type_destroy &logicalType
            KuzuValue(decodeTyped &value id, rendered)
