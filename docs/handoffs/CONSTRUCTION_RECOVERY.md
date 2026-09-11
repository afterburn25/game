# Construction queue and recovery

Construction orders retain the exact authorization quote paid at placement. The active order also stores that quote; legacy active orders restore with a zero quote so cancelling them cannot create credits.

`QueueProject` keeps at most eight unstarted orders, debits each quote once, and promotes the FIFO head only after it remains eligible. A blocked head stays visible in state for the presentation layer and can be cancelled for its full recorded quote. Active cancellation refunds the paid quote in proportion to unconsumed material work; consumed materials are never refunded.

AI automatic construction now goes through `StartProject`, selecting only affordable eligible catalog projects. It no longer assigns an active project directly.

The existing v12 envelope remains compatible because the new DTO fields are optional/defaulted. Loading validates finite values, known project IDs, duplicate/overlapping projects, queue bounds, and quoted catalog costs.
