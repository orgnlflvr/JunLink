using System.Drawing;
namespace JunLinkPet;

// a small text box next to the pet: type, press Enter
sealed class TalkForm : Form
{
    readonly TextBox tb = new TextBox();
    readonly Action<string> send;

    protected override bool ShowWithoutActivation { get { return false; } }

    public TalkForm(Form owner, Action<string> onSend)
    {
        send = onSend;
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(211, 16, 79); Padding = new Padding(3); Size = new Size(380, 36);
        tb.BorderStyle = BorderStyle.None; tb.BackColor = Color.FromArgb(18, 8, 31); tb.ForeColor = Color.White; tb.Font = FontLoader.Body(10f);
        tb.Dock = DockStyle.Fill; tb.MaxLength = 500;
        Controls.Add(tb);
        tb.KeyDown += (o, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; var t = tb.Text.Trim(); Close(); if (t.Length > 0) send(t); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
        };
        Deactivate += (o, e) => { if (!IsDisposed) Close(); };
        Shown += (o, e) => { Activate(); tb.Focus(); };
    }
}
