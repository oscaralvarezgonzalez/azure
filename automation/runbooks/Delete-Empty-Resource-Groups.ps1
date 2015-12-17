workflow Delete-Empty-Resource-Groups
{
	#testing changes
	
	#The name of the Automation Credential Asset this runbook will use to authenticate to Azure.
    $CredentialAssetName = "DefaultAzureCredential";
	
	#Get the credential with the above name from the Automation Asset store
    $Cred = Get-AutomationPSCredential -Name $CredentialAssetName;
	
    if(!$Cred) {
        Throw "Could not find an Automation Credential Asset named '${CredentialAssetName}'. Make sure you have created one in this Automation Account."
    }

    #Connect to your Azure Account   	
	Add-AzureRmAccount -Credential $Cred;
	
	#Get Azure Resource Groups
	$rgs = Get-AzureRmResourceGroup;
	
	if(!$rgs){
		Write-Output "No resource groups in your subscription";
	}
	else{
		
		Write-Output "You have $($(Get-AzureRmResourceGroup).Count) resource groups in your subscription";
		
		foreach($resourceGroup in $rgs){
			$name=  $resourceGroup.ResourceGroupName;
			$count = (Get-AzureRmResource | where { $_.ResourceGroupName -match $name }).Count;
			
			if($count -eq 0){
				Write-Output "The resource group $name has $count resources. Deleting it...";
				Remove-AzureRmResourceGroup -Name $name -Force;
			}
			else{
				Write-Output "The resource group $name has $count resources";
			}
		}
		
		Write-Output "Now you have $((Get-AzureRmResourceGroup).Count) resource group(s) in your subscription";
		
	}  
}