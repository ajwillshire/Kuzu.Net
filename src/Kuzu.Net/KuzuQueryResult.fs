namespace Kuzu.Net

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open Kuzu.Net.Native

/// The result of a Cypher query. Iterable as a sequence of rows, where each row
/// is an `IReadOnlyList<KuzuValue>` aligned with <see cref="ColumnNames"/>.
///
/// Owns the native `kuzu_query_result` via a SafeHandle; dispose it (or use a
/// `use` / `using` binding) to release native memory deterministically.
[<Sealed>]
type KuzuQueryResult internal (handle: KuzuQueryResultHandle) =

    let mutable disposed = false

    let ensureOpen () =
        if disposed then
            raise (ObjectDisposedException "KuzuQueryResult")

    /// Whether the query executed successfully.
    member _.IsSuccess =
        ensureOpen ()
        let mutable h = handle.DangerousGetHandle()
        NativeMethods.kuzu_query_result_is_success &h

    /// The engine's error message when the query failed (empty when it succeeded).
    member _.ErrorMessage =
        ensureOpen ()
        let mutable h = handle.DangerousGetHandle()
        let ptr = NativeMethods.kuzu_query_result_get_error_message &h

        if ptr = IntPtr.Zero then
            ""
        else
            (Marshal.PtrToStringUTF8 ptr |> Option.ofObj |> Option.defaultValue "")

    /// The result column names, in column order.
    member _.ColumnNames: IReadOnlyList<string> =
        ensureOpen ()
        let mutable h = handle.DangerousGetHandle()
        let n = int (NativeMethods.kuzu_query_result_get_num_columns &h)
        let names = ResizeArray<string>(n)

        for i in 0 .. n - 1 do
            let mutable namePtr = IntPtr.Zero

            NativeMethods.kuzu_query_result_get_column_name (&h, uint64 i, &namePtr)
            |> ignore

            let name =
                if namePtr = IntPtr.Zero then
                    ""
                else
                    let s = Marshal.PtrToStringUTF8 namePtr
                    NativeMethods.kuzu_destroy_string namePtr
                    if isNull s then "" else s

            names.Add name

        names :> IReadOnlyList<string>

    /// Whether another row is available.
    member _.HasNext() =
        ensureOpen ()
        let mutable h = handle.DangerousGetHandle()
        NativeMethods.kuzu_query_result_has_next &h

    /// Advance to and return the next row. Call <see cref="HasNext"/> first.
    member _.GetNext() : IReadOnlyList<KuzuValue> =
        ensureOpen ()
        let mutable h = handle.DangerousGetHandle()
        let mutable tuple = IntPtr.Zero
        NativeMethods.kuzu_query_result_get_next (&h, &tuple) |> ignore

        let n = int (NativeMethods.kuzu_query_result_get_num_columns &h)
        let row = ResizeArray<KuzuValue>(n)

        for i in 0 .. n - 1 do
            let mutable value = KuzuValueStruct()
            NativeMethods.kuzu_flat_tuple_get_value (&tuple, uint64 i, &value) |> ignore

            let strPtr = NativeMethods.kuzu_value_to_string &value

            let rendered =
                if strPtr = IntPtr.Zero then
                    null
                else
                    let s = Marshal.PtrToStringUTF8 strPtr
                    NativeMethods.kuzu_destroy_string strPtr
                    s

            NativeMethods.kuzu_value_destroy &value
            row.Add(KuzuValue rendered)

        NativeMethods.kuzu_flat_tuple_destroy &tuple
        row :> IReadOnlyList<KuzuValue>

    interface IEnumerable<IReadOnlyList<KuzuValue>> with
        member this.GetEnumerator() =
            (seq {
                while this.HasNext() do
                    yield this.GetNext()
            })
                .GetEnumerator()

    interface System.Collections.IEnumerable with
        member this.GetEnumerator() =
            (this :> IEnumerable<IReadOnlyList<KuzuValue>>).GetEnumerator() :> System.Collections.IEnumerator

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                handle.Dispose()
