namespace Kuzu.Net.Native

open System
open System.Runtime.InteropServices

/// SafeHandle over a `kuzu_database`. The stored handle is the single inner
/// pointer of the `kuzu_database` struct; `ReleaseHandle` rebuilds the struct
/// (one pointer) on the stack and hands its address to `kuzu_database_destroy`.
[<Sealed>]
type KuzuDatabaseHandle() =
    inherit SafeHandle(IntPtr.Zero, true)

    override this.IsInvalid = this.DangerousGetHandle() = IntPtr.Zero

    override this.ReleaseHandle() =
        let mutable h = this.DangerousGetHandle()
        NativeMethods.kuzu_database_destroy &h
        true

    member internal this.Initialize(ptr: nativeint) = this.SetHandle ptr

/// SafeHandle over a `kuzu_connection`.
[<Sealed>]
type KuzuConnectionHandle() =
    inherit SafeHandle(IntPtr.Zero, true)

    override this.IsInvalid = this.DangerousGetHandle() = IntPtr.Zero

    override this.ReleaseHandle() =
        let mutable h = this.DangerousGetHandle()
        NativeMethods.kuzu_connection_destroy &h
        true

    member internal this.Initialize(ptr: nativeint) = this.SetHandle ptr

/// SafeHandle over a `kuzu_query_result`.
[<Sealed>]
type KuzuQueryResultHandle() =
    inherit SafeHandle(IntPtr.Zero, true)

    override this.IsInvalid = this.DangerousGetHandle() = IntPtr.Zero

    override this.ReleaseHandle() =
        let mutable h = this.DangerousGetHandle()
        NativeMethods.kuzu_query_result_destroy &h
        true

    member internal this.Initialize(ptr: nativeint) = this.SetHandle ptr
