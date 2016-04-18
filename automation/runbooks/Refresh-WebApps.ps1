workflow Refresh-WebApps
{
	$ResourceGroupName = "PowerShell-Runbooks";
	
    $connectionName = "AzureRunAsConnection"
	try
	{
    	# Get the connection "AzureRunAsConnection "
    	$servicePrincipalConnection=Get-AutomationConnection -Name $connectionName         

    	"Logging in to Azure..."
    	Add-AzureRmAccount `
        	-ServicePrincipal `
        	-TenantId $servicePrincipalConnection.TenantId `
        	-ApplicationId $servicePrincipalConnection.ApplicationId `
        	-CertificateThumbprint $servicePrincipalConnection.CertificateThumbprint 
	}
	catch {
    	if (!$servicePrincipalConnection)
    	{
        	$ErrorMessage = "Connection $connectionName not found."
        	throw $ErrorMessage
    	} else{
        	Write-Error -Message $_.Exception
        	throw $_.Exception
    }
}

	Login-AzureRmAccount;
	
    #Get all webaps
    $webapps = Get-AzureRmWebApp -ResourceGroupName $ResourceGroupName;
		
    #Restart each webapp in the resource group
    Foreach ($webapp in $webapps)	{
		#Restart-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $webapp.name
		Stop-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $webapp.name;
		Start-Sleep 10;
		Start-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $webapp.name;
	}
}