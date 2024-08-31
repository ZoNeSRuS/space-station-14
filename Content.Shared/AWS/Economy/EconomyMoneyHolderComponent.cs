using Content.Shared.Store;
using Robust.Shared.Prototypes;
namespace Content.Shared.AWS.Economy
{
    [RegisterComponent, AutoGenerateComponentState]
    public sealed partial class EconomyMoneyHolderComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public ProtoId<CurrencyPrototype> AllowCurrency;

        [ViewVariables(VVAccess.ReadWrite)]
        [AutoNetworkedField]
        public ulong Amount = 0;
    }
}
