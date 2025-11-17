## Reflection on Baseline (3 points)

In the baseline I used a single `Model` record that mixed drawing data and history:

```fsharp
type Model = {
    finishedPolygons : PolyLine list
    currentPolygon   : PolyLine option
    mousePos         : Coord option
    past             : Model option
    future           : Model option
}
```

The code was still written in a functional style (immutable records, pattern matching in `updateModel`, using list operations like `coord :: poly`), but the functional ideas were used “inline” rather than as a separate abstraction. For example, `updateModel` handled `AddPoint` and `FinishPolygon` by returning a new record, and `addUndoRedo` wrapped this with extra logic to maintain `past` and `future`.

What I found hard in this version was the amount of bookkeeping I had to keep in my head whenever I touched the update logic. Every time I handled an editing message like `AddPoint` or `FinishPolygon` I also had to remember the undo rules: take a snapshot (`past = Some model`) and clear the redo chain (`future = None`). The compiler could not help me with forgetting one of these, because this discipline lived only in my head and in comments, not in the type structure.

This mental overhead showed up especially in undo/redo: the `Undo` and `Redo` cases in `addUndoRedo` had to pattern match on `model.past` / `model.future`, restore the previous `Model`, and keep the history linear. I ran into bugs where undo worked but redo didn’t, simply because I had swapped one assignment or forgotten to clear the opposite side once a new action happened after an undo. Debugging then meant stepping through several nested `Model` values in the debugger.

Tests helped, but they were also slightly awkward in the baseline. The helper

```fsharp
let ignorePast (m : Model) =
    { m with past = None; future = None; mousePos = None }
```

was a small code smell: to assert something about polygons I had to “sanitize” the history fields away first. That works, but it is a sign that the model mixes several concerns and that my tests are fighting the shape of the type.

Looking back, the baseline could be improved by extracting small helpers around the history protocol (e.g. a `pushPast : Model -> Model -> Model` to centralize “store previous and clear future”) and by separating the pure drawing logic into its own function that does not know about `past`/`future`. That would already make the file easier to read and would have given me a cleaner starting point for the later refactoring.

## Reflection on Purely Functional F# Variant (3 points)

In the refactored F# variant I kept everything immutable but changed the architecture: I introduced a `DrawingModel` without history and a generic `History<'a>` wrapper:

```fsharp
type DrawingModel = {
    finishedPolygons : PolyLine list
    currentPolygon   : PolyLine option
    mousePos         : Coord option
}

type History<'a> = {
    Past    : 'a list
    Present : 'a
    Future  : 'a list
}

type Model = History<DrawingModel>
```

Messages are still modeled as a discriminated union (`type Msg = | AddPoint of Coord | SetCursorPos of Coord option | FinishPolygon | Undo | Redo`), and `update` is one big pattern match, but now the editing logic lives in a pure `updateDrawing : Msg -> DrawingModel -> DrawingModel` function while undo/redo is handled generically in `History.apply`, `History.undo` and `History.redo`. The Elmish MVU loop (`init`, `update`, `render`) stays the same, but the responsibilities inside it are clearer.

The hard part here was not syntax but changing my mental model from “I update one big record that happens to contain history” to “I have a clean present state and a separate time-travel mechanism”. Designing `History.apply` so that it always pushes the old `Present` to `Past`, replaces `Present`, and clears `Future` forced me to think in invariants instead of individual assignments. Whenever I changed `DrawingModel` (for example adding fields used by the view), the compiler suddenly pointed me to all the places in tests and rendering code that had to be updated; that felt noisy at first, but in practice it made sure I did not forget any case.

This variant made the tests noticeably cleaner. For example, history-related tests can assert properties directly on `Past`, `Present` and `Future`, and application-level tests can ignore history by simply projecting `history.Present` instead of rebuilding a whole `Model` with `past = None; future = None`. Property-based tests like “undo after an action returns to the previous state” or “new action after undo clears `Future`” read almost like the specification now because the types match the concepts.

There is still room for improvement. The `update` function could be split further so that the undo/redo wiring and the drawing semantics live in separate modules, and `History<'a>` itself could be moved into a small, reusable library with its own generic test suite (for example, property tests that check `undo`/`redo` round-trips for any `'a`). Also, some of the names and grouping of tests could be tightened so that the correspondence between “Core Geometry”, “Preview Behavior” and “History” is obvious at a glance.

## Comparison of Both Approaches (4 points)

### Extensibility

The refactored variant is easier to extend with features like “delete polygon” or “change color”, because I would only touch the drawing-specific part (`DrawingModel` and `updateDrawing`) while the history logic in `History<'a>` stays unchanged, whereas in the baseline I would have to remember to duplicate the undo/redo rules in every new branch that mutates the model.

### Testability

The refactored variant is more testable because both drawing and history are expressed as pure functions over immutable values (`updateDrawing`, `History.undo`/`redo`), so unit and property tests can directly call them and compare states (often just `history.Present`), while the baseline needed helpers like `ignorePast` to work around the mixed model and made it easier to forget parts of the protocol.

### Maintainability

In terms of maintainability, the refactored code is stronger because the types (`DrawingModel`, `History<'a>`, `Msg`) clearly separate concerns and the compiler points out every place affected by a change, whereas the baseline hides this separation inside one big `Model` record and requires more discipline and reading effort to understand which fields belong to drawing and which belong to time travel.

### Complexity

Perceived complexity is lower in the baseline when you only look at the first few lines of code, but once undo/redo and multiple polygons are in play the refactored variant has the lower conceptual complexity, because each piece (drawing logic, history management, UI wiring) has a single responsibility instead of being entangled in one record and one update function.

