using System.IO;
using System.Linq;
using Jube.Service.Agent.ServiceToolCatalogue;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test
{
    public sealed class ZzDumpTests
    {
        [Fact]
        public void Dump()
        {
            var rows = ServiceToolCatalogue.All.Select(t => new
            {
                t.Name, Kind = t.Kind.ToString(), t.Idempotent, t.Destructive, t.Description,
                Perms = t.RequiredPermissions.ToArray(), t.RequiresLandlord
            });

            File.WriteAllText("/tmp/claude-1000/catalogue.json", JsonConvert.SerializeObject(rows));
        }
    }
}
