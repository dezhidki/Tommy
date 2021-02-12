# Tommy TOML style

This file outlines the code style that Tommy conforms to.
Whenever you generate a TOML file with Tommy, the file will be normalized to follow the style below.

The style normalization is done to keep the code simple and manageable.

### Comments

Only comments that are directly above the table or key-value pair are recognized.  
Comments are ignored inside arrays and inline tables.

### Dates

* The `T` separator is added to all local dates and date offsets
    * `1979-05-27 07:32:00Z` -> `1979-05-27T07:32:00Z`
* In date offsets, `Z` is used as "zero" offset
    * `1979-05-27 07:32:00+00:00` -> `1979-05-27T07:32:00Z`