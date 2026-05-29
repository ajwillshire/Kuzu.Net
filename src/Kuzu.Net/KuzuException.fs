namespace Kuzu.Net

open System

/// Raised when a Kùzu operation fails (database open, connection, or a query
/// that the engine reports as unsuccessful).
[<Sealed>]
type KuzuException(message: string) =
    inherit Exception(message)
