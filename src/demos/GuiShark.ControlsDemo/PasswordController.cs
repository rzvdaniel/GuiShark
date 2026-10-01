namespace GuiShark.ControlsDemo;

/// <summary>Demonstrates masking and reveal; no credentials are logged or sent anywhere.</summary>
internal sealed class PasswordController
{
    public PasswordController(UiView view, Action<string> log)
    {
        var document = view.Document;
        BindInline(document, log);
        var dialog = document.GetElement("password-dialog").Dialog!;
        var password = document.GetElement("lobby-password").TextInput!;
        var reveal = document.GetElement("show-password").Control!;
        var join = document.GetElement("join-lobby");
        document.GetElement("open-lobby").Clicked += _ => dialog.ShowModal();
        document.GetElement("cancel-lobby").Clicked += _ => dialog.Close("cancel");
        reveal.Changed += control => password.ShowPassword = control.Checked;
        password.Changed += _ => join.Disabled = password.Value.Length == 0;
        void Submit()
        {
            if (password.Value.Length == 0) return;
            // A real host would authenticate here; the gallery only demonstrates UI state.
            dialog.Close("join");
            log("Private camp join requested / demo only");
        }
        join.Clicked += _ => Submit();
        password.Submitted += _ => Submit();
        dialog.Closed += _ => { reveal.Checked = false; password.ShowPassword = false; password.Value = ""; };
        join.Disabled = password.Value.Length == 0;
    }
    private static void BindInline(UiDocument document, Action<string> log)
    {
        var password = document.GetElement("inline-password").TextInput!;
        var reveal = document.GetElement("inline-show-password").Control!;
        var join = document.GetElement("inline-join-lobby");
        reveal.Changed += control => password.ShowPassword = control.Checked;
        password.Changed += _ => join.Disabled = password.Value.Length == 0;
        void Submit()
        {
            if (password.Value.Length == 0) return;
            document.GetElement("inline-password-status").Text = "Join requested / demo only. Password cleared.";
            log("Private camp join requested from inline field / demo only");
            reveal.Checked = false;
            password.ShowPassword = false;
            password.Value = "";
        }
        password.Submitted += _ => Submit();
        join.Clicked += _ => Submit();
        join.Disabled = password.Value.Length == 0;
    }
}
