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

public class InventorySwapper : MonoBehaviour
{
	public CharacterMaster currentCursedMaster;
    private List<ItemTransferOrb> inFlightOrbs = new List<ItemTransferOrb>();

	private void Start()
	{
		// pick a random player to curse
		currentCursedMaster = PlayerCharacterMasterController.instances[Random.Range(0, PlayerCharacterMasterController.instances.Count)].master;
		CurseInventory(currentCursedMaster.inventory);
        currentCursedMaster.inventory.onInventoryChanged += CurrentCursedMaster_OnInventoryChanged;
    }

    private void CurrentCursedMaster_OnInventoryChanged()
    {
        foreach (ItemIndex currentItemToSend in GetItemsToTransfer(currentCursedMaster.inventory))
        {
            Log.Message($"Inventory item : {currentItemToSend}");
        }
    }

    private void CurseInventory(Inventory inventory)
	{
        currentCursedMaster.inventory.GiveItem(DLC1Content.Items.LunarSun, 1);
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F2))
		{
            var instances = PlayerCharacterMasterController.instances;
            // get a random player with an inventory that isn't the current one and swap with it
            List<PlayerCharacterMasterController> validPlayers = PlayerCharacterMasterController.instances.Where(instance => instance.master != currentCursedMaster).ToList();
            CharacterMaster targetMaster;
            if (validPlayers.Count == 0) // might just work actually since we filter out the current user and he 
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

        currentCursedMaster = targetMaster;
    }

    void GiveItems(CharacterMaster giver, CharacterMaster receiver)
    {
        ItemIndex currentItemToSend;
        ItemIndex[] itemsToTransfer = GetItemsToTransfer(giver.inventory);
        try
        {
            
            for (int i = 0; i < itemsToTransfer.Length; i++)
            {
                currentItemToSend = itemsToTransfer[i];
                giver.inventory.RemoveItem(currentItemToSend, giver.inventory.GetItemCount(currentItemToSend));
                ItemTransferOrb orb = ItemTransferOrb.DispatchItemTransferOrb(giver.bodyInstanceObject.GetComponent<CharacterBody>().corePosition, receiver.inventory, currentItemToSend, giver.inventory.GetItemCount(currentItemToSend), orb =>
                {
                    ItemTransferOrb.DefaultOnArrivalBehavior(orb);
                    inFlightOrbs.Remove(orb);
                });

                inFlightOrbs.Add(orb);
            }
        }
        catch
        {
            if(giver == null)
            {
                Log.Message("Giver inventory is null");
            }
            if(receiver == null)
            {
                Log.Message("Receiver inventory is null");
            }
            if(inFlightOrbs == null)
            {
                Log.Message("In flight orbs is null");
            }
            if(giver.gameObject.TryGetComponent(out CharacterBody giverBody))
            {
                Log.Message($"Giver body is : {giverBody}");
            }
            else
            {
                Log.Message("Giver body is null");
            }
            Debug.Log("SIZE OF FILTERED INVENTORY" + itemsToTransfer.Length);
            foreach (ItemIndex item in itemsToTransfer)
            {
                Log.Message($"Inventory item : {item}");
            }
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