using Robust.Shared.Prototypes;
using Content.Shared.Store;
using Robust.Shared.GameStates;

namespace Content.Shared.AWS.Economy
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class EconomyBankAccountComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<CurrencyPrototype> AllowCurrency;
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<EconomyAccountIdPrototype> AccountIdByProto;
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<EntityPrototype> MoneyHolderProto;

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public ulong Balance = 0;
        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public ulong Penalty = 0;

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public string AccountId = "NO VALUE";
        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public string AccountName = "UNEXPECTED USER";

        [ViewVariables(VVAccess.ReadWrite), DataField]
        [AutoNetworkedField]
        public bool Blocked = false;
    }
}
