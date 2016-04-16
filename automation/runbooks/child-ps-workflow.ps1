workflow child-ps-workflow
{
	param(
		[parameter(Mandatory)]
		[string] $Dad
	)
	Write-Output "Hello from child-ps-workflow (called from $Dad)";
}