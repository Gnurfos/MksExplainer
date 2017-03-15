
namespace Explainer
{

    public class PartExplainerModule : PartModule
    {
        [KSPEvent (active = true, guiActive = true, guiName = "Explain")]
        private void Explain()
        {
            ExplainerGui.SelectPart(part);
        }
    }
}
