using System.Collections.Generic;
using System.Windows.Forms;

namespace NuGetMonitor
{
	public partial class History : Form
	{
		public int Count => flowLayoutPanel1.Controls.Count;

		public History()
		{
			InitializeComponent();
		}

		public void Add(OnVersionChangedArgs args)
		{
			var list = new List<HistoryItem>();
			foreach (HistoryItem control in flowLayoutPanel1.Controls)
				list.Add(control);
			flowLayoutPanel1.Controls.Clear();
			var item = new HistoryItem(args.Package, args.Version, args.Description, args.ReleaseNotes);
			item.Width = flowLayoutPanel1.Width-15;
			flowLayoutPanel1.Controls.Add(item);
			foreach (HistoryItem control in list)
				flowLayoutPanel1.Controls.Add(control);
		}

		private void History_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
}
