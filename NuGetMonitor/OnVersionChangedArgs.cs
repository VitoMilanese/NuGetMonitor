namespace NuGetMonitor
{
	public class OnVersionChangedArgs
	{
		public string Package { get; set; }
		public string Version { get; set; }
		public string Description { get; set; }
		public string ReleaseNotes { get; set; }
		public string Time { get; set; }

		public OnVersionChangedArgs(string package, string version, string description, string releaseNotes)
		{
			Package = package;
			Version = version;
			Description = description;
			ReleaseNotes = releaseNotes;
		}
	}
}
