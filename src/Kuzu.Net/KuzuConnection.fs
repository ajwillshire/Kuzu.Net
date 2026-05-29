namespace Kuzu.Net

open System
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Kuzu.Net.Native

/// A connection to a <see cref="KuzuDatabase"/>. Issues Cypher queries.
///
/// A connection is not safe for concurrent use; serialise queries on a single
/// connection, or open one connection per logical caller. Owns the native
/// `kuzu_connection` via a SafeHandle.
[<Sealed>]
type KuzuConnection internal (handle: KuzuConnectionHandle) =

    let mutable disposed = false

    let ensureOpen () =
        if disposed then
            raise (ObjectDisposedException "KuzuConnection")

    /// Execute a Cypher query and return its result. Throws
    /// <see cref="KuzuException"/> if the engine reports failure.
    member _.Query(cypher: string) : KuzuQueryResult =
        ensureOpen ()

        if isNull cypher then
            nullArg "cypher"

        let mutable conn = handle.DangerousGetHandle()
        let queryPtr = Marshal.StringToCoTaskMemUTF8 cypher

        try
            let mutable resultPtr = IntPtr.Zero
            let state = NativeMethods.kuzu_connection_query (&conn, queryPtr, &resultPtr)

            let resultHandle = new KuzuQueryResultHandle()
            resultHandle.Initialize resultPtr
            let result = new KuzuQueryResult(resultHandle)

            if state <> KuzuState.Success || not result.IsSuccess then
                let message = result.ErrorMessage
                (result :> IDisposable).Dispose()

                raise (
                    KuzuException(
                        if String.IsNullOrEmpty message then
                            "Kùzu query failed"
                        else
                            message
                    )
                )

            result
        finally
            Marshal.FreeCoTaskMem queryPtr

    /// Execute a Cypher query asynchronously. The synchronous native call is
    /// offloaded to the thread pool; cancellation prevents an unstarted call
    /// from running but does not interrupt a query already in flight.
    member this.QueryAsync(cypher: string, ?cancellationToken: CancellationToken) : Task<KuzuQueryResult> =
        let ct = defaultArg cancellationToken CancellationToken.None
        Task.Run((fun () -> this.Query cypher), ct)

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                handle.Dispose()
