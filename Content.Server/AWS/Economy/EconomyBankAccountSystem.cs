using Content.Shared.Interaction;
using Content.Shared.VendingMachines;
using Content.Server.VendingMachines;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.AWS.Economy;
using Content.Server.Popups;

namespace Content.Server.AWS.Economy
{
    public sealed class EconomyBankAccountSystem : EconomyBankAccountSystemShared
    {
        [Dependency] private readonly VendingMachineSystem _vendingMachine = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<EconomyBankTerminalComponent, InteractUsingEvent>(OnTerminalInteracted);
            SubscribeLocalEvent<EconomyBankATMComponent, InteractUsingEvent>(OnATMInteracted);
        }
        private void OnATMInteracted(EntityUid uid, EconomyBankATMComponent component, InteractUsingEvent args)
        {
            var usedEnt = args.Used;

            if (!TryComp<EconomyMoneyHolderComponent>(usedEnt, out var economyMoneyHolderComponent))
                return;

            var amount = economyMoneyHolderComponent.Balance;
            var insertedAccountComponent = GetATMInsertedAccount(component);

            if (TrySendMoney(economyMoneyHolderComponent, GetATMInsertedAccount(component), amount, out var error))
            {
                if (_netManager.IsServer)
                    _popupSystem.PopupEntity(Loc.GetString("economybanksystem-atm-moneyentering"), uid, type: PopupType.Medium);

                QueueDel(usedEnt);
            }
            if (_netManager.IsServer)
                _popupSystem.PopupEntity(error, uid, type: PopupType.Medium);

            UpdateATMUserInterface((uid, component), error);
        }
        private void OnTerminalInteracted(EntityUid uid, EconomyBankTerminalComponent component, InteractUsingEvent args)
        {
            var amount = component.Amount;
            var usedEnt = args.Used;
            string? selectedItemId = null;

            if (amount <= 0)
                return;

            TryComp<EconomyMoneyHolderComponent>(usedEnt, out var economyMoneyHolderComponent);
            TryComp<EconomyBankAccountComponent>(usedEnt, out var economyBankAccountComponent);

            if (economyMoneyHolderComponent is null && economyBankAccountComponent is null)
                return;

            IEconomyMoneyHolder anyComp = (economyMoneyHolderComponent is not null ? economyMoneyHolderComponent : economyBankAccountComponent)!;

            if (TryComp<VendingMachineComponent>(uid, out var vendingMachineComponent))
            {
                if (vendingMachineComponent.SelectedItemId is not null)
                {
                    selectedItemId = vendingMachineComponent.SelectedItemId;
                    if (vendingMachineComponent.Inventory.TryGetValue(vendingMachineComponent.SelectedItemId, out var entry))
                    {
                        if (entry.Price > 0)
                        {
                            vendingMachineComponent.SelectedItemId = null;
                            goto sendMoney;
                        }
                    }
                    return;
                }
            }

        sendMoney:
            if (vendingMachineComponent is not null && selectedItemId is not null)
            {
                _vendingMachine.AuthorizedVend(uid, args.User, vendingMachineComponent.SelectedItemInventoryType, selectedItemId, vendingMachineComponent);
                if (vendingMachineComponent.Denying)
                    return;

            }

            if (!TrySendMoney(anyComp, component.LinkedAccount, amount, out var err))
            {
                if (_netManager.IsServer)
                    _popupSystem.PopupEntity(err, uid, type: PopupType.MediumCaution);
                return;
            }

            if (_netManager.IsServer)
                _popupSystem.PopupEntity(Loc.GetString("economybanksystem-transaction-success", ("amount", amount), ("currencyName", FindAccountById(component.LinkedAccount)!.AllowCurrency)), uid, type: PopupType.Medium);
        }
    }
}
