using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SchemaZen.Library.Command {
	public class ImportCommand : BaseCommand {

		public void Execute(Dictionary<string, string> namesAndSchemas, string dataTablesPattern, string dataTablesExcludePattern,
            string tableHint, List<string> filteredTypes) {
			if (!Directory.Exists(ScriptDir)) {
				var message = $"{ScriptDir} not exists - you must set import dir";
				throw new InvalidOperationException(message);
			}

			var db = CreateDatabase(filteredTypes);

			Logger.Log(TraceLevel.Verbose, "Loading database schema...");
			db.Load();
			Logger.Log(TraceLevel.Verbose, "Database schema loaded.");

			foreach (var nameAndSchema in namesAndSchemas) {
				AddDataTable(db, nameAndSchema.Key, nameAndSchema.Value);
			}

			db.ImportData(Logger.Log);

			Logger.Log(TraceLevel.Info, $"{Environment.NewLine} successfully created at {db.Dir}");
			var routinesWithWarnings = db.Routines.Select(r => new {
				Routine = r,
				Warnings = r.Warnings().ToList()
			}).Where(r => r.Warnings.Any()).ToList();
			if (routinesWithWarnings.Any()) {
				Logger.Log(TraceLevel.Info, "With the following warnings:");
				foreach (
					var warning in
						routinesWithWarnings.SelectMany(
							r =>
								r.Warnings.Select(
									w => $"- {r.Routine.RoutineType} [{r.Routine.Owner}].[{r.Routine.Name}]: {w}"))) {
					Logger.Log(TraceLevel.Warning, warning);
				}
			}
		}
	}
}
