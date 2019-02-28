<p align="center">
    <img src="logos/tommy_logo.png" height="200" />
</p>

# Tommy

Tommy is a single-file TOML reader and writer for C#.  
This library is meant for small, cross-platform projects that want support the most .NET versions possible.

To use it, simply include [Tommy.cs](Tommy/Tommy.cs) into your project and you're done!

## Features

* Full implementation of TOML 0.5.0 spec.
* Parser implemented with `TextReader` to ensure speed and simplicity.
* Parses TOML into a node-based structure that is similar to [SimpleJSON](https://github.com/Bunny83/SimpleJSON).
* Basic support for parsing and saving comments.
* Supports .NET 3.5+, Mono, .NET Core!
* Uses C# 7.2 syntax for smaller file size
* Small footprint (~32 KB compiled) compared to other similar C# libraries

## How to use

### Parsing TOML file

The TOML file:

```toml
title = "TOML Example"

[owner]
name = "Tom Preston-Werner"
dob = 1979-05-27T07:32:00-08:00

[database]
server = "192.168.1.1"
ports = [ 8001, 8001, 8002 ]
connection_max = 5000
enabled = true
```

```csharp
// Reference the Tommy namespace at the start of the file
using Tommy;


// Parse into a node
using(StreamReader reader = new StreamReader(File.OpenRead("configuration.toml")))
{
    // Parse the table
    TomlTable table = TOML.Parse(reader);

    Console.WriteLine(table["title"]);  // Prints "TOML Example"

    // You can check the type of the node via a property and access the exact type via As*-property
    Console.WriteLine(table["owner"]["dob"].IsDateTime)  // Prints "True"

    // You can also do both with C# 7 syntax
    if(table["owner"]["dob"] is TomlDate date)
        Console.WriteLine(date.OnlyDate); // Some types contain additional properties related to formatting

    // You can also iterate through all nodes inside an array or a table
    foreach(TomlNode node in table["database"]["ports"])
        Console.WriteLine(node);
}
```

## Generating or editing a TOML file

Tommy supports implicit casting from most built-in types to make file generation easy.

```csharp
// Reference the Tommy namespace at the start of the file
using Tommy;


// Generate a TOML file programmatically
TomlTable toml = new TomlTable 
{
    ["title"] = "TOML Example",
    // You can also insert comments before a node with a special property
    ["value-with-comment"] = new TomlString
    {
        Value = "Some value",
        Comment = "This is just some value with a comment"
    },
    // You don't need to specify a type for tables or arrays -- Tommy will figure that out for you
    ["owner"] = 
    {
        ["name"] = "Tom Preston-Werner",
        ["dob"] = DateTime.Now
    },
    ["array-table"] = new TomlArray 
    {
        // This is marks the array as a TOML array table
        IsTableArray = true,
        [0] = 
        {
            ["value"] = 10
        },
        [1] = 
        {
            ["value"] = 20
        }
    },
    ["inline-table"] = new TomlTable
    {
        IsInline = true,
        ["foo"] = "bar",
        ["bar"] = "baz",
        // Implicit cast from TomlNode[] to TomlArray
        ["array"] = new TomlNode[] { 1, 2, 3 }
    }
};


// You can also define the toml file (or edit the loaded file directly):
toml["other-value"] = 10;
toml["value with spaces"] = new TomlString 
{
    IsMultiline = true,
    Value = "This is a\nmultiline string"
};

// Write to a file (or any TextWriter)
// You can forcefully escape ALL Unicode characters by uncommenting the following line:
// TOML.ForceASCII = true;
using(StreamWriter writer = new StreamWriter(File.OpenWrite("out.toml")))
    toml.ToTomlString(writer);
```

The above code outputs the following TOML file:

```toml
title = "TOML Example"
# This is just some value with a comment
value-with-comment = "Some value"
inline-table = { foo = bar, bar = baz, array = [ 1, 2, 3, ], }
other-value = 10
"value with spaces" = """This is a
multiline string"""

[owner]
name = "Tom Preston-Werner"
dob = 2019-02-28 22:08:56

[[array-table]]
value = 10

[[array-table]]
value = 20
```

Some notes about the writer:

* Currently the writer doesn't use subkeys for values and instead writes out subtables. Thus instead of writing `foo.bar = "foo"` it will write a table `[foo]` with key `bar`.
* The writer only uses basic strings for complex keys (i.e. no literal strings).

## Tests

Tommy's parser passes all syntax tests in the [toml-tests](https://github.com/BurntSushi/toml-test) test suite (with additional 0.5.0-specific tests from [toml-test#51](https://github.com/BurntSushi/toml-test/pull/51)).

The parser passes some additional basic unit tests.