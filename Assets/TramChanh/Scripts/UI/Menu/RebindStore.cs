using System;
using UnityEngine.InputSystem;

namespace TramChanh.UI.Menu
{
    /// <summary>Persists an InputActionAsset's binding overrides through <see cref="RebindJsonStore"/>. Corrupt data falls back to the defaults.</summary>
    public sealed class RebindStore
    {
        private readonly RebindJsonStore _json;

        public RebindStore(IRebindTextStore store, string key = RebindJsonStore.DefaultKey)
        {
            _json = new RebindJsonStore(store, key);
        }

        /// <summary>Applies the saved overrides to <paramref name="asset"/>. Returns false (asset left on its defaults) when nothing valid was saved.</summary>
        public bool Load(InputActionAsset asset)
        {
            if (asset == null) { throw new ArgumentNullException(nameof(asset)); }
            asset.RemoveAllBindingOverrides();
            if (!_json.TryLoad(out string json)) { return false; }
            try
            {
                asset.LoadBindingOverridesFromJson(json, true);
                return true;
            }
            catch (Exception)
            {
                asset.RemoveAllBindingOverrides();
                _json.Clear();
                return false;
            }
        }

        public void Save(InputActionAsset asset)
        {
            if (asset == null) { throw new ArgumentNullException(nameof(asset)); }
            _json.Save(asset.SaveBindingOverridesAsJson());
        }

        /// <summary>Forget the saved overrides (the asset itself is not touched).</summary>
        public void Clear() => _json.Clear();
    }
}
