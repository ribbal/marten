using System;
using System.IO;
using Marten.Schema;

namespace Marten.Storage;

internal class UpdateFunction: UpsertFunction
{
    public UpdateFunction(DocumentMapping mapping): base(mapping, mapping.UpdateFunction)
    {
    }

    protected override void writeFunction(TextWriter writer, string argList, string securityDeclaration,
        string inserts, string valueList,
        string updates)
    {
        var statement = updates.Contains("where")
            ? $"UPDATE {_tableName} SET {updates} and id = docId;"
            : $"UPDATE {_tableName} SET {updates} where id = docId;";

        if (_mapping.Metadata.Revision.Enabled)
        {
            writer.WriteLine($@"
CREATE OR REPLACE FUNCTION {Identifier.QualifiedName}({argList}) RETURNS INTEGER LANGUAGE plpgsql {
    securityDeclaration
} AS $function$
DECLARE
  final_version INTEGER;
  current_version INTEGER;
BEGIN

  if revision = 1 then
    SELECT mt_version FROM {_tableName.QualifiedName} into current_version WHERE id = docId {_andTenantWhereClause};
    if current_version is not null then
      revision = current_version + 1;
    end if;
  end if;

  {statement}

  SELECT mt_version FROM {_tableName} into final_version WHERE id = docId {_andTenantWhereClause};
  RETURN final_version;
END;
$function$;
");
        }
        else if (_mapping.Metadata.Version.Enabled)
        {
            writer.WriteLine($@"
CREATE OR REPLACE FUNCTION {Identifier.QualifiedName}({argList}) RETURNS UUID LANGUAGE plpgsql {
    securityDeclaration
} AS $function$
DECLARE
  final_version uuid;
BEGIN
  {statement}

  SELECT mt_version FROM {_tableName} into final_version WHERE id = docId {_andTenantWhereClause};
  RETURN final_version;
END;
$function$;
");
        }
        else
        {
            writer.WriteLine($@"
CREATE OR REPLACE FUNCTION {Identifier.QualifiedName}({argList}) RETURNS UUID LANGUAGE plpgsql {
    securityDeclaration
} AS $function$
DECLARE
  final_version uuid;
BEGIN
  {statement}

  RETURN '{Guid.Empty}';
END;
$function$;
");
        }
    }
}
