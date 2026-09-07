using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using tarkin.doordash.Patches;

namespace tarkin.doordash
{
    [BepInPlugin("com.tarkin.doordash", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        internal static ConfigEntry<bool> Enabled;

        internal static ConfigEntry<bool> BreachLocked;

        internal static ConfigEntry<float> VelocityThresholdSqr;
        internal static ConfigEntry<float> RayDistance;

        internal static ConfigEntry<float> DislodgeChance;
        internal static ConfigEntry<float> DislodgeForce;

        internal static ConfigEntry<float> ArmDamageBase;
        internal static ConfigEntry<float> LockedBreachDamageMultiplier;
        internal static ConfigEntry<float> ContusionTime;
        internal static ConfigEntry<float> ContusionStrength;
        internal static ConfigEntry<float> RecoilHands;
        internal static ConfigEntry<float> RecoilCamera;
        internal static ConfigEntry<bool> BurnStamina;
        internal static ConfigEntry<EBodyPart> BodyPartToHurt;

        private PatchManager _patchManager;

        private RaycastBreacher _instance;

        private void Start()
        {
            Log = base.Logger;

            _patchManager = new PatchManager(this, autoPatch: true);
            _patchManager.EnablePatches();

            InitConfiguration();

            Patch_GameWorld_OnGameStarted.OnPostfix += AddRaycastBreacherToMainPlayer;
            if (Singleton<AbstractGame>.Instantiated)
            {
                AddRaycastBreacherToMainPlayer(Singleton<GameWorld>.Instance);

#if DEBUG
                Singleton<GameWorld>.Instance.MainPlayer.ActiveHealthController.ChangeHydration(100f);
                Singleton<GameWorld>.Instance.MainPlayer.ActiveHealthController.ChangeEnergy(100f);
#endif
            }
        }

        void AddRaycastBreacherToMainPlayer(GameWorld gameWorld)
        {
            if (gameWorld.MainPlayer != null)
            {
                _instance = gameWorld.MainPlayer.gameObject.GetOrAddComponent<RaycastBreacher>();
            }
        }

        void OnDestroy()
        {
            if (_instance != null)
                Destroy(_instance);

            Patch_GameWorld_OnGameStarted.OnPostfix -= AddRaycastBreacherToMainPlayer;

            _patchManager.DisablePatches();
            _patchManager = null;
        }

        private void InitConfiguration()
        {
            Enabled = Config.Bind("", "Enabled", true);

            BreachLocked = Config.Bind("Logic", "Breach Locked", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            VelocityThresholdSqr = Config.Bind("Sprint Ram", "Velocity Threshold", 20f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RayDistance = Config.Bind("Sprint Ram", "Ray Distance", 0.7f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            DislodgeChance = Config.Bind("Physical Door", "Chance To Dislodge On Breach", 0.01f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            DislodgeForce = Config.Bind("Physical Door", "Dislodge Force", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            ArmDamageBase = Config.Bind("Player Effect", "Health Damage", 10f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            LockedBreachDamageMultiplier = Config.Bind("Player Effect", "LockedBreachDamageMultiplier", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ContusionTime = Config.Bind("Player Effect", "Contusion Time", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ContusionStrength = Config.Bind("Player Effect", "Contusion Strength", 0.5f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RecoilHands = Config.Bind("Player Effect", "Recoil Hands", 2f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            RecoilCamera = Config.Bind("Player Effect", "Recoil Camera", 4f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            BurnStamina = Config.Bind("Player Effect", "Burn Stamina", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            BodyPartToHurt = Config.Bind("Player Effect", "BodyPartToHurt", EBodyPart.LeftArm, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
        }
    }
}