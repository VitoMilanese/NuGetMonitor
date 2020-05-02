using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NuGetMonitor
{
	public partial class Form1 : Form
	{
		private string DefaultRepo { get; } = @"\\192.168.253.68\NugetPackages";
		//private string DefaultRepo { get; } = @"C:\Sources\Local\NuGetMonitor\NuGetMonitor\bin\Debug\test";
		private string StartupPath => Application.StartupPath;
		private string RepoFile => $@"{StartupPath}\repo.ini";
		private string BlackListFile => $@"{StartupPath}\blacklist.ini";
		private string HistoryList => $@"{StartupPath}\history.ini";

		private bool _locker { get; set; }
		private bool _canClose { get; set; }
		private string _repo { get; set; }
		private List<string> _blackList { get; set; }
		private List<string> _packages { get; set; }
		private Dictionary<string, string> _versions { get; set; }

		private History _history { get; }

		private EventHandler<OnVersionChangedArgs> OnVersionChanged { get; set; }

		public Form1()
		{
			InitializeComponent();
			Init();
			_history = new History();

			OnVersionChanged += (o, e) =>
			{
				_versions[e.Package] = e.Version;
				notifyIcon1.ShowBalloonTip(0, @"New version", $"NuGet: {e.Package}\r\nver: {e.Version}", ToolTipIcon.Info);
				_history.Add(e);
				SaveHistoryList();
			};
		}

		private void Init()
		{
			if (!File.Exists(RepoFile))
			{
				_repo = DefaultRepo;
				File.WriteAllText(RepoFile, DefaultRepo);
			}
			else
				_repo = File.ReadAllLines(RepoFile)[0];

			_blackList?.Clear();
			if (!File.Exists(BlackListFile))
			{
				_blackList = new List<string>();
				File.WriteAllText(BlackListFile, String.Empty);
			}
			else
				_blackList = new List<string>(File.ReadAllLines(BlackListFile));

			_versions?.Clear();
			_versions = new Dictionary<string, string>();
			if (!File.Exists(HistoryList))
			{
				File.WriteAllText(HistoryList, String.Empty);
			}
			else
			{
				var list = new List<string>(File.ReadAllLines(HistoryList));
				foreach (var item in list.Select(i => i.Split('\t')))
					try
					{
						_versions.Add(item[0], item[1]);
					}
					catch
					{
					}
			}
		}

		private bool GetPackageList()
		{
			if (_locker) return false;
			_locker = true;

			try
			{
				_packages = Directory.GetDirectories(_repo).Select(p => Path.GetFileName(p)).ToList();
			}
			catch
			{
				_locker = false;
				return false;
			}

			var sel = -1;
			checkedListBox1.Invoke((MethodInvoker)delegate
			{
				sel = checkedListBox1.SelectedIndex;
				checkedListBox1.Items.Clear();
			});
			if (_packages == null)
			{
				_locker = false;
				return false;
			}

			foreach (var package in _packages)
			{
				var black = _blackList.Contains(package);
				checkedListBox1.Invoke((MethodInvoker)delegate
				{
					checkedListBox1.Items.Add(package, black ? CheckState.Unchecked : CheckState.Checked);
				});

				if (!black)
				{
					var version = GetLatestVersion(package, out var description, out var releaseNotes);
					if (_versions.Keys.Contains(package))
					{
						if (_versions[package] != version)
							Invoke((OnVersionChangedDelegate)VersionChanged, new OnVersionChangedArgs(package, version, description, releaseNotes));
						//OnVersionChanged?.Invoke(this, new OnVersionChangedArgs(package, version, description, releaseNotes));
					}
					else
						_versions.Add(package, version);
				}
			}
			checkedListBox1.Invoke((MethodInvoker)delegate
			{
				checkedListBox1.SelectedIndex = sel;
			});

			_locker = false;

			return true;
		}

		private delegate void OnVersionChangedDelegate(OnVersionChangedArgs args);
		private void VersionChanged(OnVersionChangedArgs args)
		{
			OnVersionChanged?.Invoke(this, args);
		}

		private string GetLatestVersion(string package, out string description, out string releaseNotes)
		{
			description = @"No description";
			releaseNotes = @"No release notes";
			var versions = Directory.GetDirectories($@"{_repo}\{package}")
				.Select(p => Path.GetFileName(p))
				.OrderByDescending(p => p)
				.ToList();
			if (versions.Count == 0) return @"Empty";
			versions.Sort(OrderByVersion);
			var version = versions.FirstOrDefault();
			if (version.ToLower().Contains(@"rc"))
			{
				version.Replace(@"RC", string.Empty).Replace(@"rc", string.Empty);
				version = $"{version}RC";
			}

			try
			{
				var nuspec = XDocument.Load($@"{_repo}\{package}\{version}\{package}.nuspec");

				var descriptionElement = nuspec?.Root?.Elements()?.FirstOrDefault()?.Nodes()?.ToList()?
					.FirstOrDefault(e => e is XElement && ((XElement)e).Name.LocalName.ToLower().Equals(@"description"));
				var releaseNotesElement = nuspec?.Root?.Elements()?.FirstOrDefault()?.Nodes()?.ToList()?
					.FirstOrDefault(e => e is XElement && ((XElement)e).Name.LocalName.ToLower().Equals(@"releasenotes"));

				var description0 = descriptionElement == null || !(descriptionElement is XElement) ? string.Empty : ((XElement)descriptionElement).Value;
				description = string.IsNullOrWhiteSpace(description0) ? description : description0;
				var releaseNotes0 = releaseNotesElement == null || !(releaseNotesElement is XElement) ? string.Empty : ((XElement)releaseNotesElement).Value;
				releaseNotes = string.IsNullOrWhiteSpace(releaseNotes0) ? releaseNotes : releaseNotes0;
			}
			catch
			{
			}

			return version;
		}

		private int OrderByVersion(string version1, string version2)
		{
			var ver1 = version1.Replace(@"RC", string.Empty).Replace(@"rc", string.Empty).Split('.');
			var ver2 = version2.Replace(@"RC", string.Empty).Replace(@"rc", string.Empty).Split('.');

			for (var i = 0; i < ver1.Length; ++i)
				if (ver1[i] != ver2[i])
				{
					var a = int.MinValue;
					var b = int.MinValue;
					try
					{
						a = int.Parse(ver2[i]);
						b = int.Parse(ver1[i]);
						return a - b;
					}
					catch
					{
						return a == int.MinValue ? -1 : 1;
					}
				}

			return 0;
		}

		private void Timer1_Tick(object sender, EventArgs e)
		{
			new Thread(() => GetPackageList()).Start();
			((System.Windows.Forms.Timer)sender).Interval = 5000;
		}

		private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_canClose = true;

			_repo = DefaultRepo;
			SaveRepo();
			SaveBlackList();
			SaveHistoryList();

			Close();
		}

		private void SaveRepo() => File.WriteAllText(RepoFile, DefaultRepo);
		private void SaveBlackList() => File.WriteAllLines(BlackListFile, _blackList);
		private void SaveHistoryList() => File.WriteAllLines(HistoryList, _versions.Select(v => $"{v.Key}\t{v.Value}"));

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = !_canClose;
			if (!_canClose)
			{
				Hide();
				showToolStripMenuItem.Visible = true;
			}
		}

		private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				_history.Show();
		}

		private void showToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Show();
			showToolStripMenuItem.Visible = false;
		}

		private void UpdateListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_history.Show();
		}

		private void NotifyIcon1_BalloonTipClicked(object sender, EventArgs e)
		{
			_history.Show();
		}

		private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			var package = checkedListBox1.Items[e.Index].ToString();
			switch(e.NewValue)
			{
				case CheckState.Unchecked:
					if (!_blackList.Contains(package))
						_blackList.Add(package);
					break;
				case CheckState.Checked:
					if (_blackList.Contains(package))
						_blackList.Remove(package);
					break;
			}
			SaveBlackList();
		}
	}
}
