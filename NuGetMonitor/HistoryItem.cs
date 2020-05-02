using System;
using System.Windows.Forms;

namespace NuGetMonitor
{
	public partial class HistoryItem : UserControl
	{
		private string _package { get; set; }
		public string Package
		{
			get => _package;
			set
			{
				_package = value;
				label3.Text = value;
			}
		}

		private string _version { get; set; }
		public string Version
		{
			get => _version;
			set
			{
				_version = value;
				label4.Text = value;
			}
		}

		private string _time { get; set; }
		public string Time
		{
			get => _time;
			set
			{
				_time = value;
				label6.Text = value;
			}
		}

		private string _description { get; set; }
		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				label7.Text = value;
			}
		}

		private string _releaseNotes { get; set; }
		public string ReleaseNotes
		{
			get => _releaseNotes;
			set
			{
				_releaseNotes = value;
				label8.Text = value;
			}
		}

		public HistoryItem(string package, string version, string description, string releaseNotes, DateTime? time = null)
		{
			InitializeComponent();
			if (time == null)
				time = (DateTime?) DateTime.Now;
			Package = package;
			Version = version;
			Description = description;
			ReleaseNotes = releaseNotes;
			Time = time.Value.ToString(@"HH:mm:ss   dd/MM/yyyy");
		}
	}
}
