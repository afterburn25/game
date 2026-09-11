# Colony surface shortage feedback

The colony surface now projects its status from `ColonySurfaceFeedbackReadModel`. It owns no
state and does not call a mutating simulation step.

- Sustenance capacity below population is shown as **buffered** while food or water reserves can
  cover the deficit. Actual decline is shown only after the relevant reserve is exhausted, or for
  an immediate housing deficit. Resource outposts show their support constraint without claiming
  ordinary-colony demographic decline.
- Each incomplete site shows its stored-material context, total shared site demand, its current
  proportional allocation, and a minimum build time. With no stored material it says that it is
  awaiting material availability; it does not promise a countdown or treat future production as a
  permanent deadlock.
- Build cards still authorize from credits alone. Their tooltips make clear that materials are
  spent gradually and shared with existing authorized sites.

The sidebar keeps recovery actions concise: controlled agriculture for food, water reclamation for
water, habitat complexes for housing, and material production or fewer competing sites for
construction. This is presentation-only feedback; construction authorization, allocation,
population, save data, and balance remain authoritative elsewhere.
