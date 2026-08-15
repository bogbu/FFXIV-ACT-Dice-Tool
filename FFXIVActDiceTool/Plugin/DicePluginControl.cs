using System;
using System.Linq;
using System.Windows.Forms;
using FFXIVActDiceTool.Models;

namespace FFXIVActDiceTool.Plugin
{
    public sealed class DicePluginControl : UserControl
    {
        private const int MaximumVisibleRows = 2000;
        private readonly DicePluginController _controller;
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _start = new Button();
        private readonly Button _stop = new Button();
        private readonly Label _startValue = new Label();
        private readonly Label _endValue = new Label();
        private readonly Label _playersValue = new Label();
        private readonly Label _rollsValue = new Label();
        private readonly Label _highestValue = new Label();
        private readonly Label _lowestValue = new Label();
        private readonly Label _rankResult = new Label();
        private readonly RadioButton _highest = new RadioButton();
        private readonly NumericUpDown _rank = new NumericUpDown();

        public DicePluginControl(DicePluginController controller)
        {
            _controller = controller;
            BuildUi();
            _controller.RollAdded += OnRollAdded;
            _controller.SessionChanged += OnSessionChanged;
            ShowSnapshot(new SessionSnapshot(false, null, null, new DiceSessionResult()), true);
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), RowCount = 3, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var heading = new Label { AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold), Text = "FFXIV Dice Tool\r\nACT 로그 연결됨", Padding = new Padding(0, 0, 0, 8) };
            root.Controls.Add(heading, 0, 0);
            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            _start.Text = "집계 시작"; _stop.Text = "집계 종료"; var reset = new Button { Text = "초기화", AutoSize = true };
            _start.AutoSize = _stop.AutoSize = true; buttons.Controls.AddRange(new Control[] { _start, _stop, reset }); root.Controls.Add(buttons, 0, 1);
            _start.Click += delegate { _controller.StartSession(); }; _stop.Click += delegate { _controller.StopSession(); }; reset.Click += delegate { _controller.ResetSession(); };

            var content = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 650, Panel1MinSize = 300, Panel2MinSize = 260 };
            root.Controls.Add(content, 0, 2);
            var rollsGroup = new GroupBox { Text = "실시간 주사위", Dock = DockStyle.Fill, Padding = new Padding(8) };
            ConfigureGrid(); rollsGroup.Controls.Add(_grid); content.Panel1.Controls.Add(rollsGroup);
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.Controls.Add(BuildResults(), 0, 0); right.Controls.Add(BuildRank(), 0, 1); content.Panel2.Controls.Add(right);
            Controls.Add(root);
        }

        private void ConfigureGrid()
        {
            _grid.Dock = DockStyle.Fill; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false;
            _grid.RowHeadersVisible = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.Columns.Add("Timestamp", "시간"); _grid.Columns.Add("Player", "플레이어"); _grid.Columns.Add("Value", "값");
            _grid.Columns[0].FillWeight = 35; _grid.Columns[1].FillWeight = 45; _grid.Columns[2].FillWeight = 20;
        }

        private Control BuildResults()
        {
            var group = new GroupBox { Text = "결과", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
            AddResultRow(table, "시작 시간", _startValue); AddResultRow(table, "종료 시간", _endValue); AddResultRow(table, "참여 인원", _playersValue);
            AddResultRow(table, "총 굴림", _rollsValue); AddResultRow(table, "최고", _highestValue); AddResultRow(table, "최저", _lowestValue);
            group.Controls.Add(table); return group;
        }
        private static void AddResultRow(TableLayoutPanel table, string caption, Label value)
        {
            var row = table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(new Label { Text = caption + ":", AutoSize = true, Margin = new Padding(3, 4, 10, 4) }, 0, row);
            value.AutoSize = true; value.MaximumSize = new System.Drawing.Size(300, 0); value.Margin = new Padding(3, 4, 3, 4); table.Controls.Add(value, 1, row);
        }
        private Control BuildRank()
        {
            var group = new GroupBox { Text = "순위 조회 (DenseRank)", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            _highest.Text = "높은 순"; _highest.Checked = true; var lowest = new RadioButton { Text = "낮은 순", AutoSize = true };
            _highest.AutoSize = true; _rank.Minimum = 1; _rank.Maximum = 9999; _rank.Value = 1; _rank.Width = 65;
            var query = new Button { Text = "순위 조회", AutoSize = true }; _rankResult.AutoSize = true; _rankResult.MaximumSize = new System.Drawing.Size(300, 0);
            query.Click += delegate { ShowRank(_controller.QueryRank((int)_rank.Value, _highest.Checked ? RankDirection.Highest : RankDirection.Lowest)); };
            flow.Controls.AddRange(new Control[] { _highest, lowest, new Label { Text = "N 순위:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, _rank, query, _rankResult });
            group.Controls.Add(flow); return group;
        }

        private void OnRollAdded(object sender, DiceRollEventArgs e) { RunOnUi(delegate { _grid.Rows.Add(e.Entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), e.Entry.PlayerName, e.Entry.RollValue); while (_grid.Rows.Count > MaximumVisibleRows) _grid.Rows.RemoveAt(0); ShowSnapshot(e.Snapshot, false); }); }
        private void OnSessionChanged(object sender, SessionChangedEventArgs e) { RunOnUi(delegate { ShowSnapshot(e.Snapshot, true); }); }
        private void ShowSnapshot(SessionSnapshot snapshot, bool clearRows)
        {
            if (clearRows) { _grid.Rows.Clear(); _rankResult.Text = "조회 전"; }
            _start.Enabled = !snapshot.IsRunning; _stop.Enabled = snapshot.IsRunning;
            _startValue.Text = FormatTime(snapshot.StartTime); _endValue.Text = FormatTime(snapshot.EndTime);
            _playersValue.Text = snapshot.Result.UniquePlayerCount.ToString(); _rollsValue.Text = snapshot.Result.TotalRollCount.ToString();
            _highestValue.Text = FormatRolls(snapshot.Result.HighestRolls); _lowestValue.Text = FormatRolls(snapshot.Result.LowestRolls);
        }
        private void ShowRank(RankQueryResult result) { _rankResult.Text = !result.Exists ? "결과 없음" : string.Format("{0}위 / 값 {1}: {2}", result.RequestedRank, result.RollValue, string.Join(", ", result.Entries.Select(x => string.Format("{0}({1})", x.PlayerName, x.RollValue)))); }
        private static string FormatTime(DateTime? time) { return time.HasValue ? time.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-"; }
        private static string FormatRolls(System.Collections.Generic.IList<DiceRollEntry> entries) { return entries.Count == 0 ? "-" : string.Format("{0} ({1})", entries[0].RollValue, string.Join(", ", entries.Select(x => x.PlayerName))); }
        private void RunOnUi(Action action) { if (IsDisposed || Disposing) return; if (InvokeRequired) { try { BeginInvoke(action); } catch (InvalidOperationException) { } return; } action(); }
        protected override void Dispose(bool disposing) { if (disposing) { _controller.RollAdded -= OnRollAdded; _controller.SessionChanged -= OnSessionChanged; } base.Dispose(disposing); }
    }
}
