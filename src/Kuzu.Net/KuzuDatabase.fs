namespace Kuzu.Net

open System
open System.Runtime.InteropServices
open Kuzu.Net.Native

/// An embedded Kùzu database. Open one with <see cref="Open"/> or
/// <see cref="OpenInMemory"/>, then create connections with <see cref="Connect"/>.
///
/// Owns the native `kuzu_database` via a SafeHandle; dispose it (or use a `use`
/// / `using` binding) to release the database deterministically. Dispose any
/// connections before the database they belong to.
[<Sealed>]
type KuzuDatabase internal (handle: KuzuDatabaseHandle) =

    let mutable disposed = false

    let ensureOpen () =
        if disposed then
            raise (ObjectDisposedException "KuzuDatabase")

    /// Open (or create) an on-disk database at the given path.
    static member Open(path: string) : KuzuDatabase =
        if isNull path then
            nullArg "path"

        let mutable config = NativeMethods.kuzu_default_system_config ()
        let pathPtr = Marshal.StringToCoTaskMemUTF8 path

        try
            let mutable dbPtr = IntPtr.Zero
            let state = NativeMethods.kuzu_database_init (pathPtr, config, &dbPtr)

            if state <> KuzuState.Success then
                raise (KuzuException(sprintf "Failed to open Kùzu database at '%s'" path))

            let dbHandle = new KuzuDatabaseHandle()
            dbHandle.Initialize dbPtr
            new KuzuDatabase(dbHandle)
        finally
            Marshal.FreeCoTaskMem pathPtr

    /// Open an in-memory database (nothing is persisted to disk).
    static member OpenInMemory() : KuzuDatabase = KuzuDatabase.Open ":memory:"

    /// Open a new connection to this database.
    member _.Connect() : KuzuConnection =
        ensureOpen ()
        let mutable db = handle.DangerousGetHandle()
        let mutable connPtr = IntPtr.Zero
        let state = NativeMethods.kuzu_connection_init (&db, &connPtr)

        if state <> KuzuState.Success then
            raise (KuzuException "Failed to open Kùzu connection")

        let connHandle = new KuzuConnectionHandle()
        connHandle.Initialize connPtr
        new KuzuConnection(connHandle)

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                disposed <- true
                handle.Dispose()
