workflow Shutdown-Start-ASM-VMs-Parallel
{
	Param(
		[Parameter(Mandatory=$true)]
        [String]
		$ServiceName,
		[Parameter(Mandatory=$true)]
        [Boolean]
		$Shutdown
	)
	
	#The name of the Automation Credential Asset this runbook will use to authenticate to Azure.
    $CredentialAssetName = "DefaultAzureCredential";
	
	#Get the credential with the above name from the Automation Asset store
    $Cred = Get-AutomationPSCredential -Name $CredentialAssetName;
    if(!$Cred) {
        Throw "Could not find an Automation Credential Asset named '${CredentialAssetName}'. Make sure you have created one in this Automation Account."
    }

    #Connect to your Azure Account
	Add-AzureAccount -Credential $Cred;
	
	if($Shutdown -eq $true){
		Write-Output "Stopping VMs in '$($ServiceName)' cloud service";
		Stop-AzureVM -ServiceName $ServiceName -Name * -Force;
	}
	else{
		Write-Output "Starting VMs in '$($ServiceName)' cloud service";
		Start-AzureVM -ServiceName $ServiceName -Name *;
	}	
}