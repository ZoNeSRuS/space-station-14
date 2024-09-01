using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Access.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.AWS.Economy
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
            SubscribeLocalEvent<EconomyMoneyHolderComponent, ExaminedEvent>(OnMoneyHolderExamine);
            /*SubscribeLocalEvent<EconomyBankAccountStorageComponent, ComponentInit>(OnStorageComponentInit);*/
            SubscribeLocalEvent<EconomyBankAccountComponent, ComponentInit>(OnAccountComponentInit);
            // SubscribeLocalEvent<EconomyBankAccountComponent, ComponentRemove>(OnAccountComponentRemove);

            SubscribeLocalEvent<EconomyBankATMComponent, ComponentInit>(OnATMComponentInit);
            SubscribeLocalEvent<EconomyBankATMComponent, ComponentRemove>(OnATMComponentRemove);
            SubscribeLocalEvent<EconomyBankATMComponent, EntInsertedIntoContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EntRemovedFromContainerMessage>(OnATMItemSlotChanged);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMWithdrawMessage>(OnATMWithdrawMessage);
            SubscribeLocalEvent<EconomyBankATMComponent, EconomyBankATMTransferMessage>(OnATMTransferMessage);
        }
        private void OnMoneyHolderExamine(Entity<EconomyMoneyHolderComponent> entity, ref ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("moneyholder-component-on-examine-detailed-message",
                ("moneyName", entity.Comp.AllowCurrency),
                ("amount", entity.Comp.Amount)));
        }
        private void OnAccountComponentInit(EntityUid entity, EconomyBankAccountComponent component, ComponentInit eventArgs)
        {
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

                    component.AccountId = component.AccountId.Length == 0 ? formedStrik : component.AccountId + proto.Descriptior + formedStrik;
                }
            }

            if (TryComp<IdCardComponent>(entity, out var idCardComponent))
                component.AccountName = idCardComponent.FullName ?? component.AccountName;

            /*storageComp.Accounts.Add(component);*/
            /*return;
            //}

            Log.Error("Cannot create EconomyBankAccountComponent without atleast 1 EconomyBankAccountStorageComponent!");*/
        }
        /*private void OnAccountComponentRemove(EntityUid entity, EconomyBankAccountComponent component, ComponentRemove eventArgs)
        {
            var storageComp = GetStationAccountStorage();
            if (storageComp is not null)
                if (storageComp.Accounts.Contains(component))
                    storageComp.Accounts.Remove(component);
        }*/
        private void OnATMComponentInit(EntityUid uid, EconomyBankATMComponent atm, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, EconomyBankATMComponent.ATMCardId, atm.CardSlot);

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

            string? error;

            TryWithdraw(bankAccount, atm, args.Amount, out error);
            /*DropMoney(bankAccount, args.Amount, Comp<TransformComponent>(uid).MapPosition);*/
            UpdateATMUserInterface((uid, atm), error);
        }

        private void OnATMTransferMessage(EntityUid uid, EconomyBankATMComponent atm, EconomyBankATMTransferMessage args)
        {
            var bankAccount = GetATMInsertedAccount(atm);
            if (bankAccount is null)
                return;

            string? error;

            TrySendMoney(bankAccount, atm, args.RecipientAccountId, args.Amount, out error);
            UpdateATMUserInterface((uid, atm), error);
        }

        public EconomyBankAccountComponent? FindAccountById(string id)
        {
            var enumerator = AllEntityQuery<EconomyBankAccountComponent>();
            while (enumerator.MoveNext(out var _, out var comp))
            {
                if (comp.AccountId == id)
                    return comp;
            }
            return null;
        }

        private void Withdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum)
        {
            component.Balance -= sum;
            var pos = Comp<TransformComponent>(atm.Owner).MapPosition;
            DropMoneyHandler(component.MoneyHolderProto, sum, pos);
            // log about withdraw money for captain
        }

        public bool TryWithdraw(EconomyBankAccountComponent component, EconomyBankATMComponent atm, ulong sum, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = "";
            if (sum > 0 && component.Balance >= sum)
            {
                Withdraw(component, atm, sum);
                return true;
            }
            errorMessage = "not enough money";
            return false;
        }

        private void DropMoneyHandler(ProtoId<EntityPrototype> proto, ulong amount, MapCoordinates pos)
        {
            var ent = Spawn(proto, pos);

            if (TryComp<EconomyMoneyHolderComponent>(ent, out var holderComp))
            {
                holderComp.Amount = amount;
                Dirty(holderComp);
            }
                // log about drop money for admins
            }

        private void SendMoney(EconomyBankAccountComponent fromAccount, EconomyBankAccountComponent toSend, ulong amount)
        {
            fromAccount.Balance -= amount;
            toSend.Balance += amount;

            Dirty(fromAccount);
            Dirty(toSend);
            // log about send money for captain
        }

        public bool TrySendMoney(EconomyBankAccountComponent fromAccount, EconomyBankATMComponent atm, string recipientAccountId, ulong amount, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;

            var recipientAccount = FindAccountById(recipientAccountId);

            if (fromAccount.Balance >= amount)
            {
                if (recipientAccount is not null)
                {
                    SendMoney(fromAccount, recipientAccount, amount);
                    return true;
                }

                errorMessage = "didn't find account";
                return false;
            }

            errorMessage = "not enough money";
            return false;
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
