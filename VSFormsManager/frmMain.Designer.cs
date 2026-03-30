namespace VSFormsManager
{
    partial class frmMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        // UI is built entirely in code in frmMain.cs → BuildUi().
        private void InitializeComponent()
        {
            components    = new System.ComponentModel.Container();
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        }
    }
}
