using BepInEx;
using IL.RoR2.Orbs;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace CursedInventoryPlugin
{
    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]

    // This one is because we use a .language file for language tokens
    // More info in https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class CursedInventoryPlugin : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Tarook";
        public const string PluginName = "CursedInventory";
        public const string PluginVersion = "0.0.1";

        private static ItemDef curseItemDef;


        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            curseItemDef = ScriptableObject.CreateInstance<ItemDef>();

            curseItemDef.name = "CURSEITEM_NAME";
            curseItemDef.nameToken = "CURSEITEM_NAME";
            curseItemDef.pickupToken = "CURSEITEM_PICKUP";
            curseItemDef.descriptionToken = "CURSEITEM_DESC";
            curseItemDef.loreToken = "CURSEITEM_LORE";

#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            curseItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarDef.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            curseItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            curseItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            curseItemDef.canRemove = true;
            curseItemDef.hidden = false;

            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);

            ItemAPI.Add(new CustomItem(curseItemDef, displayRules));

            Run.onRunStartGlobal += Run_onRunStartGlobal;
        }

        private void Run_onRunStartGlobal(Run run)
        {
            // maybe add a check here to see if we are the host?
            if(NetworkServer.active && !TryGetComponent(out InventorySwapper _))
                PlayerCharacterMasterController.instances[0].gameObject.AddComponent<InventorySwapper>();
        }
        public static IEnumerable<CharacterMaster> GetAllPlayerMasters(bool requireAlive)
        {
            return from playerMasterController in PlayerCharacterMasterController.instances
                   where playerMasterController
                   let playerMaster = playerMasterController.master
                   where playerMaster && (!requireAlive || !playerMaster.IsDeadAndOutOfLivesServer())
                   select playerMaster;
        }
    }

}