using System;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Game;
using osu.Game.Overlays.Settings;

namespace UwpOsu;

/// <summary>
/// The osu! game for the Xbox UWP host. For now a plain <see cref="OsuGame"/>; console-specific overrides go here.
/// </summary>
public partial class XboxOsuGame : OsuGame
{
    private readonly Action<string> log;

    public XboxOsuGame(Action<string> log)
        : base()
    {
        this.log = log;

        // Native AOT cannot create mod-setting enum dropdowns by MakeGenericType; instantiate them statically (generated file).
        XboxEnumDropdowns.Register();
    }

    public override SettingsSubsection CreateSettingsSubsectionFor(InputHandler handler)
    {
        switch (handler)
        {
            case UwpGamepadHandler pad:
                return new XboxPadSettings(pad);

            default:
                return base.CreateSettingsSubsectionFor(handler);
        }
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        log("osu.Game LoadComplete");
        startImportWatcher();
    }

    private ImportInbox? importInbox;

    private void startImportWatcher()
    {
        try
        {
            string folder = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "import");
            importInbox = new ImportInbox(folder, log);
            log("import folder: " + folder);
            Scheduler.AddDelayed(scanImportFolder, 3000, true);
        }
        catch (Exception ex)
        {
            log("import folder setup failed: " + ex);
        }
    }

    private void scanImportFolder()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (importInbox != null)
                    await importInbox.ScanAsync(file => Import(new[] { file })).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log("import scan failed: " + ex);
            }
        });
    }
}
