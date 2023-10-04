using BepInEx;
using CursedInventoryPlugin;
using RoR2.Orbs;
using R2API;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RoR2.Chat;

public class InventorySwapper : MonoBehaviour
{
	public CharacterMaster currentCursedMaster;
    private List<ItemTransferOrb> inFlightOrbs = new List<ItemTransferOrb>();

	private void Start()
	{
		// pick a random player to curse
		CurseSurvivor(PlayerCharacterMasterController.instances[Random.Range(0, PlayerCharacterMasterController.instances.Count)].master, true);
    }

    private void CurseSurvivor(CharacterMaster newCursedMaster, bool addItem)
    {
        currentCursedMaster = newCursedMaster;
        if(addItem)
            currentCursedMaster.inventory.GiveItem(DLC1Content.Items.LunarSun, 1);

        Chat.SendBroadcastChat(new SimpleChatMessage
        {
            baseToken = "<color=#e5eefc>{0}</color>",
            paramTokens = new[] { "The curse has moved to a new survivor." }
        });
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F2))
		{
            var instances = PlayerCharacterMasterController.instances;
            // get a random player with an inventory that isn't the current one and swap with it
            List<PlayerCharacterMasterController> validPlayers = PlayerCharacterMasterController.instances.Where(instance => instance.master != currentCursedMaster).ToList();
            CharacterMaster targetMaster;
            if (validPlayers.Count == 0)
			{
                Log.Message("No valid players found");
                return;
            }
			else if (validPlayers.Count == 1)
			{
                Log.Message("Only one valid player found");
				targetMaster = validPlayers[0].master;
            }
			else
			{
				Log.Message("Multiple valid players found");
				targetMaster = validPlayers[Random.Range(0, validPlayers.Count)].master;
            }

			if (targetMaster != currentCursedMaster)
			{
                SwapInventory(currentCursedMaster);
            }
			else
			{
				Log.Message("Target inventory is the same as the current cursed inventory");
			}
		}
	}

	/// <summary>
	/// Swaps the cursed inventory with the targetMaster
	/// </summary>
	/// <param name="targetInventory"></param>
	private void SwapInventory(CharacterMaster targetMaster)
	{
        Log.Message($"Swapping cursed inventory : {currentCursedMaster.inventory.name} with inventory : {targetMaster.inventory.name}");
        
        GiveItems(currentCursedMaster, targetMaster);
        GiveItems(targetMaster, currentCursedMaster);

        CurseSurvivor(targetMaster, false);
    }

    void GiveItems(CharacterMaster giver, CharacterMaster receiver)
    {
        foreach (ItemIndex currentItemToGive in GetItemsToTransfer(giver.inventory))
        {
            giver.inventory.RemoveItem(currentItemToGive, giver.inventory.GetItemCount(currentItemToGive));
            ItemTransferOrb orb = ItemTransferOrb.DispatchItemTransferOrb(giver.GetBody().GetComponent<CharacterBody>().corePosition, receiver.inventory, currentItemToGive, giver.inventory.GetItemCount(currentItemToGive), orb =>
            {
                ItemTransferOrb.DefaultOnArrivalBehavior(orb);
                inFlightOrbs.Remove(orb);
            });

            inFlightOrbs.Add(orb);
        }
    }

    ItemIndex[] GetItemsToTransfer(Inventory inventory)
    {
        List<ItemIndex> itemsToTransfer = new List<ItemIndex>();
        foreach (ItemIndex item in inventory.itemAcquisitionOrder)
        {
            if(IsItemValid(item))
            {
                Log.Message($"Added item : {item} to transfer list");
                itemsToTransfer.Add(item);
            }
            else
            {
                Log.Message($"Item : {item} is not valid and was not added");
            }
        }
        return itemsToTransfer.ToArray();
    }

    public static bool IsItemValid(ItemIndex item) // I found this method in risk of chaos repo, I'm guessing it checks if an item exists and is not hidden
    {
        ItemDef itemDef = ItemCatalog.GetItemDef(item);
        return itemDef && !itemDef.hidden && itemDef.canRemove;
    }
}