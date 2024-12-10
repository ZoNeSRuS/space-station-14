using Content.Server.Mind;
using Content.Shared.Examine;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Whitelist;

namespace Content.Server.Corvax.HiddenDescription;

public sealed partial class HiddenDescriptionSystem : EntitySystem
{

    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelis = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HiddenDescriptionComponent> hiddenDesc, ref ExaminedEvent args)
    // SS220-fix-hidden-desc-fix-begin
    {
        PushExamineInformation(hiddenDesc.Comp, ref args);
    }

    public void PushExamineInformation(HiddenDescriptionComponent component, ref ExaminedEvent args)
    // SS220-fix-hidden-desc-fix-end
    {
        _mind.TryGetMind(args.Examiner, out var mindId, out var mindComponent);

        // foreach (var item in hiddenDesc.Comp.Entries) SS220-hidden-desc-fix
        foreach (var item in component.Entries) // SS220-hidden-desc-fix
        {
            var isJobAllow = false;
            if (_roles.MindHasRole<JobRoleComponent>((mindId, mindComponent), out var jobRole))
            {
                isJobAllow = jobRole.Value.Comp1.JobPrototype != null &&
                             item.JobRequired.Contains(jobRole.Value.Comp1.JobPrototype.Value);
            }

            var isMindWhitelistPassed = _whitelis.IsValid(item.WhitelistMind, mindId);
            var isBodyWhitelistPassed = _whitelis.IsValid(item.WhitelistMind, args.Examiner);
            var passed = item.NeedAllCheck
                ? isMindWhitelistPassed && isBodyWhitelistPassed && isJobAllow
                : isMindWhitelistPassed || isBodyWhitelistPassed || isJobAllow;

            if (passed)
                // args.PushMarkup(Loc.GetString(item.Label), hiddenDesc.Comp.PushPriority);
                args.PushMarkup(Loc.GetString(item.Label), component.PushPriority); // SS220-hidden-desc-fix
        }
    }
}
