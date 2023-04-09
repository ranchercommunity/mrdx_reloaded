using System;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.ExtractDataBin.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace MRDX.Base.ExtractDataBin
{
    /// <summary>
    ///     Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase, IExports // <= Do not Remove.
    {
        /// <summary>
        ///     Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        ///     Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        ///     The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        /// <summary>
        ///     Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        ///     Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _modConfig = context.ModConfig;

            _modLoader.AddOrReplaceController<IExtractDataBin>(_owner, new ExtractDataBin(_logger));
        }

        #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod()
        {
        }
#pragma warning restore CS8618

        #endregion

        public Type[] GetTypes()
        {
            return new[] { typeof(IExtractDataBin) };
        }
    }
}