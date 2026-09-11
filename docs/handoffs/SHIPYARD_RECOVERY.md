# Shipyard order recovery

New shipyard orders persist a stable order ID, paid authorization quote, and exact population-source colony. Cancellation uses that identity so a stale control cannot cancel a promoted duplicate design. Queued orders refund their recorded quote; active orders refund only the unbuilt material fraction. Reserved colonists return only to the same owned colony with the same species. If that source cannot be proven, cancellation is rejected without changing credits, population, or the order.

Older saves default quote and source fields to zero/null. They remain playable and can complete, but a legacy population reservation without a provable source cannot be cancelled.
