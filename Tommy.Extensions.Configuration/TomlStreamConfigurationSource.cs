using Microsoft.Extensions.Configuration;

namespace Tommy.Extensions.Configuration
{
    /// <summary>
    /// Represents a TOML file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class TomlStreamConfigurationSource : StreamConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="TomlStreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="TomlStreamConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
            => new TomlStreamConfigurationProvider(this);
    }
}