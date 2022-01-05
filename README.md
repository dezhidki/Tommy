<p align="center">
    <img src="logos/tommy_logo.png" height="200" />
</p>

![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/dezhidki/Tommy?style=flat-square)
[![Nuget](https://img.shields.io/nuget/dt/Tommy?label=NuGet&style=flat-square)](https://www.nuget.org/packages/Tommy)

# Tommy

Tommy is a single-file TOML reader and writer for C#.  
This library is meant for small, cross-platform projects that want support the most .NET versions possible.

To use it, simply include [Tommy.cs](Tommy/Tommy.cs) into your project and you're done!

Alternatively, you can obtain the prebuilt package from [NuGet](https://www.nuget.org/packages/Tommy)!

## Features

* Full implementation of TOML 1.0.0 spec.
* Parser implemented with `TextReader` for simplicity and vast input support (i.e. string inputs with `StringReader`, streams via `StreamReader`, etc).
* Parses TOML into a node-based structure that is similar to [SimpleJSON](https://github.com/Bunny83/SimpleJSON).
* Basic support for parsing and saving comments.
* Supports .NET 3.5+, Mono, .NET Core!
* Uses C# 9 syntax for smaller file size.
* Small footprint (~41 KB compiled) compared to other similar C# libraries.
* Performs well compared to other similar C# libraries ([view benchmarks](https://github.com/bugproof/TomlLibrariesBenchmark))

### Extensions

Tommy includes only a reader and a writer. There exist a few additional extensions that you can use

Officially maintained
* [Tommy.Extensions](https://www.nuget.org/packages/Tommy.Extensions) -- General helper extensions for Tommy
* [Tommy.Extensions.Configuration](https://www.nuget.org/packages/Tommy.Extensions.Configuration) -- `Microsoft.Extensions.Configuration` integration for Tommy

3rd party
* [Tommy.Serializer](https://github.com/instance-id/Tommy.Serializer) -- (De)serialization of objects for Tommy

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
using(StreamReader reader = File.OpenText("configuration.toml"))
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

Note that `TOML.Parse` is just a shorthand for creating a `TOMLParser` object and parsing it. In essence, `TOML.Parse` is just simply a wrapper for the following code block:

```csharp
TomlTable table;
using(TOMLParser parser = new TOMLParser(reader))
    table = parser.Parse();
```

In some cases, you might want to write the snippet manually, since the TOML parser can contain some additional parsing options.

### Catching parse errors

Tommy is an optimistic parser: when it encounters a parsing error, it does not stop the parsing process right away. 
Instead, Tommy logs all parsing errors and throws them as a single `TomlParseException`. In addition to parsing errors, 
the exception object also contains the *partially parsed* TOML file that you can still attempt to use at your own risk.

Here's an example of handling parsing errors:

```csharp
TomlTable table;

try
{
    // Read the TOML file normally.
    table = TOML.Parse(reader);
} catch(TomlParseException ex) 
{
    // Get access to the table that was parsed with best-effort.
    table = ex.ParsedTable;

    // Handle syntax error in whatever fashion you prefer
    foreach(TomlSyntaxException syntaxEx in ex.SyntaxErrors)
        Console.WriteLine($"Error on {syntaxEx.Column}:{syntaxEx.Line}: {syntaxEx.Message}");
}
```

If you do not wish to handle exceptions, you can instead use [`TommyExtensions.TryParse()`](Tommy/TommyExtensions.cs#L21).

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
using(StreamWriter writer = File.CreateText("out.toml"))
{
    toml.WriteTo(writer);
    // Remember to flush the data if needed!
    writer.Flush();
}
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

### Collapsed values

Tommy supports collapsed values (i.e. values with keys of the form `foo.bar`). For that, simply set the `CollapseLevel` property of a value node.  
By default, the collapse level for each TOML node is `0`, which means that the node will appear under the table you define it in. 
Setting collapse level one value higher will move the value one table higher in the hierarchy.

In other words, if you define the following table:

```csharp
TomlTable table = new TomlTable {
    ["foo"] = new TomlTable {
        ["bar"] = new TomlTable {
            ["baz"] = new TomlString {
                Value = "Hello, world!"
            }
        }
    }
};
```

Will output the TOML file:

```toml
[foo.bar]
baz = "Hello, world!"
```

Adding `CollapseLevel = 1` to `foo.bar.baz` will "collapse" the key by one level:

```csharp
TomlTable table = new TomlTable {
    ["foo"] = new TomlTable {
        ["bar"] = new TomlTable {
            ["baz"] = new TomlString {
                CollapseLevel = 1, // Here we collapse the foo.bar.baz by one level
                Value = "Hello, world!"
            }
        }
    }
};
```

```toml
[foo]
bar.baz = "Hello, world!"
```

### Some notes about the writer

* **The writer does not currently preserve the layout of the original document!** This is to save size and keep things simple for now.
* Check out [Style info](./style.md) for information on what style Tommy uses to output TOML
* The writer only uses basic strings for complex keys (i.e. no literal strings).

## Optional extensions

In addition to main functionality, Tommy includes *optional* extensions located in [TommyExtensions.cs](Tommy/TommyExtensions.cs). 
The file is a collection of various functions that you might find handy, like `TOMLParser.TryParse`.

To use the extensions, simply include the file in your project. The extension methods will appear in types they are defined for.

## Tests

Tommy's parser is tested against [toml-lang/compliance](https://github.com/toml-lang/compliance) test suite with additions from [pyrmont/toml-specs](https://github.com/pyrmont/toml-specs).

## What's with the name?

[Because TOML sounded like Tommy, hahaha](https://i.ytimg.com/vi/y9N1GV88T7g/maxresdefault.jpg)
