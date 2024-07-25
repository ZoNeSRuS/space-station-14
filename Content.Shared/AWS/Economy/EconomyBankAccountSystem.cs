using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Access.Components;
using Robust.Shared.Containers;

namespace Content.Shared.AW.Economy
{
    public sealed partial class EconomyBankAccountSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;

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
            SubscribeLocalEvent<EconomyBankATMComponent, EntInsertedIntoContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EntRemovedFromContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMWithdrawMessage>(OnATMWithdrawMessage);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMTransferMessage>(OnATMTransferMessage);
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
                // string account_id = component.AccountIdByProto;

                if (_prototypeManager.TryIndex(component.AccountIdByProto, out EconomyAccountIdPrototype? proto))
                {
                    component.AccountId = proto.Prefix;

                    for (int strik = 0; strik < proto.Strik; strik++)
                    {
                        string formedStrik = "";
                        for (int num = 0; num < proto.NumbersPerStrik; num++)
                        {
                            formedStrik += _robustRandom.Next(0, 10);
                        }
                        component.AccountId += proto.Descriptior + formedStrik;
                    }
                }

                if (TryComp<IdCardComponent>(entity, out var idCardComponent))
                    component.AccountName = idCardComponent.FullName ?? component.AccountName;

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
            UpdateATMUserInterface((uid, atm));
        }
        private void OnATMComponentRemove(EntityUid uid, EconomyBankATMComponent atm, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, atm.CardSlot);
        }

        private void OnATMItemSlotChanged(EntityUid uid, EconomyBankATMComponent atm, ContainerModifiedMessage args)
        {
            if (args.Container.ID != atm.CardSlot.ID)
                return;

            UpdateATMUserInterface((uid, atm));
        }

        private void OnATMWithdrawMessage(EntityUid uid, EconomyBankATMComponent atm, EconomyBankATMWithdrawMessage args)
        {
            var bankAccount = GetATMInsertedAccount(atm);
            if (bankAccount is null)
                return;
            if (!TryWithdraw(bankAccount, atm, args.Amount, out var error))
                UpdateATMUserInterface((uid, atm), error);
        }

        private void OnATMTransferMessage(EntityUid uid, EconomyBankATMComponent atm, EconomyBankATMTransferMessage args)
        {
            var bankAccount = GetATMInsertedAccount(atm);
            if (bankAccount is null)
                return;
            if (!TryTransfer(bankAccount, (uid, atm), args.RecipientAccountId, args.Amount, out var error))
                UpdateATMUserInterface((uid, atm), error);
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
            // UI should be updated 
            //UpdateATMUserInterface(???);
        }

        public bool TryTransfer(EconomyBankAccountComponent fromAccount, Entity<EconomyBankATMComponent> atm, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;
            return true;
        }

        private void UpdateATMUserInterface(Entity<EconomyBankATMComponent> entity, string? error = null)
        {
            var bankAccount = GetATMInsertedAccount(entity.Comp);
            _userInterfaceSystem.SetUiState(entity.Owner, EconomyBankATMUiKey.Key, new EconomyBankATMUserInterfaceState()
            {
                BankAccount = bankAccount is null ? null :
                new() {
                    Balance = bankAccount.Balance,
                    AccountId = bankAccount.AccountId,
                    AccountName = bankAccount.AccountName,
                    Blocked = bankAccount.Blocked,
                },
                Error = error,
            });
        }

        private EconomyBankAccountComponent? GetATMInsertedAccount(EconomyBankATMComponent atm)
        {
            TryComp(atm.CardSlot.Item, out EconomyBankAccountComponent? bankAccount);
            return bankAccount;
        }
    }
}
