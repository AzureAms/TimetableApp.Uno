# Fix for Github Pages. Local builds do not need this fix.

$s = Get-ChildItem -Name -Directory ($PSScriptRoot + "\bin\Release\net6.0\publish\package*"); 
$htmlFile = $PSScriptRoot + "\bin\Release\net6.0\publish\" + $s + "\index.html";
echo ("Copying file to " + $PSScriptRoot + $htmlFile);
Copy-Item ($PSScriptRoot + "\package_index.html") $htmlFile