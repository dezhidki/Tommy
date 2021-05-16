# Tommy TOML style

This file outlines the code style that Tommy conforms to.
Whenever you generate a TOML file with Tommy, the file will be normalized to follow the style below.

The style normalization is done to keep the code simple and manageable.

### Key-Value pairs

* If a key includes quotable characters, it is put into normal quotes instead of literal ones.

### Comments

* Only comments that are directly above the table or key-value pair are recognized.  
* Comments are ignored inside arrays and inline tables.
* If there is a comment for a key-value pair, a newline is added before the comment to increase readability.
* If a comment is added to root TomlTable, it will be put at the top of the file. 

### Strings

* Newline marker `/` is not supported in multiline strings and is escaped.

### Arrays

* Trailing commas are removed
* There is always starting/ending whitespace between array markers. That is, `[ 1, 2 ]` is output instead of `[1, 2]`.

### Floats and integers

* Exponent marker is always output as lowercase `e`.
* Exponent marker is output according to .NET general formatting rules.
* Separator marker `_` is not preserved.
* `+inf`, `+nan` and `-nan` are not supported and will be normalized into `inf` and `nan`

### Dates

* The `T` separator is added to all local dates and date offsets
    * `1979-05-27 07:32:00Z` -> `1979-05-27T07:32:00Z`
* In date offsets, `Z` is used as "zero" offset
    * `1979-05-27 07:32:00+00:00` -> `1979-05-27T07:32:00Z`