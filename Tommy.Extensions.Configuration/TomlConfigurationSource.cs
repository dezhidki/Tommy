using Microsoft.Extensions.Configuration;

namespace Tommy.Extensions.Configuration
{
    /// <summary>
    ///     Represents a TOML file as an <see cref="IConfigurationSource" />.
    /// </summary>
    public class TomlConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        ///     Builds the <see cref="TomlConfigurationProvider" /> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder" />.</param>
        /// <returns>A <see cref="TomlConfigurationProvider" /></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new TomlConfigurationProvider(this);
        }
    }
}