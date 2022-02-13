param ($CorsServer, $Domain);

@"
namespace TimetableApp.Core
{
    public partial class WasmWebClient
    {
        private const string CorsServer = "$CorsServer";
    }
}
"@ | Out-File ($PSScriptRoot + "\WasmWebClient.CorsServer.cs");

if ("$Domain" -ne "")
{
    "$Domain" | Out-File ($PSScriptRoot + "\wwwroot\CNAME");
}