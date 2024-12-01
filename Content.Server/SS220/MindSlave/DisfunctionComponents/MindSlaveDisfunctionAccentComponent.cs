// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.MindSlave.DisfunctionComponents;

[RegisterComponent]
public sealed partial class MindSlaveDisfunctionAccentComponent : Component
{
    [DataField]
    public float Prob = 0.33f;
}
