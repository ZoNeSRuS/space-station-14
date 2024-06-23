using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.AW.Economy
{
    public sealed partial class EconomyBankAccountSystem : EntitySystem
    {
        [Dependency] protected readonly ItemSlotsSystem _itemSlotsSystem = default!;
        const uint MinPaydayPrecent = 25;
        const uint MaxPaydayPrecent = 75;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EconomyBankAccountStorageComponent, ComponentInit>(OnStorageComponentInit);
            SubscribeLocalEvent<EconomyBankAccountComponent, ComponentInit>(OnAccountComponentInit);
            SubscribeLocalEvent<EconomyBankAccountComponent, ComponentRemove>(OnAccountComponentRemove);

            SubscribeLocalEvent<EconomyBankATMComponent, ComponentInit>(OnATMComponentInit);
            SubscribeLocalEvent<EconomyBankATMComponent, ComponentRemove>(OnATMComponentRemove);
        }
        private void OnStorageComponentInit(EntityUid entity, EconomyBankAccountStorageComponent component, ComponentInit eventArgs)
        {
            if (GetStationAccountStorage() is not null)
            {
                EntityManager.RemoveComponent(entity, component);
                Log.Error("Cannot create more than 1 entities with EconomyBankAccountStorageComponent!");
            }
        }
        private void OnAccountComponentInit(EntityUid entity, EconomyBankAccountComponent component, ComponentInit eventArgs)
        {
            var storageComp = GetStationAccountStorage();
            if (storageComp is not null)
            {
                storageComp.Accounts.Add(component);
                return;
            }

            Log.Error("Cannot create EconomyBankAccountComponent without atleast 1 EconomyBankAccountStorageComponent!");
        }
        private void OnAccountComponentRemove(EntityUid entity, EconomyBankAccountComponent component, ComponentRemove eventArgs)
        {
            var storageComp = GetStationAccountStorage();
            if (storageComp is not null)
                if (storageComp.Accounts.Contains(component))
                    storageComp.Accounts.Remove(component);
        }
        private void OnATMComponentInit(EntityUid uid, EconomyBankATMComponent atm, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, EconomyBankATMComponent.ATMCardId, atm.CardSlot);

            // UpdatePdaAppearance(uid, pda);
        }
        private void OnATMComponentRemove(EntityUid uid, EconomyBankATMComponent atm, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, atm.CardSlot);
        }

        public EconomyBankAccountStorageComponent? GetStationAccountStorage()
        {
            AllEntityQuery<EconomyBankAccountStorageComponent>().MoveNext(out var _, out var comp);

            return comp;
        }

        public EconomyBankAccountComponent? FindAccountById(string id)
        {
            return null;
        }

        public bool TryWithdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = "";
            if (component.Balance >= sum)
                return true;
            return false;
        }

        public void Withdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum)
        {
            
        }
    }
}