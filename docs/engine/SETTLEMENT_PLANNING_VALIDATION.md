# Settlement planning validation

The retained actual C# oracle contains 88 cases covering colony and staffed
resource-outpost plans, explicit order assessments, operational reach, and outpost
fleet classification. The matrix covers private, partial, and complete surveys;
fleet and passenger validation; duplicate system and body dictionaries; missing
economies; exact affordability boundaries and currency messages; occupied and
reserved systems; foreign mission noninterference; candidate caps; explicit colony
targets beyond the display cap; outpost targets beyond its hard assessment cap;
stable ties; NaN ordering; flat float-distance ties; large-coordinate depth distance;
and both injected and authoritative lane reach.

Every candidate and plan field is compared, including ordered output, biology,
deposit metadata, physical distance, reach payloads, reservation metadata, status,
and exact approval or rejection text. The oracle records 78 reach callback calls,
including callbacks that throw before a result exists. The native consumer decodes
and validates fixture arguments before its operation catch, invokes only the typed
production method inside that catch, and encodes and checks typed results afterward.
It also verifies read-only world preservation. Six null-reference observations that
the typed native API cannot represent remain separately reported.

Source comparison exposed three production differences that were repaired: colony
approval text now uses the source's one-decimal population format, ascending distance
ordering follows .NET's NaN ordering, and physical distance now reuses the accepted
`interstellar_distance_from_fleet` helper. The latter preserves C# `Vector2.Distance`
float behavior for legacy flat geometry and double subtraction for depth-aware
geometry.

The promoted fixture SHA-256 is
`03BD7250358D19AC9D107A1C5D2119D0186E50623992DB0865735F914F092BB0`.
Standalone Release and Debug builds compiled as C++23 with `/W4 /WX` and passed all
88 actual C# cases plus the six separately reported null-reference observations.

The maintained `windows-testing` build passed 39 of 39 CTest targets and 19 of 19
Python recovery/package-integrity tests. Its complete log is
`work/native-029-settlement-planning-testing.log`.
