namespace AmpShell.System
{
    using global::System.Drawing;
    using global::System.Windows.Forms;

    public class DarkStripColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(30, 30, 30); }
        }

        public override Color MenuItemBorder
        {
            get { return Color.FromArgb(30, 30, 30); }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Color.FromArgb(30, 30, 30); }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return Color.FromArgb(30, 30, 30); }
        }
    }
}
