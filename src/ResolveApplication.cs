namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // Describes the target application. The plugin is universal (see ResolveSpeedDialPlugin), so this
    // does not create an application profile; the frontmost check lives in FrontmostApp.
    public class ResolveApplication : ClientApplication
    {
        public ResolveApplication()
        {
        }

        // Windows process name / macOS process name (DaVinci Resolve.app/Contents/MacOS/Resolve).
        protected override String GetProcessName() => "Resolve";

        // macOS bundle identifier.
        protected override String GetBundleName() => "com.blackmagic-design.DaVinciResolve";

        public override ClientApplicationStatus GetApplicationStatus() =>
            System.IO.Directory.Exists("/Applications/DaVinci Resolve/DaVinci Resolve.app")
            || System.IO.File.Exists(@"C:\Program Files\Blackmagic Design\DaVinci Resolve\Resolve.exe")
                ? ClientApplicationStatus.Installed
                : ClientApplicationStatus.NotInstalled;
    }
}
