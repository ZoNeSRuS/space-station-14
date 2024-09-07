using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using FastAccessors;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Emag.Components;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        private VendingMachineSystem _vendingMachineSystem;

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _vendingMachineSystem = EntMan.System<VendingMachineSystem>();
        }

        protected override void Open()
        {
            base.Open();

            var vendingMachineSys = EntMan.System<VendingMachineSystem>();

            _cachedInventory = vendingMachineSys.GetAllInventory(Owner);

            _menu = new VendingMachineMenu { Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName };

            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;
            _menu.OnSearchChanged += OnSearchChanged;

            _menu.Populate(_cachedInventory, out _cachedFilteredIndex);

            _menu.OpenCenteredLeft();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory;

            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, _menu.SearchBar.Text);
        }

        private VendingMachineInventoryEntry? GetEntry(EntityUid uid, VendingMachineComponent component)
        {
            string selectedId = component.SelectedItemId!;
            if (component.SelectedItemInventoryType == InventoryType.Emagged && EntMan.HasComponent<EmaggedComponent>(uid))
                return component.EmaggedInventory.GetValueOrDefault(selectedId);

            if (component.SelectedItemInventoryType == InventoryType.Contraband && component.Contraband)
                return component.ContrabandInventory.GetValueOrDefault(selectedId);

            return component.Inventory.GetValueOrDefault(selectedId);
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(args.ItemIndex));

            if (selectedItem == null)
                return;

            // SS14RU
            // SendMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
            /*if (EntMan.TryGetComponent<VendingMachineComponent>(Owner, out var vendingMachineComponent))
            {
                var entry = GetEntry(Owner, vendingMachineComponent);
                if (entry is not null)
                {
                    if (entry.Price > 0)
                    {
                        SendMessage(new VendingMachineSelectMessage(selectedItem.Type, selectedItem.ID));
                    }
                }
            }*/
            //if (EntMan.TryGetComponent<VendingMachineComponent>(Owner, out var vendingMachineComponent))

            SendMessage(new VendingMachineSelectMessage(selectedItem.Type, selectedItem.ID));
        // SS14RU
    }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
            _menu.Dispose();
        }

        private void OnSearchChanged(string? filter)
        {
            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, filter);
        }
    }
}
