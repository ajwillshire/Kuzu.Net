namespace Kuzu.Net

open System

/// The kind of a result cell, surfaced as a plain enum at the public boundary
/// instead of an F# discriminated union so C# consumers get idiomatic usage.
///
/// The skeleton currently distinguishes only Null vs. String (via Kùzu's
/// universal value-to-string representation). Typed kinds (Int64, Double, Bool,
/// node/rel/list/struct) light up when the typed value accessors are wired —
/// see README "Roadmap".
type KuzuValueKind =
    | Null = 0
    | String = 1

/// A single cell of a query result row.
///
/// Boundary type by design: it exposes plain accessors rather than an F#
/// discriminated union, so a C# caller writes `row[0].AsString()` rather than
/// pattern-matching an F# union. Values are currently surfaced via Kùzu's own
/// string representation of the cell; typed accessors are a roadmap item.
[<Sealed>]
type KuzuValue internal (raw: string) =

    /// The kind of this value.
    member _.Kind =
        if isNull raw then
            KuzuValueKind.Null
        else
            KuzuValueKind.String

    /// True when the underlying Kùzu value is NULL.
    member _.IsNull = isNull raw

    /// The value rendered as a string (Kùzu's `kuzu_value_to_string`), or null
    /// when the cell is NULL.
    member _.AsString() = raw

    override _.ToString() = if isNull raw then "null" else raw
