using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SchemaZen.Library.Models;

namespace SchemaZen.Library.Command {
	public class CompareCommand : BaseCommand {
		public string Source { get; set; }
		public string Target { get; set; }
		public bool Verbose { get; set; }
		public string OutDiff { get; set; }

		public string VersionNumber { get; set; }

		public bool Execute() {
			var sourceDb = new Database();
			var targetDb = new Database();
			sourceDb.Connection = Source;
			targetDb.Connection = Target;
			sourceDb.Load();
			targetDb.Load();
			if (Verbose)
			{
				//File.WriteAllText(OutDiff + ".routines.source", sourceDb.DumpRoutines());
				//File.WriteAllText(OutDiff + ".routines.target", targetDb.DumpRoutines());
			}
			var diff = sourceDb.Compare(targetDb);
			if (diff.IsDiff) {
				Console.WriteLine("Databases are different.");
				Console.WriteLine(diff.SummarizeChanges(Verbose));
				File.WriteAllText(OutDiff + ".diff", diff.SummarizeChanges(Verbose));
				if (!string.IsNullOrEmpty(OutDiff)) {
					Console.WriteLine();
					if (!Overwrite && File.Exists(OutDiff)) {
						var message = $"{OutDiff} already exists - set overwrite to true if you want to delete it";
						throw new InvalidOperationException(message);
					}

					string diffScript = diff.Script();
					if (VersionNumber!=null)
					{
						diffScript += "-- INSERT INTO [dbo].[SystemAppVersions] ([AppDbVersion],[AppWebVersion],[LastChangeDate]) \n";
						diffScript += "-- VALUES(" + VersionNumber.Replace(".","") + ", '" + VersionNumber + "', GETDATE())\n";
						diffScript += "-- GO";
					}

					File.WriteAllText(OutDiff, diffScript);
					Console.WriteLine($"Script to make the databases identical has been created at {Path.GetFullPath(OutDiff)}");
				}
				return true;
			}
			Console.WriteLine("Databases are identical.");
			return false;
		}
	}
}
