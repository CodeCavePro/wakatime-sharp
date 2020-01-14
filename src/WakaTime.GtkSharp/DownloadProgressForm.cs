using Gtk;
using WakaTime;
using System.ComponentModel;
using System.Net;
using GLib;

public partial class DownloadProgressForm: Window, IDownloadProgressReporter
{
    public DownloadProgressForm(Window parent)
        : base(WindowType.Toplevel)
    {
        Build();

        TransientFor = parent;
        SetPosition(WindowPosition.CenterOnParent);
    }

    void OnDeleteEvent(object sender, SignalArgs a)
    {
        a.RetVal = true;
        Destroy();
    }

    public void Show(string message = "")
    {
        Gtk.Application.Invoke(delegate
            {
                progressbar1.Text = message;
                base.Show();
            });
    }

    public void Close(AsyncCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            MessageBox.Show(e.Error.Message);
        }

        Destroy();
    }

    public void Report(DownloadProgressChangedEventArgs e)
    {
        Gtk.Application.Invoke(delegate
        {
            progressbar1.Fraction = e.ProgressPercentage;
            progressbar1.Window.ProcessUpdates(true); // Request visual update
            while (Gtk.Application.EventsPending())
                Gtk.Application.RunIteration(true); // Process any messages waiting in the Application Message Loop
        });
    }
}