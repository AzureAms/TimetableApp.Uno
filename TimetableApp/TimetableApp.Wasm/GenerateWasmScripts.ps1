@"
namespace TimetableApp.Core
{
    public partial class WasmWebClient
    {
        private const string CorsServer = "$args";
    }
}
"@ | Out-File ($PSScriptRoot + "\WasmWebClient.CorsServer.cs")