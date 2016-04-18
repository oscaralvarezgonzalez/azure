Write-Output "PSModulePath environment variable on worker $env:ComputerName`:"
Foreach ($item in $env:PSModulePath.split(";"))
{
	Write-Output $item
}