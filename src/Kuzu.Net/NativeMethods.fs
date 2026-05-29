namespace Kuzu.Net.Native

open System
open System.Runtime.InteropServices

/// Return code from Kùzu C API calls (`kuzu_state`). Success = 0, Error = 1.
type KuzuState =
    | Success = 0
    | Error = 1

/// Mirrors `kuzu_system_config` from `kuzu.h`.
///
/// IMPORTANT: this layout MUST match the targeted Kùzu C API version field for
/// field. It is declared here against a recent Kùzu release and still needs
/// verifying against a pinned version before the binding is run — a mismatched
/// struct layout is a silent memory-corruption hazard. See README "Roadmap".
[<StructLayout(LayoutKind.Sequential)>]
type KuzuSystemConfig =
    struct
        val mutable BufferPoolSize: uint64
        val mutable MaxNumThreads: uint64

        [<MarshalAs(UnmanagedType.U1)>]
        val mutable EnableCompression: bool

        [<MarshalAs(UnmanagedType.U1)>]
        val mutable ReadOnly: bool

        val mutable MaxDbSize: uint64

        [<MarshalAs(UnmanagedType.U1)>]
        val mutable AutoCheckpoint: bool

        val mutable CheckpointThreshold: uint64
    end

/// Mirrors `kuzu_value` from `kuzu.h`: an owned pointer plus a flag indicating
/// whether the underlying C++ object owns the memory. Passed by reference to
/// the value-accessor and value-destroy entry points.
[<StructLayout(LayoutKind.Sequential)>]
type KuzuValueStruct =
    struct
        val mutable Value: nativeint

        [<MarshalAs(UnmanagedType.U1)>]
        val mutable IsOwnedByCpp: bool
    end

/// Mirrors `kuzu_logical_type` from `kuzu.h`: a single owned pointer to the
/// underlying logical-type object. Obtained via `kuzu_value_get_data_type` and
/// released with `kuzu_data_type_destroy`.
[<StructLayout(LayoutKind.Sequential)>]
type KuzuLogicalType =
    struct
        val mutable DataType: nativeint
    end

/// Mirrors `kuzu_data_type_id` from `kuzu.h` — the logical type of a value.
///
/// IMPORTANT: these numeric values are version-specific and MUST be verified
/// against the pinned Kùzu C API version before the binding is run (same caveat
/// as the struct layouts above — a drifted id mapping silently mis-decodes
/// result cells). Declared here against a recent Kùzu release. See README
/// "Roadmap".
type KuzuDataTypeId =
    | Any = 0
    | Node = 10
    | Rel = 11
    | RecursiveRel = 12
    | Serial = 13
    | Bool = 22
    | Int64 = 23
    | Int32 = 24
    | Int16 = 25
    | Int8 = 26
    | UInt64 = 27
    | UInt32 = 28
    | UInt16 = 29
    | UInt8 = 30
    | Int128 = 31
    | Double = 32
    | Float = 33
    | Date = 34
    | Timestamp = 35
    | TimestampSec = 36
    | TimestampMs = 37
    | TimestampNs = 38
    | TimestampTz = 39
    | Interval = 40
    | Decimal = 41
    | InternalId = 42
    | String = 50
    | Blob = 51
    | List = 52
    | Array = 53
    | Struct = 54
    | Map = 55
    | Union = 56
    | Pointer = 58
    | Uuid = 59

/// P/Invoke declarations over the Kùzu C API.
///
/// Kùzu's opaque handle types (`kuzu_database`, `kuzu_connection`,
/// `kuzu_query_result`, `kuzu_flat_tuple`) are single-pointer structs, which are
/// ABI-equivalent to a bare pointer when passed by value. We therefore model a
/// handle as a `nativeint` and pass its address (`nativeint&`) wherever the C
/// API wants `kuzu_<type>*`. This must be re-checked if a future Kùzu release
/// changes those structs to carry more than one field.
[<RequireQualifiedAccess>]
module internal NativeMethods =

    [<Literal>]
    let private Lib = "kuzu"

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuSystemConfig kuzu_default_system_config()

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_database_init(nativeint database_path, KuzuSystemConfig system_config, nativeint& out_database)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_database_destroy(nativeint& database)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_connection_init(nativeint& database, nativeint& out_connection)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_connection_destroy(nativeint& connection)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_connection_query(nativeint& connection, nativeint query, nativeint& out_query_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_query_result_destroy(nativeint& query_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    [<return: MarshalAs(UnmanagedType.U1)>]
    extern bool kuzu_query_result_is_success(nativeint& query_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern nativeint kuzu_query_result_get_error_message(nativeint& query_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern uint64 kuzu_query_result_get_num_columns(nativeint& query_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_query_result_get_column_name(nativeint& query_result, uint64 index, nativeint& out_column_name)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    [<return: MarshalAs(UnmanagedType.U1)>]
    extern bool kuzu_query_result_has_next(nativeint& query_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_query_result_get_next(nativeint& query_result, nativeint& out_flat_tuple)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_flat_tuple_destroy(nativeint& flat_tuple)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_flat_tuple_get_value(nativeint& flat_tuple, uint64 index, KuzuValueStruct& out_value)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_value_destroy(KuzuValueStruct& value)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern nativeint kuzu_value_to_string(KuzuValueStruct& value)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_destroy_string(nativeint str)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    [<return: MarshalAs(UnmanagedType.U1)>]
    extern bool kuzu_value_is_null(KuzuValueStruct& value)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_data_type(KuzuValueStruct& value, KuzuLogicalType& out_type)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuDataTypeId kuzu_data_type_get_id(KuzuLogicalType& data_type)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern void kuzu_data_type_destroy(KuzuLogicalType& data_type)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_bool(KuzuValueStruct& value, [<MarshalAs(UnmanagedType.U1)>] bool& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_int8(KuzuValueStruct& value, sbyte& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_int16(KuzuValueStruct& value, int16& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_int32(KuzuValueStruct& value, int32& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_int64(KuzuValueStruct& value, int64& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_uint8(KuzuValueStruct& value, byte& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_uint16(KuzuValueStruct& value, uint16& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_uint32(KuzuValueStruct& value, uint32& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_double(KuzuValueStruct& value, double& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_float(KuzuValueStruct& value, single& out_result)

    [<DllImport(Lib, CallingConvention = CallingConvention.Cdecl)>]
    extern KuzuState kuzu_value_get_string(KuzuValueStruct& value, nativeint& out_result)
