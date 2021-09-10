namespace AmpShell.System
{
    using global::System.Windows.Forms;

    public class DarkStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkStripRenderer()
            : base(new DarkStripColors())
        {
        }
    }
}
