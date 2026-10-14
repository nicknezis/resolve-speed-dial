namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // Keypad buttons that select the dial mode, like the JOG / SHTL / SCRL keys on the Speed Editor.
    // The active mode's tile is lit.
    public class DialModeCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor Active = new(0, 122, 204);
        private static readonly BitmapColor Inactive = new(28, 28, 30);

        public DialModeCommand()
        {
            this.DisplayName = "Dial Mode";
            this.GroupName = "Dial";
            this.Description = "Select the Speed Editor dial mode";

            this.AddParameter("JOG", "Dial mode: JOG", "Dial");
            this.AddParameter("SHTL", "Dial mode: SHTL (position)", "Dial");
            this.AddParameter("SCRL", "Dial mode: SCRL", "Dial");

            DialState.Changed += () => this.ActionImageChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            if (DialState.TryParse(actionParameter, out var mode))
            {
                DialState.SetMode(mode);
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            IsActive(actionParameter) ? $"{actionParameter}\n●" : actionParameter;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var builder = new BitmapBuilder(imageSize);
            builder.Clear(IsActive(actionParameter) ? Active : Inactive);
            var text = actionParameter;
            if (IsActive(actionParameter))
            {
                // Show the live status (e.g. shuttle speed) on the active tile.
                var status = DialModes.Get(DialState.Mode).Status;
                var detail = status.StartsWith(actionParameter, StringComparison.Ordinal) ? status[actionParameter.Length..].Trim() : status;
                text = String.IsNullOrEmpty(detail) ? actionParameter : $"{actionParameter}\n{detail}";
            }

            builder.DrawText(text, BitmapColor.White);
            return builder.ToImage();
        }

        private static Boolean IsActive(String actionParameter) =>
            DialState.TryParse(actionParameter, out var mode) && mode == DialState.Mode;
    }
}
