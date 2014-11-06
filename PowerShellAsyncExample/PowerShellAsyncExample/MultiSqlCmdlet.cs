using System.Data.SqlClient;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TTRider.PowerShellAsync;

namespace PowerShellAsyncExample
{
    [Cmdlet(VerbsLifecycle.Invoke, "MultiSql")]
    public class MultiSqlCmdlet : AsyncCmdlet
    {
        [NotNull, Parameter(Mandatory = true)]
        public string[] Server { get; set; }


        protected override Task ProcessRecordAsync()
        {
            return Task.WhenAll(
                this.Server.Select(
                    server => 
                        this.ExecuteStatement(server, "select * from sys.objects")));
        }

        [NotNull]
        async Task ExecuteStatement([NotNull] string server, [NotNull] string statement)
        {
            var connectionBuilding = new SqlConnectionStringBuilder
            {
                DataSource = server,
                IntegratedSecurity = true,
                AsynchronousProcessing = true
            };

            var connection = new SqlConnection(connectionBuilding.ConnectionString);

            await connection.OpenAsync();

            var cmd = connection.CreateCommand();
            cmd.CommandText = statement;

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var names = new string[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        names[i] = reader.GetName(i);
                    }

                    do
                    {
                        var item = new PSObject();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);

                            item.Properties.Add(new PSNoteProperty(names[i], value));
                        }

                        this.WriteObject(item);

                    } while (await reader.ReadAsync());
                }
            }
        }
    }
}
