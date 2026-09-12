# Nearby star catalog

`hyg-nearby-500-v1.json` is a reproducible selection of the 500 nearest valid HYG systems, grouped by HYG `comp_primary`. Multiple components of a system are visited once; widely separated catalog systems such as Proxima Centauri remain separate where HYG assigns a separate primary.

The source is HYG Database v4.1 at the pinned commit [c7f7f883fe678cc7680169a50ccd7dcc49b060ce](https://github.com/astronexus/HYG-Database/tree/c7f7f883fe678cc7680169a50ccd7dcc49b060ce), downloaded from the URL recorded in the JSON. HYG is licensed CC BY-SA 4.0 ([license](https://creativecommons.org/licenses/by-sa/4.0/)); this derived JSON is provided under the same license with attribution. The source SHA-256 is recorded in the JSON and can be reproduced with `scripts/build_nearby_star_catalog.py --source <hygdata_v41.csv> --verify`.

This is not a complete census and is not a current Gaia release. Distances are the catalog's approximate parallax-derived values; no estimates are substituted for missing values. Coordinates are HYG equatorial J2000 XYZ converted from parsecs to light-years (3.26156). The Sun is exactly at (0, 0, 0). Planet content elsewhere in the game is fictional.

Reference distances in the selected sample include Barnard's Star (~5.95 ly), Sirius (~8.60 ly), Alpha Centauri (~4.37 ly), and Proxima Centauri (~4.23 ly). The sample's maximum distance is reported by the build/validation notes and is approximately 50 light-years.
