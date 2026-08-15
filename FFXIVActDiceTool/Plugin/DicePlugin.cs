using System.Windows.Forms;
using Advanced_Combat_Tracker;
using FFXIVActDiceTool.ACT;
using FFXIVActDiceTool.Services;

namespace FFXIVActDiceTool.Plugin
{
    public sealed class DicePlugin : IActPluginV1
    {
        private TabPage _tab;
        private Label _status;
        private DicePluginController _controller;
        private DicePluginControl _control;

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            _tab = pluginScreenSpace; _status = pluginStatusText;
            _controller = new DicePluginController(new ActLogSource(), new DiceParser(), new DiceSessionManager(), new RankCalculator(), new DiceDuplicateFilter());
            _control = new DicePluginControl(_controller) { Dock = DockStyle.Fill };
            _tab.Controls.Add(_control);
            _controller.StartListening();
            _status.Text = "FFXIV Dice Tool 활성화됨";
        }

        public void DeInitPlugin()
        {
            if (_controller != null) _controller.Dispose();
            if (_tab != null && _control != null) _tab.Controls.Remove(_control);
            if (_control != null) _control.Dispose();
            if (_status != null) _status.Text = "FFXIV Dice Tool 비활성화됨";
            _controller = null; _control = null; _tab = null; _status = null;
        }
    }
}
