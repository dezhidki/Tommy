using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Tommy.Extensions.Configuration
{
    /// <summary>
    ///     A TOML file based <see cref="FileConfigurationProvider" />.
    /// </summary>
    public class TomlConfigurationProvider : FileConfigurationProvider
    {
        /// <summary>
        ///     Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public TomlConfigurationProvider(TomlConfigurationSource source) : base(source) { }

        /// <summary>
        ///     Loads the TOML data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            try
            {
                Data = TomlConfigurationFileParser.Parse(stream);
            }
            catch (TomlParseException e)
            {
                throw new FormatException("Could not parse the TOML file.", e);
            }
        }
    }
}