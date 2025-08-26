using System.Collections.Generic;
using System.Linq;
using SchemaZen.Library.Extensions;

namespace SchemaZen.Library.Models {
	public class Constraint : INameable, IScriptable {
		public string IndexType { get; set; }
		public List<ConstraintColumn> Columns { get; set; } = new List<ConstraintColumn>();
		public List<string> IncludedColumns { get; set; } = new List<string>();
		public string Name { get; set; }
		public Table Table { get; set; }
		public string Type { get; set; }
		public string Filter { get; set; }
		public bool Unique { get; set; }
		private bool _isNotForReplication;
		private string _checkConstraintExpression;

		public string ContraintDefinition;

		public Constraint(string name, string type, string columns) {
			Name = name;
			Type = type;
			if (!string.IsNullOrEmpty(columns)) {
				Columns = new List<ConstraintColumn>(columns.Split(',').Select(x => new ConstraintColumn(x, false)));
			}
		}

		public static Constraint CreateCheckedConstraint(string name, bool isNotForReplication, string checkConstraintExpression) {
			var constraint = new Constraint(name, "CHECK", "") {
				_isNotForReplication = isNotForReplication,
				_checkConstraintExpression = checkConstraintExpression
			};
			return constraint;
		}

		public static Constraint CreateDefaultConstraint(string name, string contraintDefinition,string columns)
		{
			var constraint = new Constraint(name, "DEFAULT_CONSTRAINT", columns)
			{
				Type = "D",
				ContraintDefinition = contraintDefinition
			};
			return constraint;
		}

		public string UniqueText => Type != " PRIMARY KEY" && !Unique ? "" : " UNIQUE";

		public string ScriptCreate() {
			var sql = string.Empty;
			switch (Type) {
				case "CHECK":
					var notForReplicationOption = _isNotForReplication ? "NOT FOR REPLICATION" : "";
					return $"CONSTRAINT [{Name}] CHECK {notForReplicationOption} {_checkConstraintExpression}";
				case "INDEX":
					if (Table.Name.ToUpper().StartsWith("ZZ"))
					{
						sql = $"-- [{Table.Owner}].[{Table.Name}].[{Name}]";
					}
					else
					{
						sql = $"IF NOT EXISTS(SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[{Table.Owner}].[{Table.Name}]') AND name = N'{Name}')";
						sql += $" CREATE{UniqueText}{IndexType.Space()} INDEX [{Name}] ON [{Table.Owner}].[{Table.Name}] ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
						if (IncludedColumns.Count > 0)
						{
							sql += $" INCLUDE ([{string.Join("], [", IncludedColumns.ToArray())}])";
						}
						if (!string.IsNullOrEmpty(Filter))
						{
							sql += $" WHERE {Filter}";
						}
					}
					return sql;
				case "PRIMARY KEY":
					if (Table.IsType)
					{
						sql = string.Empty;
					}else
					{
						sql = (Table.IsType ? string.Empty : $"CONSTRAINT [{Name}] ") + $"{Type}{IndexType.Space()} ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
					}
					return sql;
				case "ALTER INDEX":
					if (Table.IsType)
					{
						sql = string.Empty;
					}
					else
					{
						sql = $"ALTER TABLE [{Table.Owner}].[{Table.Name}] ADD CONSTRAINT [{Name}] PRIMARY KEY {IndexType.Space()} ({string.Join(", ", Columns.Select(c => c.Script()).ToArray())})";
						sql = sql + " WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]";
					}
					return sql;
			}

			return string.Empty;
		}
		public string ScriptDrop()
		{
			var sql = string.Empty;
			switch (Type)
			{
				case "INDEX":
					sql = $"IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[{Table.Owner}].[{Table.Name}]') AND name = N'{Name}')\r\n";
					sql += $" DROP INDEX [{Name}] ON [{Table.Owner}].[{Table.Name}] ";
					return sql;
				case "PRIMARY KEY":
					if (Table.IsType)
					{
						sql = string.Empty;
					}
					else
					{
						sql = $"ALTER TABLE [{Table.Owner}].[{Table.Name}] DROP CONSTRAINT [{Name}] ";
					}
					return sql;
			}
			return sql;
		}
	}
}
