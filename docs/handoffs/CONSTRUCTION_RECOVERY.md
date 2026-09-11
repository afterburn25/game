# Construction queue and recovery

Construction orders retain the exact authorization quote paid at placement. The active order also stores that quote; legacy active orders restore with a zero quote so cancelling them cannot create credits.

`QueueProject` keeps at most eight unstarted orders, debits each quote once, and promotes the FIFO head only after it remains eligible. A blocked head stays visible in state for the presentation layer and can be cancelled for its full recorded quote. Active cancellation refunds the paid quote in proportion to unconsumed material work; consumed materials are never refunded.

AI automatic construction now goes through `StartProject`, selecting only affordable eligible catalog projects. It no longer assigns an active project directly.

The existing v12 envelope remains compatible because the new DTO fields are optional/defaulted. This is an additive field change inside an already-supported envelope, so it does not change the format version. Loading validates finite values, known project IDs, duplicate/overlapping projects, and queue bounds. Stored paid quotes are deliberately not compared to current catalog prices: a later balance revision must not alter a saved authorization or make an older campaign unloadable.
