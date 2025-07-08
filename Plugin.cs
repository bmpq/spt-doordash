using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using tarkin.doordash.Patches;

namespace tarkin.doordash
{
    [BepInPlugin("com.tarkin.doordash", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> VelocityThresholdSqr;
        internal static ConfigEntry<float> RayDistance;

        internal static ConfigEntry<float> DislodgeChance;
        internal static ConfigEntry<float> DislodgeForce;
        internal static ConfigEntry<float> HitDamage;
        
        private void Awake()
        {
            Log = base.Logger;

            InitConfiguration();

            new Patch_Door_KickOpen().Enable();

            new Patch_GameWorld_OnGameStarted().Enable();
            Patch_GameWorld_OnGameStarted.OnPostfix += (gameWorld) =>
            {
                gameWorld.MainPlayer.gameObject.GetOrAddComponent<RaycastBreacher>();
            };
        }

        private void InitConfiguration()
        {
            Enabled = Config.Bind("", "Enabled", true);

            VelocityThresholdSqr = Config.Bind("Sprint Ram", "Velocity Threshold", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RayDistance = Config.Bind("Sprint Ram", "Ray Distance", 0.7f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            DislodgeChance = Config.Bind("Physical Door", "Chance To Dislodge On Breach", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            DislodgeForce = Config.Bind("Physical Door", "Dislodge Force", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            HitDamage = Config.Bind("Physical Door", "Player Collision Damage", 100f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
        }
    }
}